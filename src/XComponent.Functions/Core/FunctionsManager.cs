using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using XComponent.Functions.Core.Clone;
using XComponent.Functions.Core.Owin;
using XComponent.Functions.Core.Senders;
using XComponent.Functions.Core.Exceptions;
using XComponent.Functions.Utilities;

namespace XComponent.Functions.Core
{
    public class FunctionsManager : IFunctionsManager
    {
        private OwinServer _owinServerRef;
        private readonly ConcurrentQueue<FunctionParameter> _taskQueue = new ConcurrentQueue<FunctionParameter>();

        internal event Action<FunctionResult> NewTaskFunctionResult;

        private readonly Dictionary<object, SenderWrapper> _senderWrapperBySender = new Dictionary<object, SenderWrapper>();
        private List<string> _pendingRequests = new List<string>();

        internal FunctionsManager(string componentName, string stateMachineName)
        {
            ComponentName = componentName;
            StateMachineName = stateMachineName;
        }

        public string ComponentName { get; }
        public string StateMachineName { get; }

        internal void InitManager(Uri url)
        {
            _owinServerRef = OwinServerFactory.CreateOwinServer(url);
        }

        public void ApplyFunctionResult(FunctionResult result, object publicMember, object internalMember, object context, object sender)
        {
            if (result == null)
                throw new ValidationException("Result should not be null");

            if (!_senderWrapperBySender.ContainsKey(sender))
                throw new ValidationException("Sender object received from worker not found in dictionary");

            if (context == null)
                throw new ValidationException("Context should not be null");

            if (publicMember != null && result.PublicMember != null)
            {
                var newPublicMember = SerializationHelper.DeserializeObjectFromType(publicMember.GetType(), result.PublicMember);
                if (newPublicMember.GetType() == publicMember.GetType())
                {
                    XCClone.Clone(newPublicMember, publicMember);
                }
                else
                {
                    Trace.WriteLine($"Deserialized object type {newPublicMember.GetType()} doesn't match required type {publicMember.GetType()}");
                }
            }

            if (internalMember != null && result.InternalMember != null)
            {
                var newInternalMember = SerializationHelper.DeserializeObjectFromType(internalMember.GetType(), result.InternalMember);
                if (newInternalMember.GetType() == internalMember.GetType())
                {
                    XCClone.Clone(newInternalMember, publicMember);
                }
                else
                {
                    Trace.WriteLine($"Deserialized object type {newInternalMember.GetType()} doesn't match required type {internalMember.GetType()}");
                }
            }

            lock (_senderWrapperBySender)
            {
                _senderWrapperBySender[sender].TriggerSender(result, context);
            }
        }

        public async Task<FunctionResult> AddTaskAsync(object xcEvent, object publicMember, object internalMember,
            object context, object sender, [CallerMemberName] string functionName = null)
        {
            if (xcEvent == null) throw new ValidationException("Event should not be null");
            if (publicMember == null) throw new ValidationException("Public member should not be null");
            if (internalMember == null) throw new ValidationException("Internal member should not be null");
            if (context == null) throw new ValidationException("Context should not be null");
            if (sender == null) throw new ValidationException("Sender should not be null");

            RegisterSender(sender);

            var functionParameter = FunctionParameterFactory.CreateFunctionParameter(xcEvent,
                publicMember,
                internalMember,
                context, ComponentName,
                StateMachineName, functionName);

            var requestId = functionParameter.RequestId;

            var taskCompletionSource = new TaskCompletionSource<FunctionResult>();

            Action<FunctionResult> resultHandler = null;

            resultHandler = delegate (FunctionResult result)
            {
                if (result.RequestId != requestId) return;

                NewTaskFunctionResult -= resultHandler;

                lock (_pendingRequests)
                {
                    _pendingRequests.Remove(requestId);
                }

                taskCompletionSource.SetResult(result);
            };

            lock (_pendingRequests)
            {
                _pendingRequests.Add(requestId);
            }

            NewTaskFunctionResult += resultHandler;

            _taskQueue.Enqueue(functionParameter);

            var cancellationTokenSource = new CancellationTokenSource();
            var timeoutValue = FunctionsFactory.Instance.Configuration.TimeoutInMillis;

            if (timeoutValue.HasValue)
            {
                cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(timeoutValue.Value));

                cancellationTokenSource.Token.Register(() => taskCompletionSource.TrySetCanceled());
            }

            try {
                return await taskCompletionSource.Task;
            } catch(TaskCanceledException) {
                return new FunctionResult
                {
                    IsError = true,
                    ErrorMessage = $"Timeout exceeded ({timeoutValue} ms)"
                };
            } catch(Exception e) {
                return new FunctionResult
                {
                    IsError = true,
                    ErrorMessage = e.ToString()
                };
            }
        }

        public Task AddTask(object xcEvent, object publicMember, object internalMember,
            object context, object sender, [CallerMemberName] string functionName = null)
        {
            return AddTaskAsync(xcEvent, publicMember, internalMember, context, sender, functionName)
                .ContinueWith((taskResult) =>
                {
                    ApplyFunctionResult(taskResult.Result, publicMember, internalMember, context, sender);
                });
        }

        public void AddTaskResult(FunctionResult functionResult)
        {
            if (functionResult == null) throw new ValidationException("Function result cannot be null");
            lock (_pendingRequests)
            {
                if (!_pendingRequests.Contains(functionResult.RequestId))
                    throw new ValidationException($"Unknown request id '{functionResult.RequestId}'");
            }
            NewTaskFunctionResult?.Invoke(functionResult);
        }

        public FunctionParameter GetTask()
        {
            FunctionParameter functionParameter = null;
            return _taskQueue.TryDequeue(out functionParameter) ? functionParameter : null;
        }

        private void RegisterSender(object sender)
        {
            if (sender == null)
                throw new ValidationException("Sender object cannot be null");

            lock (_senderWrapperBySender)
            {
                if (!_senderWrapperBySender.ContainsKey(sender))
                {
                    _senderWrapperBySender.Add(sender, new SenderWrapper(sender));
                }
            }
        }

        public void Dispose()
        {
            OwinServerFactory.UnRegisterOwinServer(_owinServerRef);
        }
    }
}
