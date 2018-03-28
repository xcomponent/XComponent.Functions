using System;
using System.Collections.Generic;

namespace XComponent.Functions.Core
{
    public interface IFunctionsFactory {
        IFunctionsManager CreateFunctionsManager(string componentName, string stateMachineName, Uri url = null);
        FunctionParameter GetTask(string componentName, string stateMachineName);
        void AddTaskResult(FunctionResult result);
        FunctionsConfiguration Configuration { get; set; }
        void AddKeyValuePair(string componentName, string key, string value);
        List<KeyValuePairSettingsItem> GetKeyValuePairs();
    }
}
