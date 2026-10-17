using System.Collections.Generic;
namespace CraftopiaDiagnostic
{
    // Differences are evidence only, never completion decisions.
    public sealed class ProgressionObservation
    {
        private string context;
        private readonly Dictionary<int,int> stages=new Dictionary<int,int>();
        public Dictionary<string,object> Observe(string currentContext, int instance, int? stage)
        {
            if(currentContext!=context || currentContext==null) { stages.Clear(); context=currentContext; }
            if(currentContext==null || !stage.HasValue) { stages.Remove(instance); return null; }
            if(!stages.TryGetValue(instance,out var previous)) {
                if(stages.Count<128) stages[instance]=stage.Value;
                return null;
            }
            stages[instance]=stage.Value;
            return previous==stage.Value ? null : new Dictionary<string,object> {
                ["before"]=previous,["after"]=stage.Value,
                ["interpretation"]="sampled_change_not_proof_of_paid_repair_or_new_completion" };
        }
        public void Retain(HashSet<int> instances)
        {
            foreach(var id in new List<int>(stages.Keys)) if(!instances.Contains(id)) stages.Remove(id);
        }
    }
}
