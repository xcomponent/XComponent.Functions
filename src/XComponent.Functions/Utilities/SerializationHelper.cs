using System;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using XComponent.Functions.Core.Exceptions;

namespace XComponent.Functions.Utilities
{
    internal static class SerializationHelper
    {
        public static string SerializeObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            try
            {
                string serializeObject = JsonConvert.SerializeObject(obj);
                return serializeObject;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return null;
        }

        public static T DeserializeObject<T>(string obj) where T:class
        {
            if (string.IsNullOrEmpty(obj))
            {
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(obj);
            }
            catch (Exception e)
            {
                throw new ValidationException($"Couldn't serialize object {obj}", e);
            }
        }

        public static object DeserializeObjectFromType(Type objType, object objectToDeserialize)
        {
            if (objectToDeserialize == null)
            {
                return null;
            }
            try
            {
                return typeof(SerializationHelper).GetMethod(nameof(DeserializeObject),
                        BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(new[] { objType })
                    .Invoke(null, new[] { objectToDeserialize.ToString() });
            }
            catch (Exception e)
            {
                throw new ValidationException($"Couldn't deserialize object {objectToDeserialize} from {objType}", e);
            }
        }
    }
}
