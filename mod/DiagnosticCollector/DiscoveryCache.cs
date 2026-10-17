using System;
namespace CraftopiaDiagnostic
{
    // Unity validity is supplied by the caller so destroyed objects are not retained.
    public sealed class DiscoveryCache<T> where T : class
    {
        private T value;
        private DateTime retryAfter;
        public T Get(DateTime now, Func<T,bool> valid, Func<T> discover)
        {
            if(value != null && valid(value)) return value;
            value = null;
            if(now < retryAfter) return null;
            retryAfter = now.AddSeconds(10);
            value = discover();
            return value;
        }
    }
}
