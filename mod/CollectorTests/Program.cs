using System;
using System.Collections.Generic;
using System.Reflection;
class BaseRow { public int id = 51; }
class Row : BaseRow { public int worldLevel = 3; public int[] materialIds = { 1, 2 }; public object secret = new object(); public string Title => throw new Exception("property must not execute"); }
class StationData { public int id = 212; public int familyId = 20012; }
class StationItem { public StationData data { get; } = new StationData(); }
class Station { public StationItem _BldgItem_InItemSlot { get; } = new StationItem(); }
class Program {
    static void Main() {
        var type = Assembly.GetExecutingAssembly().GetType("CraftopiaDiagnostic.CatalogFields");
        if (type == null) throw new Exception("Missing field-only catalog reader");
        var method = type.GetMethod("Read");
        var row = new Row();
        var result = (Dictionary<string, object>)method.Invoke(null, new object[] {row, new[] {"id", "worldLevel", "materialIds", "secret", "Title", "missing"}});
        if ((int)result["id"] != 51 || (int)result["worldLevel"] != 3) throw new Exception("Inherited field lost");
        if (result["Title"] != null || result["missing"] != null || result["secret"] != null) throw new Exception("Unknown/property/object must remain null");
        var values = (List<object>)result["materialIds"];
        if (values.Count != 2 || (int)values[1] != 2) throw new Exception("Array values lost");
        var stationMethod = type.GetMethod("StationIdentity");
        if (stationMethod == null) throw new Exception("Missing field-only station identity reader");
        var station = (Dictionary<string, object>)stationMethod.Invoke(null, new object[] {new Station()});
        if ((int)station["item_id"] != 212 || (int)station["family_id"] != 20012) throw new Exception("Station mapping lost");
        var absent = (Dictionary<string, object>)stationMethod.Invoke(null, new object[] {new Row()});
        if (absent["item_id"] != null || absent["family_id"] != null) throw new Exception("Missing station data invented");
        Console.WriteLine("CollectorTests: field inheritance, primitive arrays, missing fields, property and object exclusion passed");
        EvidenceTests.Run();
        ProgressionEvidenceTests.Run(); DiscoveryCacheTests.Run(); MilestoneEvidenceTests.Run();
    }
}
