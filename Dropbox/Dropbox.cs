using ClickLib.Clicks;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Memory;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Security.AccessControl;

namespace Dropbox
{
    public unsafe class Dropbox : IDalamudPlugin
    {
        public string Name => "Dropbox";

        bool Active = false;

        string TradeText => Svc.Data.GetExcelSheet<Addon>().GetRow(102223).Text.ExtractText();

        public Dropbox(DalamudPluginInterface i)
        {
            ECommonsMain.Init(i, this);
            Svc.Framework.Update += Framework_Update;
            EzConfigGui.Init(Draw);
            EzCmd.Add("/dropbox", EzConfigGui.Open);
            
        }


        private void Framework_Update(Dalamud.Game.Framework framework)
        {
            if(Active && Svc.Condition[ConditionFlag.TradeOpen])
            {
                {
                    if (TryGetAddonByName<AtkUnitBase>("Trade", out var addon) && IsAddonReady(addon))
                    {
                        var check = addon->UldManager.NodeList[31]->GetAsAtkComponentNode()->Component->UldManager.NodeList[0]->GetAsAtkImageNode();
                        var ready = check->AtkResNode.Color.A == 0xFF;
                        var tradeButton = (AtkComponentButton*)(addon->UldManager.NodeList[3]->GetComponent());
                        if (ready && tradeButton->IsEnabled && EzThrottler.Throttle("Delay", 200) && EzThrottler.Throttle("ReadyTrade", 2000))
                        {
                            PluginLog.Information($"Locking trade");
                            new ClickGeneric("Trade", (nint)addon).ClickButton(tradeButton);
                        }
                    }
                }
                {
                    var addon = GetSpecificYesno(TradeText);
                    if (addon != null && EzThrottler.Throttle("Delay", 200) && EzThrottler.Throttle("SelectYes", 2000))
                    {
                        PluginLog.Information($"Confirming trade");
                        ClickSelectYesNo.Using((nint)addon).Yes();
                    }
                }
            }
        }

        void Draw()
        {
            ImGuiEx.EzTabBar("Tabs",
                ("Main", () => { ImGui.Checkbox($"Enable auto-accept trades", ref Active); }, null, true),
                ("Log", InternalLog.PrintImgui, null, false)
            );
        }

        internal static AtkUnitBase* GetSpecificYesno(params string[] s)
        {
            for (int i = 1; i < 100; i++)
            {
                try
                {
                    var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", i);
                    if (addon == null) return null;
                    if (IsAddonReady(addon))
                    {
                        var textNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
                        var text = MemoryHelper.ReadSeString(&textNode->NodeText).ExtractText();
                        if (text.EqualsAny(s))
                        {
                            PluginLog.Verbose($"SelectYesno {s} addon {i}");
                            return addon;
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Log();
                    return null;
                }
            }
            return null;
        }


        public void Dispose()
        {
            Svc.Framework.Update -= Framework_Update;
            ECommonsMain.Dispose();
        }
    }
}