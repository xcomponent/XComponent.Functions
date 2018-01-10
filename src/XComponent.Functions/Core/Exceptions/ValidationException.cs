using System;

namespace XComponent.Functions.Core.Exceptions
{
    public class ValidationException: Exception
    {
        public ValidationException(string message):
            base(message)
        { }

        public ValidationException(string message, Exception exception) :
            base(message, exception)
        { }
    }
}
