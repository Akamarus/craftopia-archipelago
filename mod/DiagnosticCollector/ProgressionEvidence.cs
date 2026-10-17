using System;
using System.Collections.Generic;
namespace CraftopiaDiagnostic
{
    // Explicit projections avoid executing getters and avoid traversing inventories.
    public static class ProgressionEvidence
    {
        private static object Scalar(object value) => value is int || value is bool || value is byte ? value : null;
        private static object Field(object value,string name) => Scalar(CatalogFields.Field(value,name));
        public static Dictionary<string,object> Tower(object value) => new Dictionary<string,object> {
            ["guid"]=Field(value,"_GUID"), ["stage"]=Field(value,"_UnderBuildingNum"),
            ["is_tower"]=Field(value,"_IsTower"),
            ["definition"]=TowerData(CatalogFields.Field(value,"_Data")),
            ["identity_status"]="candidate_not_verified_across_worlds" };
        private static object Costs(object value)
        {
            if(!(value is Array array)) return new Dictionary<string,object>{["status"]="missing_or_not_array"};
            var rows=new List<object>();
            for(int i=0;i<Math.Min(array.Length,32);i++) {
                var row=array.GetValue(i);
                rows.Add(new Dictionary<string,object>{["item_id"]=Field(CatalogFields.Field(row,"item"),"id"),["count"]=Field(row,"count")});
            }
            return new Dictionary<string,object>{["count"]=array.Length,["rows"]=rows,["truncated"]=array.Length>32};
        }
        public static Dictionary<string,object> TowerData(object value)
        {
            var result=new Dictionary<string,object>{["item_id"]=Field(value,"_WorldHeritageItemId")};
            if(!(CatalogFields.Field(value,"requireItemsAllProgress") is Array stages)) {
                result["status"]="missing_or_not_array"; return result;
            }
            var rows=new List<object>();
            for(int i=0;i<Math.Min(stages.Length,32);i++) rows.Add(new Dictionary<string,object>{
                ["raw_stage_index"]=i,["costs"]=Costs(CatalogFields.Field(stages.GetValue(i),"requireItems"))});
            result["stages"]=rows; result["count"]=stages.Length; result["truncated"]=stages.Length>32;
            return result;
        }
        public static Dictionary<string,object> Age(object value) => new Dictionary<string,object>{
            ["world_level"]=Field(value,"worldLevel"),["gold"]=Field(value,"requireGold"),
            ["enabled"]=Field(value,"isEnabled"),["costs"]=Costs(CatalogFields.Field(value,"requireItems"))};
    }
}
