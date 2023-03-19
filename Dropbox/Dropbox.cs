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
using ECommons.Configuration;

namespace Dropbox
{
    public unsafe class Dropbox : IDalamudPlugin
    {
        public string Name => "Dropbox";
        string TradePartnerName = "";
        Config Config;
        bool Active = false;


        string TradeText => Svc.Data.GetExcelSheet<Addon>().GetRow(102223).Text.ExtractText();

        public Dropbox(DalamudPluginInterface i)
        {
            ECommonsMain.Init(i, this);
            Svc.Framework.Update += Framework_Update;
            Config = EzConfig.Init<Config>();
            EzConfigGui.Init(Draw);
            EzCmd.Add("/dropbox", EzConfigGui.Open);
            Svc.Chat.ChatMessage += Chat_ChatMessage;
        }

        private void Chat_ChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (((int)type).EqualsAny(313, 569))
            {
                var mStr = message.ToString();
                if (mStr.StartsWith("Trade request sent to") || mStr.EndsWith("wishes to trade with you."))
                {
                    PluginLog.Debug("Detected trade request");
                    foreach (var payload in message.Payloads)
                    {
                        if (payload.Type == PayloadType.Player)
                        {
                            var playerPayload = (PlayerPayload)payload;
                            var senderNameWithWorld = $"{playerPayload.PlayerName}@{playerPayload.World.Name}";
                            PluginLog.Debug($"Name trade out: {senderNameWithWorld}");
                            TradePartnerName = senderNameWithWorld;
                            Notify.Info($"You begin trade with {TradePartnerName}.");
                            break;
                        }
                    }
                }
            }
            if (type == XivChatType.SystemMessage)
            {
                var msg = message.ToString();
                if (msg.Equals("Trade complete."))
                {
                    Notify.Info($"You finished trade with {TradePartnerName}");
                    TradePartnerName = "";
                }
                else if (msg.Equals("Trade canceled."))
                {
                    Notify.Info("Trade canceled");
                    TradePartnerName = "";
                }
            }
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
                        if (ready)
                        {
                            if (EzThrottler.Check("TradeArtificialThrottle") && tradeButton->IsEnabled && EzThrottler.Throttle("Delay", 200) && EzThrottler.Throttle("ReadyTrade", 2000))
                            {
                                PluginLog.Information($"Locking trade");
                                new ClickGeneric("Trade", (nint)addon).ClickButton(tradeButton);
                            }
                        }
                        else
                        {
                            EzThrottler.Throttle("TradeArtificialThrottle", Config.Delay, true);
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
                ("Main", () => 
                { 
                    ImGui.Checkbox($"Enable auto-accept trades", ref Active);
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.SliderIntAsFloat("Delay before accepting, s", ref Config.Delay, 0, 10000);
                }, null, true),
                InternalLog.ImGuiTab(),
                ("Debug", () =>
                {
                    EzThrottler.ImGuiPrintDebugInfo();
                }, ImGuiColors.DalamudGrey, true)
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
            Svc.Chat.ChatMessage -= Chat_ChatMessage;
            ECommonsMain.Dispose();
        }
    }
}