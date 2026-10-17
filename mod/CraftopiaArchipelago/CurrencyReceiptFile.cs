using System;
using System.IO;
using System.Text;

namespace CraftopiaArchipelago
{
    // Exclusive process-lifetime ownership plus atomic replacement. No delete/move gap.
    public sealed class CurrencyReceiptFile : ICurrencyReceiptStore, IDisposable
    {
        readonly string path;
        readonly FileStream owner;
        readonly object sync=new object();
        bool disposed;
        public CurrencyReceiptFile(string path) {
            this.path=Path.GetFullPath(path);
            owner=new FileStream(this.path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        public string Read() {
            lock(sync) {
                if(disposed) throw new ObjectDisposedException(nameof(CurrencyReceiptFile));
                // Directories/access failures must not look like a fresh missing file.
                try { return File.ReadAllText(path,Encoding.UTF8); }
                catch(FileNotFoundException) { return null; }
            }
        }
        public bool CompareExchange(string expected,string next) {
            lock(sync) {
                try {
                    if(disposed || Read()!=expected || next==null) return false;
                    string temp=path+".tmp";
                    using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {
                        var bytes=Encoding.UTF8.GetBytes(next);
                        stream.Write(bytes,0,bytes.Length); stream.Flush(true);
                    }
                    if(expected==null) File.Move(temp,path);
                    else File.Replace(temp,path,null);
                    return true;
                } catch { return false; }
            }
        }
        public void Dispose() { lock(sync) { if(disposed) return; disposed=true; owner.Dispose(); } }
    }
}
