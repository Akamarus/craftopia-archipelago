using System;
using System.Collections.Generic;
namespace CraftopiaArchipelago {
 public sealed class StarterKitItem {
  public readonly int ItemId,Amount; public readonly string Name;
  public StarterKitItem(int id,int amount,string name){ItemId=id;Amount=amount;Name=name;}
 }
 public static class StarterKitCatalog {
  public const long SkillPointReceipt=9000000000000000L;
  public static StarterKitItem[] Items(string choice) {
   switch(choice){
    case "Sword":return new[]{new StarterKitItem(44,1,"Stone One-Handed Sword")};
    case "Bow":return new[]{new StarterKitItem(92,1,"Branch Bow"),new StarterKitItem(181,30,"Branch Arrows")};
    case "Shield":return new[]{new StarterKitItem(116,1,"Wooden Shield")};
    case "Lance":return new[]{new StarterKitItem(1914,1,"Pitchfork")};
    case "SwordTwoHand":return new[]{new StarterKitItem(1702,1,"Zweihander")};
    case "Katana":return new[]{new StarterKitItem(6430,1,"Bokken")};
    case "MagicStaff":return new[]{new StarterKitItem(6392,1,"Crutch")};
    case "all_basic":var all=new List<StarterKitItem>();foreach(var c in new[]{"Sword","Bow","Shield","Lance","SwordTwoHand","Katana","MagicStaff"})all.AddRange(Items(c));return all.ToArray();
    default:return Array.Empty<StarterKitItem>();
   }
  }
 }
}
