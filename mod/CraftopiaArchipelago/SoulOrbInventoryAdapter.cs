using System;
using System.Reflection;
using Oc.Item;
using Oc.Item.UI;

namespace CraftopiaArchipelago
{
    // Used only by the negotiated experimental currency runtime.
    // Main-thread only; caller must durably record intent BEFORE invoking TryApply.
    internal static class SoulOrbInventoryAdapter
    {
        const int ItemId=3840;
        static readonly MethodInfo insert=typeof(OcItemUI_InventoryMng).GetMethod("TryTakeItem_toInventory",
            BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(OcItemStack),
                typeof(OcItemUI_InventoryMng.ItemTakeTrigger),typeof(bool)},null);
        internal static int Count() {
            var inventory=OcItemUI_InventoryMng.InstUISS;
            var data=OcItemDataMng.Inst.GetItemData_OrNull(ItemId,null);
            if(inventory==null || data==null) throw new InvalidOperationException("Currency inventory unavailable");
            return inventory.GetItemCount(data,false,true);
        }

        internal static CurrencyMutation TryApply(int amount) {
            // First implementation deliberately grants single currency units.
            // One-unit insertion avoids partial batches under full inventory.
            if(amount!=1 || insert==null) return CurrencyMutation.NoMutation;
            OcItemUI_InventoryMng inventory;
            ItemData data;
            OcItemStack_Temporary stack;
            try {
                inventory=OcItemUI_InventoryMng.InstUISS;
                data=OcItemDataMng.Inst.GetItemData_OrNull(ItemId,null);
                if(inventory==null || data==null) return CurrencyMutation.NoMutation;
                stack=new OcItemStack_Temporary(data,amount);
                if(stack.StackCount!=amount) return CurrencyMutation.NoMutation;
            } catch { return CurrencyMutation.NoMutation; }
            return CurrencyInventoryVerifier.Apply(amount,
                ()=>inventory.GetItemCount(data,false,true),
                n=> {
                    // Temporary stack + direct inventory method: no world-drop path.
                    // Verified enum value Inventory=3. Live inventory behavior still
                    // needs the controlled runtime test; no claim of saved delivery here.
                    insert.Invoke(inventory,new object[]{stack,OcItemUI_InventoryMng.ItemTakeTrigger.Inventory,false});
                    return stack.StackCount;
                });
        }
    }
}
