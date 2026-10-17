using System;
using CraftopiaDiagnostic;
static class DiscoveryCacheTests
{
    public static void Run()
    {
        var cache = new DiscoveryCache<object>();
        var now = DateTime.UtcNow;
        var value = new object(); int scans = 0;
        Func<object> discover = () => { scans++; return value; };
        for (int i=0;i<1000;i++)
            if (cache.Get(now.AddSeconds(i), x=>x!=null, discover)!=value) throw new Exception("Cache lost object");
        if(scans!=1) throw new Exception("Repeated scene discovery in steady state");
        cache.Get(now.AddSeconds(1001), x=>false, ()=>{scans++;return null;});
        cache.Get(now.AddSeconds(1002), x=>false, ()=>{scans++;return null;});
        if(scans!=2) throw new Exception("Missing world retry not throttled");
        if(cache.Get(now.AddSeconds(1012), x=>x!=null, discover)!=value || scans!=3) throw new Exception("World replacement not discovered");
        Console.WriteLine("DiscoveryCacheTests: steady-state reuse, destroyed/missing retry and replacement passed");
    }
}
