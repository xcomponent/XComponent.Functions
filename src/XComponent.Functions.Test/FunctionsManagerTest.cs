using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using XComponent.Functions.Core;
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
            var functionsManager = new FunctionsManager("component", "statemachine");
            functionsManager.AddTask(null, null, null, context, sender);
            functionsManager.ApplyFunctionResult(functionResult,
                    publicMemberBefore,
                    null,
                    context,
                    sender);

            Assert.AreEqual("after", publicMemberBefore.State);
        }

        [Test]
        public void ApplyFunctionResultShouldThrowSerializationException()
        {
            var publicMemberBefore = new PublicMember() { State = "before" };

            var publicMemberAfter = new JObject();
            publicMemberAfter.Add("State", new JValue("after"));

            var functionResult = new FunctionResult()
            {
                PublicMember = "Hello",
            };

            var functionsManager = new FunctionsManager("component", "statemachine");
            
            Assert.Throws<SerializationException>(() => functionsManager.ApplyFunctionResult(functionResult,
                publicMemberBefore,
                null,
                new object(),
                new object()));
        }

        [Test]
        public void ApplyFunctionResultShouldThrowSenderNotFoundException()
        {
            var functionsManager = new FunctionsManager("component", "statemachine");

            Assert.Throws<Exception>(() => functionsManager.ApplyFunctionResult(null,
                null,
                null,
                new object(),
                new object()));
        }
    }
}
