using System;
using CraftopiaArchipelago;

static class ReadTests
{
    class Item
    {
        public int id,status,wpCategory,equipSlot,workbenchId,workbenchLv,existPrefab,inventoryTabId;
        public bool isOldData,cantUse;
        public Item(EquipmentSnapshot s) {
            id=s.Id;status=s.Status;wpCategory=s.WeaponCategory;equipSlot=s.Slot;
            workbenchId=s.Workbench;workbenchLv=s.WorkbenchLevel;existPrefab=s.Prefab;
            inventoryTabId=s.InventoryTab;isOldData=s.OldData;cantUse=s.CannotUse;
        }
    }
    class Entry { public int equipSlot,itemId; public bool isOld; }
    static void Check(bool value) { if(!value) throw new Exception("Equipment adapter regression"); }
    static void Reject(Action read) { try { read(); } catch { return; } throw new Exception("Unreadable adapter accepted"); }
    public static void Run(EquipmentCatalog catalog)
    {
        var sword=new Item(catalog.Snapshot(44));
        var copper=new Item(catalog.Snapshot(45));
        var live=new[]{sword,copper};
        Check(EquipmentRead.Item(sword).Id==44);
        Reject(()=>EquipmentRead.Item(null));
        Reject(()=>EquipmentRead.Item(new object()));
        Check(EquipmentRead.Loadout(new[]{new Entry {equipSlot=0,itemId=0}},live).Count==0);
        Reject(()=>EquipmentRead.Loadout(new[]{new Entry {equipSlot=0,itemId=44,isOld=true}},live));
        Reject(()=>EquipmentRead.Loadout(new[]{new Entry {equipSlot=0,itemId=999999}},live));
        Reject(()=>EquipmentRead.Loadout(new[]{new Entry {equipSlot=0,itemId=-1}},live));
        Reject(()=>EquipmentRead.Loadout(new[]{new Entry {equipSlot=10,itemId=44}},live));
        Reject(()=>EquipmentRead.Loadout(new[]{new Entry {equipSlot=0,itemId=44},new Entry {equipSlot=0,itemId=45}},live));
        Reject(()=>EquipmentRead.Loadout(new Entry[0],new[]{sword,sword}));
        Reject(()=>EquipmentRead.Loadout(null,live));
        var permissions=new PermissionManager(null,null);
        var session=new EquipmentSession(true,-123,50,"slot");
        Check(session.Bind(50,"slot",EquipmentCatalog.CatalogId,1,0));
        EquipmentSnapshot vanilla=null;
        for(int id=1;id<10000 && vanilla==null;id++)
            if(catalog.Decision(id)=="vanilla") vanilla=catalog.Snapshot(id);
        Check(vanilla!=null);
        foreach(var items in new[]{new EquipmentSnapshot[0],new[]{vanilla}})
        {
            Func<bool> gear=()=>EquipmentEnforcement.AllowLoadout(true,catalog,null,()=>items,()=>0,null);
            Check(gear());
            Check(!session.Admit(session.Ready(123,50,"slot",true),gear));
        }
        permissions.UnlockEquipmentTier("Sword",1);
        int transfers=0;
        var entries=new[]{new Entry {equipSlot=0,itemId=44},new Entry {equipSlot=1,itemId=45}};
        if(EquipmentEnforcement.AllowLoadout(true,catalog,permissions,
            ()=>EquipmentRead.Loadout(entries,live),()=>1,_=>throw new Exception())) transfers++;
        Check(transfers==0); // A later denied entry prevents the whole original callback.
        permissions.UnlockEquipmentTier("Sword",2);
        if(EquipmentEnforcement.AllowLoadout(true,catalog,permissions,
            ()=>EquipmentRead.Loadout(entries,live),()=>1,null)) transfers++;
        Check(transfers==1);
        Console.WriteLine("PASS equipment field/loadout adapters");
    }
}
