using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ChatMethods;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox;
public unsafe static class TaskAddItemsToTrade
{
    public static void Enqueue(IEnumerable<QueueEntry> Entries, int gil)
    {
        P.TaskManager.Enqueue(delegate { TradeTask.ConfirmAllowed = false; }, "ConfirmAllowed = false");
        P.TaskManager.Enqueue(() => TradeTask.UseTradeOn(new Sender((PlayerCharacter)Svc.Targets.FocusTarget).ToString()), $"UseTradeOn({Svc.Targets.FocusTarget})");
        P.TaskManager.Enqueue(TradeTask.WaitUntilTradeOpen);
        if (gil > 0)
        {
            P.TaskManager.Enqueue(TradeTask.OpenGilInput);
            P.TaskManager.Enqueue(() => TradeTask.SetNumericInput(gil), $"SetNumericInput({gil})");
        }
        foreach (var entry in Entries)
        {
            P.TaskManager.Enqueue(() =>
            {
                if (TryGetAddonByName<AtkUnitBase>("Trade", out var addon) && IsAddonReady(addon))
                {
                    if (TradeTask.GenericThrottle() && EzThrottler.Throttle("OfferTrade", 250))
                    {
                        P.Memory.SafeOfferItemTrade(entry.Type, (ushort)entry.SlotID);
                        return true;
                    }
                }
                return false;
            }, "OfferItemTask");
            if (Utils.GetSlot(entry.Type, entry.SlotID).Quantity > 1)
            {
                var amount = Math.Min(Utils.GetSlot(entry.Type, entry.SlotID).Quantity, entry.Quantity);
                if (amount < 1) throw new ArgumentOutOfRangeException();
                P.TaskManager.Enqueue(() => TradeTask.SetNumericInput((int)amount));
            }
        }
        P.TaskManager.Enqueue(delegate { TradeTask.ConfirmAllowed = true; }, "ConfirmAllowed = true");
        P.TaskManager.Enqueue(TradeTask.WaitUntilTradeNotOpen);
        P.TaskManager.DelayNext(15, true);
    }
}
