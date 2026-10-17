using System;
namespace CraftopiaArchipelago {
 internal static class TowerIdentity {
  // Authored coordinates, not a nearest-tower or region heuristic.
  public static bool Matches(int definition,double x,double y,double z,int actualDefinition,double actualX,double actualY,double actualZ) =>
   definition==actualDefinition && Math.Abs(x-actualX)<=0.1 && Math.Abs(y-actualY)<=0.1 && Math.Abs(z-actualZ)<=0.1;
 }
}
