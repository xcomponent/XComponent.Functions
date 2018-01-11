using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using XComponent.Functions.Core;
using XComponent.Functions.Core.Exceptions;

namespace XComponent.Functions.Controllers
{
    public class ConfigurationController: ApiController
    {
        [SwaggerResponse(HttpStatusCode.NoContent, "Configuration updated")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request")]
        public HttpResponseMessage PostConfiguration(FunctionsConfiguration configuration)
        {
            try {
                FunctionsFactory.Instance.Configuration = configuration;
                return Request.CreateResponse(HttpStatusCode.NoContent);
            } catch(ValidationException ve) {
                return Request.CreateResponse<ValidationException>(HttpStatusCode.BadRequest, ve);
            }
        }

    }
}

