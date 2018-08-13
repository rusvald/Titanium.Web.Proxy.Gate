using System.Collections.Generic;

namespace Titanium.Web.Proxy.Http
{
    public class InternalDataStore : Dictionary<string, object>
    {
        public bool TryGetValueAs<T>(string key, out T value)
        {
            object value1;
            bool result = TryGetValue(key, out value1);
            if (result)
            {
                value = (T)value1;
            }
            else
            {
                value = default(T);
            }

            return result;
        }

        public T GetAs<T>(string key)
        {
            return (T)this[key];
        }
    }
}