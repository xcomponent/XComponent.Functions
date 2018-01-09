
using System;
using System.Collections.Generic;
using XComponent.Functions.Core.Exceptions;

namespace XComponent.Functions.Core
{
    public class FunctionsFactory : IFunctionsFactory
    {
        public static readonly Uri DefaultUrl = new Uri("http://127.0.0.1:9676");

        internal static IFunctionsFactory instance;
        internal static object syncRoot = new Object();

        public static IFunctionsFactory Instance  
        {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        instance = new FunctionsFactory();
                    }
                }
                return instance;
            }
        }
        
        private FunctionsFactory() { }

        private static readonly Dictionary<int, IFunctionsManager> _functionsFactoryByKey = new Dictionary<int, IFunctionsManager>();

        public IFunctionsManager CreateFunctionsManager(string componentName, string stateMachineName, Uri url)
        {

            var functionsManager = new FunctionsManager(componentName, stateMachineName);

            lock (_functionsFactoryByKey)
            {
                int key = GetFunctionsManagerKey(componentName, stateMachineName);
                if (_functionsFactoryByKey.ContainsKey(key))
                {
                    throw  new FunctionsFactoryException("A function manager is already registered for: " + componentName + "," + stateMachineName);
                }

                functionsManager.InitManager(url);

                _functionsFactoryByKey.Add(key, functionsManager);
            }

            return functionsManager;
        }

        public void UnRegisterFunctionsManager(IFunctionsManager functionManager)
        {

            lock (_functionsFactoryByKey)
            {
                int key = GetFunctionsManagerKey(functionManager);
                if (_functionsFactoryByKey.ContainsKey(key))
                {
                    functionManager.Dispose();
                    _functionsFactoryByKey.Remove(key);
                }
            }
        }

        public FunctionParameter GetTask(string componentName, string stateMachineName)
        {
            int key = GetFunctionsManagerKey(componentName, stateMachineName);
            lock (_functionsFactoryByKey)
            {
                if (_functionsFactoryByKey.ContainsKey(key))
                {
                    return _functionsFactoryByKey[key].GetTask();
                } else {
                    throw new ValidationException($"No manager found for component {componentName} and state machine {stateMachineName}");
                }
            }

            return null;
        }

        public void AddTaskResult(FunctionResult result)
        {
            int key = GetFunctionsManagerKey(result.ComponentName, result.StateMachineName);
            lock (_functionsFactoryByKey)
            {
                if (_functionsFactoryByKey.ContainsKey(key))
                {
                    _functionsFactoryByKey[key].AddTaskResult(result);
                } else {
                    throw new ValidationException($"No manager found for component {result.ComponentName} and state machine {result.StateMachineName}");
                }
            }

        }

        internal static int GetFunctionsManagerKey(string componentName, string stateMachineName)
        {
            if (string.IsNullOrEmpty(componentName) || string.IsNullOrEmpty(stateMachineName))
            {
                return -1;
            }
            return componentName.GetHashCode() ^ stateMachineName.GetHashCode();
        }

        internal static int GetFunctionsManagerKey(IFunctionsManager functionsManager)
        {
            return GetFunctionsManagerKey(functionsManager.ComponentName, functionsManager.StateMachineName);
        }
    }
}
