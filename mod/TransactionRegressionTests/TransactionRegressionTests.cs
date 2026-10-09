using System;
using System.Collections.Generic;
using System.IO;
using CraftopiaArchipelago;

class TransactionRegressionTests
{
    static int passed = 0, failed = 0;
    static void Check(bool cond, string name)
    {
        if (cond) { Console.WriteLine("PASS: " + name); passed++; }
        else { Console.WriteLine("FAIL: " + name); failed++; }
    }

    static EntitlementStore MakeStore(string path)
    {
        var s = new EntitlementStore(path, _ => {}, _ => {});
        s.Load();
        return s;
    }

    static void Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "craftopia-tx-test-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);

        // Test 1: Successful transaction persists entitlement and applied.
        {
            string storePath = Path.Combine(dir, "ent1.json");
            string intentPath = Path.Combine(dir, "intent1.json");
            string appliedPath = Path.Combine(dir, "applied1.json");
            var store = MakeStore(storePath);
            int mutations = 0;
            var tx = new GrantTransaction(
                intentPath, store,
                indices => {
                    // Simulate SaveGrantStateFor: write indices to file.
                    var data = new Dictionary<string, object> {
                        ["epoch"] = 123L,
                        ["applied"] = new List<object>(indices.ConvertAll(x => (object)x)),
                        ["quarantined"] = new List<object>()
                    };
                    File.WriteAllText(appliedPath, MiniJson.Stringify(data));
                    return true;
                },
                _ => {}, _ => {});
            var acks = tx.Execute(
                new List<long> { 10 }, 1, 123, 1,
                ids => { mutations++; return GrantApplyResult.Confirmed; });
            Check(mutations == 1, "success: exactly one mutation");
            Check(acks.Count == 1 && acks[0] == 10, "success: ack returned");
            var read = store.Read(1, 123, true, true);
            Check(read.Status == EntitlementStatus.Ok && read.Points == 1,
                "success: entitlement=1 (not double-counted)");
            Check(File.Exists(appliedPath), "success: applied file written");
            Check(!File.Exists(intentPath), "success: intent cleared");
        }

        // Test 2: Entitlement write failure -> retry succeeds without re-mutation.
        {
            string storePath = Path.Combine(dir, "ent2.json");
            string intentPath = Path.Combine(dir, "intent2.json");
            // Make store path unwritable initially (parent doesn't exist).
            string badDir = Path.Combine(dir, "nonexistent");
            var badStore = MakeStore(Path.Combine(badDir, "store.json"));
            int mutations = 0;
            var tx = new GrantTransaction(
                intentPath, badStore,
                indices => true, // applied persist succeeds
                _ => {}, _ => {});
            var acks1 = tx.Execute(
                new List<long> { 20 }, 1, 123, 1,
                ids => { mutations++; return GrantApplyResult.Confirmed; });
            Check(acks1.Count == 0, "fail: no ack on entitlement failure");
            Check(mutations == 1, "fail: mutation happened once");
            // Restore IO (create parent), retry same batch.
            Directory.CreateDirectory(badDir);
            var acks2 = tx.Execute(
                new List<long> { 20 }, 1, 123, 1,
                ids => { mutations++; return GrantApplyResult.Confirmed; });
            Check(mutations == 1, "retry: no second mutation");
            Check(acks2.Count == 1, "retry: ack on success");
            var read = badStore.Read(1, 123, true, true);
            Check(read.Points == 1, "retry: entitlement=1 (not double)");
        }

        // Test 3: Session binding — same index, different epoch, requires new mutation.
        {
            string storePath = Path.Combine(dir, "ent3.json");
            string intentPath = Path.Combine(dir, "intent3.json");
            var store = MakeStore(storePath);
            int mutations = 0;
            var tx = new GrantTransaction(
                intentPath, store, indices => true, _ => {}, _ => {});
            var acks1 = tx.Execute(
                new List<long> { 30 }, 1, 123, 1,
                ids => { mutations++; return GrantApplyResult.Confirmed; });
            Check(acks1.Count == 1, "epoch123: ack");
            // Same index, new epoch: should mutate again (different session).
            var acks2 = tx.Execute(
                new List<long> { 30 }, 1, 124, 1,
                ids => { mutations++; return GrantApplyResult.Confirmed; });
            Check(mutations == 2, "epoch124: new mutation (session-bound dedup)");
            Check(acks2.Count == 1, "epoch124: ack");
        }

        // Test 4: Eligibility same-epoch reconnect retains.
        {
            var e = new Eligibility(_ => {}, _ => {});
            var player = new object();
            e.OnLoadStart(player, 1, 1);
            e.OnLoadComplete(player, 1, 1);
            e.OnValidation(ValidationOutcome.Pass, player, 1, 123, 1);
            Check(e.IsEligible(player, 1, 123, 1), "eligible after validation+completion");
            e.OnSessionChange(123); // same epoch reconnect
            Check(e.IsEligible(player, 1, 123, 1), "same-epoch reconnect retains eligibility");
            e.OnSessionChange(124); // new epoch
            Check(!e.IsEligible(player, 1, 124, 1), "new epoch invalidates");
        }

        Console.WriteLine($"\n=== {passed} passed, {failed} failed ===");
        Environment.Exit(failed == 0 ? 0 : 1);
    }
}
