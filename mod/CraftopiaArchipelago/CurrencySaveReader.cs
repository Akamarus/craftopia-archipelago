using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CraftopiaArchipelago
{
    internal static class CurrencySaveReader
    {
        // SQLiteNet is already shipped by the game. Do not use SaveDataConnection:
        // its constructor includes Create/ReadWrite, even when used for a load.
        internal static bool Matches(string path,string player,string inventory) {
            if(!Path.IsPathRooted(path) || !File.Exists(path)) return false;
            object connection=null;
            try {
                var type=Type.GetType("SQLite.SQLiteConnection, SQLiteNet",true);
                var flags=Type.GetType("SQLite.SQLiteOpenFlags, SQLiteNet",true);
                var ctor=type.GetConstructor(new[]{typeof(string),flags,typeof(bool)});
                if(ctor==null) return false;
                var readOnly=Convert.ToInt32(Enum.Parse(flags,"ReadOnly"));
                var mutex=Convert.ToInt32(Enum.Parse(flags,"FullMutex"));
                connection=ctor.Invoke(new[]{(object)path,Enum.ToObject(flags,readOnly|mutex),true});
                var scalar=type.GetMethods().Single(m=>m.Name=="ExecuteScalar" && m.IsGenericMethodDefinition &&
                    m.GetParameters().Length==2).MakeGenericMethod(typeof(string));
                // Both rows tested in ONE SQLite read snapshot. No write SQL,
                // no create flag, no logs containing save contents.
                const string query="SELECT CASE WHEN (SELECT Value FROM Entity WHERE Id='PlayerSave') = ? " +
                    "AND (SELECT Value FROM Entity WHERE Id='InGame') = ? THEN 'verified' ELSE 'mismatch' END";
                return (string)scalar.Invoke(connection,new object[]{query,new object[]{player,inventory}})=="verified";
            } catch { return false; }
            finally { try { (connection as IDisposable)?.Dispose(); } catch { } }
        }
    }
}
