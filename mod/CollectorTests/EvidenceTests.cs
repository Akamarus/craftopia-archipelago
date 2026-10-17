using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

class EvidenceRow : BaseRow {
    public ulong large = ulong.MaxValue;
    public float invalid = float.NaN;
    public string text = "quote\"\n\t";
    public object secret = new Explosive();
    public int[] many = new int[300];
    public int Danger => throw new Exception("Getter ran");
}
class Explosive { public override string ToString() => throw new Exception("ToString ran"); }
static class EvidenceTests {
    static void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); }
    public static void Run() {
        var asm = Assembly.GetExecutingAssembly();
        var projection = asm.GetType("CraftopiaDiagnostic.EvidenceProjection");
        Check(projection != null, "Missing bounded evidence projection");
        var row = (Dictionary<string, object>)projection.GetMethod("Read").Invoke(null,
            new object[] { new EvidenceRow(), new[] {"id", "large", "invalid", "text", "secret", "many", "Danger", "absent"} });
        var writer = asm.GetType("CraftopiaDiagnostic.JsonWriter");
        Check(writer != null, "Missing shared JSON writer");
        var json = (string)writer.GetMethod("Stringify").Invoke(null, new object[] {row});
        using (var parsed = JsonDocument.Parse(json)) {
            var fields = parsed.RootElement.GetProperty("fields");
            Check(fields.GetProperty("id").GetInt32() == 51, "Inherited ID lost");
            Check(fields.GetProperty("large").GetUInt64() == ulong.MaxValue, "Unsigned ID truncated");
            Check(fields.GetProperty("invalid").ValueKind == JsonValueKind.Null, "Invalid JSON numeric");
            Check(fields.GetProperty("text").GetString() == "quote\"\n\t", "Escaping failed");
            Check(fields.GetProperty("secret").GetProperty("status").GetString() == "not_projected", "Arbitrary reference traversed");
            Check(fields.GetProperty("many").GetProperty("truncated").GetBoolean(), "Array cap not disclosed");
            Check(fields.GetProperty("Danger").GetProperty("status").GetString() == "field_missing", "Getter called");
        }
        var bufferType = asm.GetType("CraftopiaDiagnostic.ObservationBuffer");
        Check(bufferType != null, "Missing observation buffer");
        var buffer = Activator.CreateInstance(bufferType, new object[] {2});
        var add = bufferType.GetMethod("Add");
        for (int i=0;i<3;i++) add.Invoke(buffer,new object[]{new Dictionary<string,object>{{"n",i}}});
        Check((long)bufferType.GetProperty("Dropped").GetValue(buffer) == 1, "Overflow not reported");
        var batch = (List<Dictionary<string,object>>)bufferType.GetMethod("Peek").Invoke(buffer,null);
        Check(batch.Count == 2 && (int)batch[0]["n"] == 0, "Pending evidence overwritten");
        Check(((List<Dictionary<string,object>>)bufferType.GetMethod("Peek").Invoke(buffer,null)).Count == 2, "Peek drained evidence before write");
        bufferType.GetMethod("Confirm").Invoke(buffer,new object[]{batch.Count});
        Check(((List<Dictionary<string,object>>)bufferType.GetMethod("Peek").Invoke(buffer,null)).Count == 0, "Commit did not drain");
        Console.WriteLine("EvidenceTests: bounded projection, no getters/ToString, numeric JSON, overflow and persist-before-drain passed");
    }
}
