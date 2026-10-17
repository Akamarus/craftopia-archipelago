using System.Collections.Generic;

namespace CraftopiaDiagnostic
{
    public static class MilestoneEvidence
    {
        public static Dictionary<string,object> Anubis(object page) => EvidenceProjection.Read(page,
            new[]{"_exchangeCount","_lastExchange","_totalRewardCount"});

        public static Dictionary<string,object> Category(object manager,int categoryId)
        {
            var values=CatalogFields.Field(manager,"categoryTookList") as List<int>;
            bool truncated=values!=null && values.Count>1024;
            return new Dictionary<string,object>{["category_id"]=categoryId,
                ["taken"]=values==null || truncated ? null : (object)values.Contains(categoryId),
                ["truncated"]=truncated,["status"]=values==null ? "field_missing" : "collected"};
        }
    }
}
