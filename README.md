# XComponent.Functions
XComponent Functions is used to take advantage of XComponent in any programming language/environment (Node, Java, C++, ...)


[![Slack](http://slack.xcomponent.com/badge.svg)](http://slack.xcomponent.com/)

[![Build status](https://ci.appveyor.com/api/projects/status/dsj723fkcacptuoq?svg=true)](https://ci.appveyor.com/project/fredericcarre/xcomponent-functions)

[![XComponent.Functions nuget](https://img.shields.io/nuget/v/XComponent.Functions.svg)](https://www.nuget.org/packages/XComponent.Functions/)


This library automatically exposes XComponent Triggered Methods to a Rest Endpoint. This way, Triggered methods can be implemented in any language. 
Moreover, with this library you can take advantage of XComponent Studio to design complex orchestrations and code in the technology that fits your needs.

# How it works ?

1. Reference the following Nuget package in the TriggeredMethod projects: 

https://www.nuget.org/packages/XComponent.Functions/

2. Initialize the library in the Triggered Method project.

First of all, you need to create a **FunctionsManager** object for each state machine. **FunctionsManager**s expose, through a Rest API, the tasks to execute for a given type of state machine. 
Initialize a **FunctionsManager** with the following lines of code :

```csharp
 FunctionsManager myFunctionManager = FunctionsFactory.Instance.CreateFunctionsManager(ComponentHelper.COMPONENT_NAME, "MyStateMAchineName", FunctionsFactory.DefaultUrl);
 ```

 > Note: The **FunctionsManager** object can be stored on the TriggeredMethodContext.

 3. To implement a trigger method, add a call to the **AddTask** method of the provided **FunctionsManager** .

```csharp
public static void ExecuteOn_Calculating_Through_Calculate(Calculate calculate, Calculator calculator, object object_InternalMember, Context context, ICalculateCalculateOnCalculatingCalculatorSenderInterface sender)
{
    myFunctionManager.AddTask(calculate, calculator, object_InternalMember, context, sender);
} 
```

4. REST API

By default, the REST Endpoint is: http://127.0.0.1:9676/swagger/ui/index

This REST API exposes two methods:

+ The GET method should be called to retrieve the next task to execute.
```Json
{
  "Event": {},
  "PublicMember": {},
  "InternalMember": {},
  "Context": {},
  "ComponentName": "string",
  "StateMachineName": "string",
  "FunctionName": "string",
  "RequestId": "string"
}
```

+ The POST method should be used to push the result of the Triggered Method execution.

```Json
{
  "ComponentName": "string",
  "StateMachineName": "string",
  "PublicMember": {},
  "InternalMember": {},
  "Senders": [
      {
        "SenderName": "string",
        "SenderParameter": "string",
        "UseContext": true
      }
   ],
  "RequestId": "string"
}
```

> the Public Member and Internal Member of the state mamchine instance can be modified this way. You can also call one or more sender methods by providing, for each of them, its name and parameter in the `Senders` field.



