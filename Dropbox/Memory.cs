using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox;
public unsafe class Memory
{
    delegate void OfferItemTrade(nint tradeAddress, ushort slot, InventoryType type);
    [EzHook("48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 83 B9 ?? ?? ?? ?? ?? 41 8B F0", false)]
    EzHook<OfferItemTrade> OfferItemTradeHook;

    public Memory()
    {
        EzSignatureHelper.Initialize(this);
    }

    void OfferItemTradeDetour(nint tradeAddress, ushort slot, InventoryType type)
    {
        throw new NotImplementedException();
    }

    public void SafeOfferItemTrade(InventoryType type, ushort slot)
    {
        nint TradeAddress = ((nint)UIModule.Instance()->GetAgentModule()->GetAgentByInternalId(AgentId.Trade)) + 40;
        if(Utils.GetSlot(type, slot).ItemID == 0)
        {
            throw new InvalidOperationException($"Attempted to use trade on empty slot {type}, {slot}");
        }
        OfferItemTradeHook.Delegate(TradeAddress, slot, type);
    }
}
