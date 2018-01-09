using System;
using NUnit.Framework;
using XComponent.Functions.Core;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;

namespace XComponent.Functions.Test
{

    [TestFixture]
    public class ControllersTest
    {

        private readonly int Port = new Random().Next(40000, 50000);
        private IFunctionsManager _functionsManager;
        private const string ComponentName = "ComponentName";
        private const string StateMachineName = "StateMachineName";
        private string _baseAddress;

        public ControllersTest() {
			Environment.SetEnvironmentVariable("OWIN_SERVER", "Microsoft.Owin.Host.HttpListener.OwinServerFactory, XComponent.Functions.Core");

            _baseAddress = $"http://127.0.0.1:{Port}";

            _functionsManager = FunctionsFactory.Instance
                .CreateFunctionsManager(
                        ComponentName,
                        StateMachineName, 
                        new Uri(_baseAddress));
        }

        [Test]
        public void GetControllerReturnsBadRequestIfComponentUnknown() {
            var address = $"http://127.0.0.1:{Port}/api/Functions?componentName={ComponentName+"Wrong"}&StateMachineName={StateMachineName}";
            var httpClient = new HttpClient();

            var responseTask = httpClient.GetAsync(address);

            responseTask.Wait();

            var response = responseTask.Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Test]
        public void PostTaskControllerReturnsBadRequestIfUnknownComponentName() {
            var address = $"http://127.0.0.1:{Port}/api/Functions";
            var httpClient = new HttpClient();

            var functionResult = new FunctionResult() {
                ComponentName = ComponentName + "Wrong",
                StateMachineName = StateMachineName
            };

            var jsonString = JsonConvert.SerializeObject(functionResult);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var responseTask = httpClient.PostAsync(address, content);

            responseTask.Wait();

            var response = responseTask.Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
