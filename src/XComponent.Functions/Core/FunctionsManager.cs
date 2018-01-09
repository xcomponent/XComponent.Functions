using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using XComponent.Functions.Core.Clone;
using XComponent.Functions.Core.Owin;
using XComponent.Functions.Core.Senders;
using XComponent.Functions.Utilities;

namespace XComponent.Functions.Core
{
    public class FunctionsManager : IFunctionsManager
    {
        private OwinServer _owinServerRef;
        private readonly ConcurrentQueue<FunctionParameter> _taskQueue = new ConcurrentQueue<FunctionParameter>();

        internal event Action<FunctionResult> NewTaskFunctionResult;

        private readonly Dictionary<object, SenderWrapper> _senderWrapperBySender = new Dictionary<object, SenderWrapper>();

        internal FunctionsManager(string componentName, string stateMachineName)
        {
            ComponentName = componentName;
            StateMachineName = stateMachineName;
        }

        public string ComponentName { get;  }
        public string StateMachineName { get;  }

        internal void InitManager(Uri url)
        {
            _owinServerRef = OwinServerFactory.CreateOwinServer(url);
        }

        public void ApplyFunctionResult(FunctionResult result, object publicMember, object internalMember, object context, object sender) {

            if (publicMember != null && result.PublicMember != null)
            {
                var newPublicMember = SerializationHelper.DeserializeObjectFromType(publicMember.GetType(), result.PublicMember);
                XCClone.Clone(newPublicMember, publicMember);
            }
            if (internalMember != null && result.InternalMember != null)
            {
                var newInternalMember = SerializationHelper.DeserializeObjectFromType(internalMember.GetType(), result.InternalMember);
                XCClone.Clone(newInternalMember, internalMember);
            }
            
            lock (_senderWrapperBySender)
            {
                if (_senderWrapperBySender.ContainsKey(sender)) {
                    _senderWrapperBySender[sender].TriggerSender(result, context);
                } else {
                    throw new Exception("Sender received from worker not found in dictionary");
                }
            }
        }

        public Task<FunctionResult> AddTaskAsync(object xcEvent, object publicMember, object internalMember,
            object context, object sender, [CallerMemberName] string functionName = null) 
        {
            RegisterSender(sender);

            var functionParameter =  FunctionParameterFactory.CreateFunctionParameter(xcEvent,
                publicMember,
                internalMember,
                context, ComponentName,
                StateMachineName, functionName);

            var requestId = functionParameter.RequestId;
            FunctionResult functionResult = null;
            
            var autoResetEvent = new AutoResetEvent(false);

            Action<FunctionResult> resultHandler = null;
            resultHandler = delegate(FunctionResult result)
            {
                if (result.RequestId == requestId)
                {
                    NewTaskFunctionResult -= resultHandler;
                    functionResult = result;
                    autoResetEvent.Set();
                }
            };

            NewTaskFunctionResult += resultHandler;

            _taskQueue.Enqueue(functionParameter);

            return Task.Run(() =>
            {
                autoResetEvent.WaitOne();
                return functionResult;
            });
        }

        public Task AddTask(object xcEvent, object publicMember, object internalMember,
            object context, object sender, [CallerMemberName] string functionName = null)
        {
            return AddTaskAsync(xcEvent, publicMember, internalMember, context, sender, functionName)
                    .ContinueWith((taskResult) => {
                        ApplyFunctionResult(taskResult.Result, publicMember, internalMember, context, sender);
                    });
        }

        public void AddTaskResult(FunctionResult functionResult)
        {
            NewTaskFunctionResult?.Invoke(functionResult);
        }

        public FunctionParameter GetTask()
        {
            FunctionParameter functionParameter = null;
            if (_taskQueue.TryDequeue(out functionParameter))
            {
                return functionParameter;
            }

            return null;
        }

        private void RegisterSender(object sender)
        {
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
