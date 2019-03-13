using System;
using System.Net.Http;

namespace XComponent.Functions.Utilities
{
    public static class HttpResponseMessageHelper
    {
        public static HttpResponseMessage SetMandatoryFields(this HttpResponseMessage httpResponseMessage)
        {
            try
            {
                if (httpResponseMessage.ReasonPhrase == null)
                {
                    httpResponseMessage.ReasonPhrase = string.Empty;
                }
            }
            catch (Exception)
            {
                httpResponseMessage.ReasonPhrase = string.Empty;
            }

            return httpResponseMessage;
        }
    }
}
