using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.Automation;
using ECommons.ChatMethods;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox
{
    internal unsafe static class TradeTask
    {

        internal static bool IsActive => P.TaskManager.IsBusy;

        internal static bool GenericThrottle(bool rethrottle = false) => FrameThrottler.Throttle("TaskThrottle", 15, rethrottle);

        internal static volatile bool ConfirmAllowed = false;

        internal static void Enqueue(TradeQueueEntry entry)
        {
            P.TaskManager.Enqueue(delegate { ConfirmAllowed = false; }, "ConfirmAllowed = false");
            P.TaskManager.Enqueue(() => UseTradeOn(entry.Player), $"UseTradeOn({entry.Player})");
            P.TaskManager.Enqueue(WaitUntilTradeOpen);
            P.TaskManager.Enqueue(OpenGilInput);
            P.TaskManager.Enqueue(() => SetNumericInput(entry.Gil), $"SetNumericInput({entry.Gil})");
            P.TaskManager.DelayNext(15, true);
            P.TaskManager.Enqueue(delegate { ConfirmAllowed = true; }, "ConfirmAllowed = true");
            P.TaskManager.Enqueue(WaitUntilTradeNotOpen);
            P.TaskManager.Enqueue(() => C.TradeQueue.RemoveAll(x => x.GUID == entry.GUID));
            P.TaskManager.DelayNext(15, true);
        }

        internal static bool? WaitUntilTradeOpen() => Svc.Condition[ConditionFlag.TradeOpen];
        internal static bool? WaitUntilTradeNotOpen() => !Svc.Condition[ConditionFlag.TradeOpen];

        internal static bool? UseTradeOn(string player)
        {
            var targetPlayer = Svc.Objects.Where(x => x is PlayerCharacter pc && pc.IsTargetable && new Sender(pc).ToString() ==  player).FirstOrDefault();
            if(targetPlayer != null)
            {
                if(Svc.Targets.Target?.Address == targetPlayer.Address)
                {
                    if(GenericThrottle() && EzThrottler.Throttle("TradeOpen", 3000))
                    {
                        Chat.Instance.SendMessage("/trade");
                        return true;
                    }
                }
                else
                {
                    if (GenericThrottle())
                    {
                        Svc.Targets.Target = targetPlayer;
                    }
                }
            }
            return false;
        }

        internal static bool? OpenGilInput()
        {
            if(TryGetAddonByName<AtkUnitBase>("Trade", out var addon) && IsAddonReady(addon))
            {
                if (GenericThrottle())
                {
                    Callback.Fire(addon, true, 2, Callback.ZeroAtkValue);
                    return true;
                }
            }
            else
            {
                GenericThrottle(true);
            }
            return false;
        }

        internal static bool? SetNumericInput(int num)
        {
            if (num < 0 || num > 1000000) throw new ArgumentOutOfRangeException(nameof(num));
            if (TryGetAddonByName<AtkUnitBase>("InputNumeric", out var addon) && IsAddonReady(addon))
            {
                if (GenericThrottle())
                {
                    Callback.Fire(addon, true, num);
                    return true;
                }
            }
            else
            {
                GenericThrottle(true);
            }
            return false;
        }
    }
}
