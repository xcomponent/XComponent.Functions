using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using XComponent.Functions.Core;
using XComponent.Functions.Core.Exceptions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using XComponent.Functions.Core.Senders;

namespace XComponent.Functions.Test
{

    [TestFixture]
    public class FunctionsManagerTest
    {

        [SetUp]
        public void SetUp() {
           Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
        }

        [Test]
        public void AddTaskShouldPutTaskOnQueue() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var xcEvent = new object();
            var publicMember = new object();
            var internalMember = new object();
            var context = new object();
            var sender = new object();

            functionsManager.AddTask(xcEvent, publicMember, internalMember, context, sender, "function");

            var enqueuedParameter = functionsManager.GetTask();

            Assert.AreEqual(xcEvent, enqueuedParameter.Event);
            Assert.AreEqual(publicMember, enqueuedParameter.PublicMember);
            Assert.AreEqual(internalMember, enqueuedParameter.InternalMember);
            Assert.AreEqual(context, enqueuedParameter.Context);
            Assert.AreEqual("component", enqueuedParameter.ComponentName);
            Assert.AreEqual("statemachine", enqueuedParameter.StateMachineName);
            Assert.AreEqual("function", enqueuedParameter.FunctionName);
            Assert.IsNotNull(enqueuedParameter.RequestId);
        }

        [Test]
        public void NullSenderThrowsValidationException() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var exception = Assert.Throws<ValidationException>(() => 
                functionsManager.
                    AddTask(new object(), new object(), new object(), new object(), null, "function"));

