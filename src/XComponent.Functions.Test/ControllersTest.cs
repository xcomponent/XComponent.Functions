using System;
using NUnit.Framework;
using XComponent.Functions.Core;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
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

        public ControllersTest()
        {
            Environment.SetEnvironmentVariable(
                    "OWIN_SERVER",
                    "Microsoft.Owin.Host.HttpListener.OwinServerFactory, XComponent.Functions.Core");

            _baseAddress = $"http://127.0.0.1:{Port}";

            _functionsManager = FunctionsFactory.Instance
                .CreateFunctionsManager(
                        ComponentName,
                        StateMachineName,
                        new Uri(_baseAddress));
        }

        [Test]
        public async Task GetControllerReturnsBadRequestIfComponentUnknown()
        {
            var wrongComponentName = ComponentName + "Wrong";
            var address = $"http://127.0.0.1:{Port}/api/Functions?componentName={wrongComponentName}&StateMachineName={StateMachineName}";

            var response = await new HttpClient().GetAsync(address);

            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsTrue(responseContent.Contains(wrongComponentName),
                    "Error message states what is wrong with the request");
        }

        [Test]
        public async Task PostTaskControllerReturnsBadRequestIfUnknownComponentName()
        {
            var wrongComponentName = ComponentName + "Wrong";
            var address = $"http://127.0.0.1:{Port}/api/Functions";

            var functionResult = new FunctionResult()
            {
                ComponentName = wrongComponentName,
                StateMachineName = StateMachineName
            };

            var jsonString = JsonConvert.SerializeObject(functionResult);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await new HttpClient().PostAsync(address, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsTrue(responseContent.Contains(wrongComponentName),
                    "Error message states what is wrong with the request");
        }

        [Test]
        public async Task PostConfigurationDoesNotUpdateBadConfiguration()
        {
            var address = $"http://127.0.0.1:{Port}/api/Configuration";

            var configuration = new FunctionsConfiguration()
            {
                TimeoutInMillis = -1000
            };

            var jsonString = JsonConvert.SerializeObject(configuration);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await new HttpClient().PostAsync(address, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsTrue(responseContent.Contains(configuration.TimeoutInMillis.ToString()),
                    "Error message states what is wrong with the request");
        }

        [Test]
        public async Task PostConfigurationUpdateConfiguration()
        {
            var address = $"http://127.0.0.1:{Port}/api/Configuration";

            var configuration = new FunctionsConfiguration()
            {
                TimeoutInMillis = 1000
            };

            var jsonString = JsonConvert.SerializeObject(configuration);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var response = await new HttpClient().PostAsync(address, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
            Assert.AreEqual(1000, FunctionsFactory.Instance.Configuration.TimeoutInMillis);
        }
    }
}
