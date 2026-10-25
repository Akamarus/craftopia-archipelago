using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Steamworks;

class Program
{
    const uint App = 1307550;
    static T Wait<T>(SteamAPICall_t call) where T : struct
    {
        bool done=false, failed=false; T value=default;
        var result=CallResult<T>.Create((v,io)=>{value=v;failed=io;done=true;});
        result.Set(call);
        var watch=Stopwatch.StartNew();
        while(!done && watch.Elapsed.TotalSeconds<180){SteamAPI.RunCallbacks();Thread.Sleep(50);}
        result.Dispose();
        if(!done || failed)throw new IOException("Steam request failed or timed out; retain the item ID and inspect before retrying.");
        return value;
    }
    static void Require(bool ok,string action){if(!ok)throw new IOException(action+" failed");}
    static void Result(EResult result){if(result!=EResult.k_EResultOK)throw new IOException("Steam result: "+result);}
    static int Main(string[] args)
    {
        Environment.SetEnvironmentVariable("SteamAppId",App.ToString());
        if(!SteamAPI.Init()){Console.Error.WriteLine("Sign in to Steam with Craftopia installed.");return 2;}
        try
        {
            Require(SteamUtils.GetAppID().m_AppId==App && SteamUser.BLoggedOn(),"Craftopia Steam session");
            if(args.Length==2 && (args[0]=="details" || args[0]=="publish"))
            {
                var id=new PublishedFileId_t(ulong.Parse(args[1]));
                var query=SteamUGC.CreateQueryUGCDetailsRequest(new[]{id},1);
                try
                {
                    Result(Wait<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(query)).m_eResult);
                    Require(SteamUGC.GetQueryUGCResult(query,0,out SteamUGCDetails_t details),"Item details");
                    Result(details.m_eResult);
                    Require(details.m_nConsumerAppID.m_AppId==App,"Craftopia item identity");
                    Console.WriteLine("item="+id+" visibility="+details.m_eVisibility+" title="+details.m_rgchTitle);
                    if(args[0]=="publish")
                    {
                        Require(details.m_ulSteamIDOwner==SteamUser.GetSteamID().m_SteamID,"Item ownership");
                        var handle=SteamUGC.StartItemUpdate(new AppId_t(App),id);
                        Require(SteamUGC.SetItemVisibility(handle,ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic),"Public visibility");
                        var result=Wait<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(handle,"Public regional alpha release"));
                        Result(result.m_eResult);
                        Require(!result.m_bUserNeedsToAcceptWorkshopLegalAgreement,"Workshop legal agreement already accepted");
                        Console.WriteLine("Published existing item "+id);
                    }
                    return 0;
                }
                finally {SteamUGC.ReleaseQueryUGCRequest(query);}
            }
            if(args.Length==2 && (args[0]=="inspect" || args[0]=="subscribe" || args[0]=="unsubscribe"))
            {
                var id=new PublishedFileId_t(ulong.Parse(args[1]));
                if(args[0]=="unsubscribe")
                    Result(Wait<RemoteStorageUnsubscribePublishedFileResult_t>(SteamUGC.UnsubscribeItem(id)).m_eResult);
                if(args[0]=="subscribe")
                {
                    Result(Wait<RemoteStorageSubscribePublishedFileResult_t>(SteamUGC.SubscribeItem(id)).m_eResult);
                    Require(SteamUGC.DownloadItem(id,true),"DownloadItem");
                }
                Console.WriteLine("state="+SteamUGC.GetItemState(id));
                if(SteamUGC.GetItemInstallInfo(id,out ulong size,out string folder,4096,out uint stamp))
                    Console.WriteLine("installed="+folder+" bytes="+size);
                return 0;
            }
            if(args.Length!=5 || args[0]!="upload")throw new ArgumentException("upload CONTENT ID_FILE PREVIEW DESCRIPTION | inspect ID | subscribe ID");
            string content=Path.GetFullPath(args[1]),idFile=Path.GetFullPath(args[2]);
            Require(File.Exists(Path.Combine(content,"manifest.json")) && File.Exists(Path.Combine(content,"plugins","CraftopiaArchipelago","CraftopiaArchipelago.dll")),"Clean packaged content in official loader layout");
            Require(File.Exists(args[3]) && File.Exists(args[4]),"Preview and description");
            PublishedFileId_t item;
            if(File.Exists(idFile))item=new PublishedFileId_t(ulong.Parse(File.ReadAllText(idFile).Trim()));
            else
            {
                var created=Wait<CreateItemResult_t>(SteamUGC.CreateItem(new AppId_t(App),EWorkshopFileType.k_EWorkshopFileTypeCommunity));
                Result(created.m_eResult);item=created.m_nPublishedFileId;
                // Persist immediately so a later upload failure cannot create duplicate listings.
                File.WriteAllText(idFile,item.ToString());
                Console.WriteLine("created="+item+" agreementRequired="+created.m_bUserNeedsToAcceptWorkshopLegalAgreement);
                if(created.m_bUserNeedsToAcceptWorkshopLegalAgreement)throw new IOException("Accept the Workshop legal agreement in Steam before retrying this same item.");
            }
            Console.WriteLine("Updating private item "+item);
            var update=SteamUGC.StartItemUpdate(new AppId_t(App),item);
            Require(SteamUGC.SetItemVisibility(update,ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate),"Private visibility");
            const string title="Craftopia Archipelago - Regional Alpha";
            Require(SteamUGC.SetItemTitle(update,title),"Title");
            Require(SteamUGC.SetItemDescription(update,File.ReadAllText(args[4])),"Description");
            File.WriteAllText(Path.Combine(content,"modInfo.txt"),item+"\n"+title+"\nmod");
            Require(SteamUGC.SetItemContent(update,content),"Content");
            Require(SteamUGC.SetItemPreview(update,Path.GetFullPath(args[3])),"Preview");
            var submitted=Wait<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(update,"Private alpha installation validation"));
            Result(submitted.m_eResult);
            Console.WriteLine("Upload accepted; agreementRequired="+submitted.m_bUserNeedsToAcceptWorkshopLegalAgreement);
            var dependency=Wait<AddUGCDependencyResult_t>(SteamUGC.AddDependency(item,new PublishedFileId_t(2519948110))).m_eResult;
            // Re-uploading the same item retains its dependency; Steam reports duplicate.
            if(dependency!=EResult.k_EResultDuplicateRequest)Result(dependency);
            Console.WriteLine("Private upload complete: https://steamcommunity.com/sharedfiles/filedetails/?id="+item);
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}
        finally {SteamAPI.Shutdown();}
    }
}
