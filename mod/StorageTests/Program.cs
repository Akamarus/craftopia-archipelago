using System;
using System.IO;
using CraftopiaArchipelago;
class Program {
 static void A(bool v,string message){if(!v)throw new Exception(message);}
 static void Main(){
  string temp=Path.Combine(Path.GetTempPath(),"ap-storage-"+Guid.NewGuid());Directory.CreateDirectory(temp);
  try {
   string old=Path.Combine(temp,"old"),root=Path.Combine(temp,"user"),other=Path.Combine(temp,"other");
   Directory.CreateDirectory(old);Directory.CreateDirectory(other);
   File.WriteAllText(Path.Combine(old,"craftopia-currency-test.json"),"delivered");
   File.WriteAllText(Path.Combine(old,"com.craftopia.archipelago.cfg"),"preferences");
   File.WriteAllText(Path.Combine(old,"unrelated.cfg"),"not ours");
   UserStorage.Migrate(root,new[]{old});
   A(File.ReadAllText(Path.Combine(root,"craftopia-currency-test.json"))=="delivered","preserve state");
   A(File.Exists(Path.Combine(old,"craftopia-currency-test.json")),"original untouched");
   A(!File.Exists(Path.Combine(root,"unrelated.cfg")),"ignore unrelated data");
   File.WriteAllText(Path.Combine(root,"craftopia-currency-test.json"),"new delivery");
   UserStorage.Migrate(root,new[]{old});
   A(File.ReadAllText(Path.Combine(root,"craftopia-currency-test.json"))=="new delivery","never replay stale migration");
   File.WriteAllText(Path.Combine(other,"craftopia-currency-test.json"),"conflict");
   string conflict=Path.Combine(temp,"conflict");bool failed=false;
   try {UserStorage.Migrate(conflict,new[]{old,other});}catch(IOException){failed=true;}
   A(failed&&!Directory.Exists(conflict),"conflict leaves no initialized root");
   string locked=Path.Combine(temp,"locked");failed=false;
   using(var stream=new FileStream(Path.Combine(old,"craftopia-currency-test.json"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)){
    try{UserStorage.Migrate(locked,new[]{old});}catch(IOException){failed=true;}
   }
   A(failed&&!Directory.Exists(locked),"active state cannot be migrated");
   string partial=Path.Combine(temp,"partial");Directory.CreateDirectory(partial);failed=false;
   try{UserStorage.Migrate(partial,new[]{old});}catch(IOException){failed=true;}
   A(failed,"unrecognized destination fails closed");
   string fresh=Path.Combine(temp,"fresh");UserStorage.Migrate(fresh,new string[0]);
   A(File.Exists(Path.Combine(fresh,"storage-v1.complete")),"fresh install initialized");
   string plugins=Path.Combine(temp,"plugins"),previous=Path.Combine(plugins,"previous"),current=Path.Combine(plugins,"new");
   Directory.CreateDirectory(Path.Combine(previous,"bridge","client"));Directory.CreateDirectory(current);
   File.WriteAllText(Path.Combine(previous,"bridge","client","connection_host.py"),"host");
   File.WriteAllText(Path.Combine(previous,"craftopia_observations.log"),"queued observation");
   File.WriteAllText(Path.Combine(previous,"bridge","client","craftopia_mp_seed.json"),"shared history");
   string switched=Path.Combine(temp,"switched");
   UserStorage.Migrate(switched,UserStorage.LegacyDirectories(old,current,plugins));
   A(File.ReadAllText(Path.Combine(switched,"craftopia_observations.log"))=="queued observation","previous bundle root outbox migrates");
   A(File.ReadAllText(Path.Combine(switched,"craftopia_mp_seed.json"))=="shared history","experimental shared history is preserved too");
   Console.WriteLine("StorageTests passed");
  } finally {Directory.Delete(temp,true);}
 }
}
