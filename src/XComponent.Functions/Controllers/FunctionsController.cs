using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using XComponent.Functions.Core;
using XComponent.Functions.Core.Exceptions;
using XComponent.Functions.Utilities;

namespace XComponent.Functions.Controllers
{
    public class FunctionsController: ApiController
    {
        [SwaggerResponse(HttpStatusCode.OK, "Next available task", typeof(FunctionParameter))]
        [SwaggerResponse(HttpStatusCode.NoContent, "No task available")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request")]
        public HttpResponseMessage GetTask(string componentName, string stateMachineName)
        {
            try {
                var response = FunctionsFactory.Instance.GetTask(componentName, stateMachineName);
                return Request.CreateResponse<FunctionParameter>(response == null ? HttpStatusCode.NoContent : HttpStatusCode.OK, response).SetMandatoryFields();
            } catch(ValidationException ve) {
                return Request.CreateResponse<ValidationException>(HttpStatusCode.BadRequest, ve).SetMandatoryFields();
            }
        }

        [SwaggerResponse(HttpStatusCode.NoContent, "Task result received")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request")]
        public HttpResponseMessage PostTaskResult(FunctionResult result)
        {
            try {
                FunctionsFactory.Instance.AddTaskResult(result);
                return Request.CreateResponse(HttpStatusCode.NoContent).SetMandatoryFields();
            } catch(ValidationException ve) {
                return Request.CreateResponse<ValidationException>(HttpStatusCode.BadRequest, ve).SetMandatoryFields();
            }
        }

    }
}