            Assert.IsTrue(exception.Message.Contains("Sender"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void NullPublicMemberThrowsValidationException() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var exception = Assert.Throws<ValidationException>(() => 
                functionsManager.
                    AddTask(new object(), null, new object(), new object(), new object(), "function"));

            Assert.IsTrue(exception.Message.Contains("Public"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void NullInternalMemberThrowsValidationException() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var exception = Assert.Throws<ValidationException>(() => 
                functionsManager.
                    AddTask(new object(), new object(), null, new object(), new object(), "function"));

            Assert.IsTrue(exception.Message.Contains("Internal"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void NullContextThrowsValidationException() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var exception = Assert.Throws<ValidationException>(() => 
                functionsManager.
                    AddTask(new object(), new object(), new object(), null, new object(), "function"));

            Assert.IsTrue(exception.Message.Contains("Context"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void NullEventThrowsValidationException() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var exception = Assert.Throws<ValidationException>(() => 
                functionsManager.
                    AddTask(null, new object(), new object(), new object(), new object(), "function"));

            Assert.IsTrue(exception.Message.Contains("Event"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public async Task AddTaskAsyncShouldReturnPostedResult() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var xcEvent = new object();
            var publicMember = new object();
            var internalMember = new object();
            var context = new object();
            var sender = Substitute.For<ISender>();

            var task = functionsManager.AddTaskAsync(xcEvent, publicMember, internalMember, context, sender, "function");

            var enqueuedParameter = functionsManager.GetTask();

            var functionResult = new FunctionResult() {
                ComponentName = "component",
                StateMachineName = "statemachine",
                PublicMember = "{}",
                InternalMember = "{}",
                Senders = null,
                RequestId = enqueuedParameter.RequestId,
            };

            functionsManager.AddTaskResult(functionResult);

            var publishedResult = await task;

            Assert.AreEqual(functionResult, publishedResult);
        }

        [Test]
        public void NullFunctionResultThrowsValidationException() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var exception = Assert.Throws<ValidationException>(() => functionsManager.AddTaskResult(null));

            Assert.IsTrue(exception.Message.Contains("null"),
                    "Exception message should show where is the problem");
        }

        private class PublicMember {
            public string State { get; set; }
        }

        public class DoEvent {
            public string Value { get; set; }
        }

        public class UndoEvent {
            public string Value { get; set; }
        }

        public interface ISender
        {
            void Do(object context, DoEvent transitionEvent, string privateTopic);
            void Undo(object context, UndoEvent transitionEvent, string privateTopic);
            void Reply(object context, object transitionEvent, string privateTopic);
            void SendEvent(DoEvent evt);
            void SendEvent(UndoEvent evt);
        }

        [Test]
        public async Task AddTaskShouldApplyPostedResultAndCallSenders() {
            var functionsManager = new FunctionsManager("component", "statemachine");

            var xcEvent = new object();
            var publicMember = new PublicMember() { State = "before" };
            var internalMember = new object();
            var context = new object();
            var sender = Substitute.For<ISender>();

            var task = functionsManager.AddTask(xcEvent, publicMember, internalMember, context, sender, "function");

            Task.Run(() => {
                while(true)
                {
                    var postedTask = functionsManager.GetTask();
                    if (postedTask != null)
                    {
                        var functionResult = new FunctionResult() {
                            ComponentName = "component",
                            StateMachineName = "statemachine",
                            PublicMember = "{ \"State\": \"after\" }",
                            Senders = new List<SenderResult> 
                            {
                                new SenderResult 
                                {
                                    SenderName = "Do",
                                    SenderParameter =  "{ \"Value\": \"do\" }",
                                    UseContext = true
                                },
                                new SenderResult
                                { 
                                    SenderName = "Undo",
                                    SenderParameter =  "{ \"Value\": \"undo\" }",
                                    UseContext = false
                                },
                                new SenderResult
                                {
                                    SenderName = "Reply",
                                    UseContext = true
                                }
                            },
                            RequestId = postedTask.RequestId,
                        };
                        functionsManager.AddTaskResult(functionResult);

                        break;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });

            await task;

            Assert.AreEqual("after", publicMember.State);
            sender.Received().Do(context, Arg.Is<DoEvent>(evt => evt.Value == "do"), null);
            sender.Received().SendEvent(Arg.Is<UndoEvent>(evt => evt.Value == "undo"));
            sender.Received().Reply(context, Arg.Any<object>(), null);
        }

        [Test]
        public void ApplyPostedResultShouldWorkWithJObjects() {
            var publicMemberBefore = new PublicMember() { State = "before" };

            var publicMemberAfter = new JObject();
            publicMemberAfter.Add("State", new JValue("after"));

            var functionResult = new FunctionResult() {
                PublicMember = publicMemberAfter,
            };

            var sender = new object(); 
            var context = new object();
            var internalMember = new object();
            var functionsManager = new FunctionsManager("component", "statemachine");

            functionsManager.AddTask(new object(),
                    publicMemberBefore,
                    internalMember,
                    context,
                    sender);

            functionsManager.ApplyFunctionResult(functionResult,
                    publicMemberBefore,
                    new object(),
                    context,
                    sender);

            Assert.AreEqual("after", publicMemberBefore.State);
        }

        [Test]
        public void ApplyFunctionResultShouldThrowInvalidSenderValidationException()
        {
            var sender = new object();
            var functionResult = new FunctionResult()
            {
                Senders = new List<SenderResult>() {
                    new SenderResult
                    { 
                        SenderName = "UnknownSender",
                        SenderParameter =  "{ \"Value\": \"undo\" }",
                        UseContext = false
                    },
                },
            };

            var functionsManager = new FunctionsManager("component", "statemachine");
            
            functionsManager.AddTask(new object(), new object(), new object(), new object(), sender, "function");

            while(true)
            {
                var postedTask = functionsManager.GetTask();
                if (postedTask != null)
                {
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            var exception = Assert.Throws<ValidationException>(() => functionsManager.ApplyFunctionResult(functionResult,
                new object(),
                new object(),
                new object(),
                sender));

            Assert.IsTrue(exception.Message.Contains("UnknownSender"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void ApplyFunctionResultShouldThrowValidationExceptionWhenDeserializationFails()
        {
            var sender = new object();
            var publicMemberBefore = new PublicMember() { State = "before" };

            var publicMemberAfter = new JObject();
            publicMemberAfter.Add("State", new JValue("after"));

            var functionResult = new FunctionResult()
            {
                PublicMember = "Hello",
            };

            var functionsManager = new FunctionsManager("component", "statemachine");

            functionsManager.AddTask(new object(), new object(), new object(), new object(), sender, "function");

            while(true)
            {
                var postedTask = functionsManager.GetTask();
                if (postedTask != null)
                {
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            
            var exception = Assert.Throws<ValidationException>(() => functionsManager.ApplyFunctionResult(functionResult,
                publicMemberBefore,
                new object(),
                new object(),
                sender));

            Assert.IsTrue(exception.Message.Contains("Hello"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void ApplyFunctionResultShouldThrowValidationExceptionWhenNullFunctionResult()
        {
            var sender = new object();

            var functionsManager = new FunctionsManager("component", "statemachine");

            functionsManager.AddTask(new object(), new object(), new object(), new object(), sender, "function");

            while(true)
            {
                var postedTask = functionsManager.GetTask();
                if (postedTask != null)
                {
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            
            var exception = Assert.Throws<ValidationException>(() => functionsManager.ApplyFunctionResult(null,
                new object(),
                new object(),
                new object(),
                sender));

            Assert.IsTrue(exception.Message.Contains("Result"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void ApplyFunctionResultShouldThrowValidationExceptionWhenNullContext()
        {
            var sender = new object();

            var functionResult = new FunctionResult() { };

            var functionsManager = new FunctionsManager("component", "statemachine");

            functionsManager.AddTask(new object(), new object(), new object(), new object(), sender, "function");

            while(true)
            {
                var postedTask = functionsManager.GetTask();
                if (postedTask != null)
                {
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            
            var exception = Assert.Throws<ValidationException>(() => functionsManager.ApplyFunctionResult(functionResult,
                new object(),
                new object(),
                null,
                sender));

            Assert.IsTrue(exception.Message.Contains("Context"),
                    "Exception message should show where is the problem");
        }

        [Test]
        public void AddTaskResultShouldThrowValidationExceptionWhenInvalidRequestId()
        {
            var sender = new object();

            var functionResult = new FunctionResult() { 
                RequestId = "unknown request id"
            };

            var functionsManager = new FunctionsManager("component", "statemachine");

            functionsManager.AddTask(new object(), new object(), new object(), new object(), sender, "function");

            while(true)
            {
                var postedTask = functionsManager.GetTask();
                if (postedTask != null)
                {
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            
            var exception = Assert.Throws<ValidationException>(() 
                    => functionsManager.AddTaskResult(functionResult));

            Assert.IsTrue(exception.Message.Contains("unknown request id"),
                    "Exception message should show where is the problem");
        }
    }
}
