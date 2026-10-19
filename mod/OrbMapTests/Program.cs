using System;
using System.Linq;
using CraftopiaArchipelago;
class Program {
 static void A(bool v,string s){if(!v)throw new Exception(s);}
 static void Main(){
 var m=new OrbMapState(); var a=new OrbMarker(1,2001,2,3,4); var b=new OrbMarker(2,2002,5,6,7);
 m.ApplySession("a",new[]{a,b}); A(m.Visible(false).Count()==2,"unexplored selected visible");
 m.MarkCollected("a",1); A(m.Visible(true).Count()==1,"hide collected");
 m.ApplyCompleted("a",new long[]{2002}); A(m.Visible(true).Count()==0,"released checked is complete");
 A(m.Visible(false).Single(x=>x.Guid==2).Completion==OrbCompletion.Checked,"release distinct from pickup");
 m.ApplySession("a",new[]{a,b});A(m.Visible(true).Count()==0,"same session retains pending");
 m.ApplySession("b",new[]{a});m.ApplyCompleted("a",new long[]{2001});m.MarkCollected("a",1);
 A(m.Visible(true).Count()==1,"stale session cannot mutate");
 m.ApplySession("c",new[]{new OrbMarker(3,2003,float.NaN,0,0),new OrbMarker(4,2004,0,0,0),a,a});
 A(m.Visible(false).Count()==2,"nonfinite rejected duplicates collapse real origin allowed");
 m.Clear();A(!m.Visible(false).Any(),"clear removes markers");
 Console.WriteLine("OrbMapTests passed"); }
}