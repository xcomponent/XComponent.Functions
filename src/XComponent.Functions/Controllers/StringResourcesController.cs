using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using XComponent.Functions.Core;
using XComponent.Functions.Core.Exceptions;
using XComponent.Functions.Utilities;

namespace XComponent.Functions.Controllers
{
    public class StringResourcesController : ApiController
    {
        [SwaggerResponse(HttpStatusCode.OK, "Get Available String Resources as a list", typeof(FunctionParameter))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request")]
        public HttpResponseMessage GetStringResources()
        {
            try
            {
                return Request.CreateResponse(FunctionsFactory.Instance.GetKeyValuePairs()).SetMandatoryFields();
            }
            catch (ValidationException ve)
            {
                return Request.CreateResponse<ValidationException>(HttpStatusCode.BadRequest, ve).SetMandatoryFields();
            }
        }
    }
}
