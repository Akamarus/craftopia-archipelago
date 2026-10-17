using System;
using System.Collections.Generic;
using CraftopiaDiagnostic;
static class MilestoneEvidenceTests
{
    class Parent { public int[] _exchangeCount = { 11, 0, 0 }; }
    class Page : Parent { public int _lastExchange=10, _totalRewardCount=61; public int Secret => throw new Exception(); }
    class Manager { public List<int> categoryTookList = new List<int>{3}; }
    public static void Run() {
        var type=typeof(CatalogFields).Assembly.GetType("CraftopiaDiagnostic.MilestoneEvidence");
        if(type==null) throw new Exception("Missing milestone evidence reader");
        var page=new Page();
        var snapshot=(Dictionary<string,object>)type.GetMethod("Anubis").Invoke(null,new object[]{page});
        page._exchangeCount[0]=12;
        var fields=(Dictionary<string,object>)snapshot["fields"];
        var counter=(Dictionary<string,object>)fields["_exchangeCount"];
        if((int)((List<object>)counter["items"])[0]!=11) throw new Exception("Snapshot aliases counter");
        var method=type.GetMethod("Category");
        var claimed=(Dictionary<string,object>)method.Invoke(null,new object[]{new Manager(),3});
        if(!Equals(claimed["taken"],true)) throw new Exception("Claimed category lost");
        var missing=(Dictionary<string,object>)method.Invoke(null,new object[]{new object(),3});
        if(missing["taken"]!=null) throw new Exception("Missing state invented");
        var large=new Manager(); large.categoryTookList.Clear();
        for(int i=0;i<1100;i++) large.categoryTookList.Add(i);
        var truncated=(Dictionary<string,object>)method.Invoke(null,new object[]{large,1099});
        if(!Equals(truncated["truncated"],true) || truncated["taken"]!=null) throw new Exception("Truncated state treated complete");
        Console.WriteLine("MilestoneEvidenceTests: copied counters, inherited fields, missing and truncated claims passed");
    }
}
