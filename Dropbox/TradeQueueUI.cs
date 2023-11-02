using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ChatMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox
{
    internal static class TradeQueueUI
    {
        static TradeQueueEntry NewEntry = new();
        internal static void Draw()
        {
            if (P.TaskManager.IsBusy)
            {
                ImGuiEx.Text(EColor.Green, $"{P.TaskManager.NumQueuedTasks} steps remaining");
                ImGui.SameLine();
                if (ImGui.SmallButton("Stop"))
                {
                    P.TaskManager.Abort();
                }
            }
            else
            {
                ImGuiEx.Text($"No tasks are running");
                ImGui.SameLine();
                if(ImGui.SmallButton("Run all"))
                {
                    foreach(var x in C.TradeQueue)
                    {
                        TradeTask.Enqueue(x);
                    }
                }
            }
            ImGui.SetNextItemWidth(200f);
            {
                ImGui.InputTextWithHint("##name", Svc.Targets.Target is PlayerCharacter pc ? new Sender(pc).ToString() : "Player Name@World", ref NewEntry.Player, 100);
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("Gil", ref NewEntry.Gil.ValidateRange(0, int.MaxValue), 100000, 100000);
            ImGui.SameLine();
            if (ImGui.Button("Add"))
            {
                if (NewEntry.Player == "" && Svc.Targets.Target is PlayerCharacter pc) NewEntry.Player = new Sender(pc).ToString();
                var n = NewEntry.JSONClone();
                var gil = n.Gil;
                while (gil > 1000000)
                {
                    n.Gil = 1000000;
                    gil -= 1000000;
                    C.TradeQueue.Add(n.JSONClone());
                }
                if(gil > 0)
                {
                    n.Gil = gil;
                    C.TradeQueue.Add(n.JSONClone());
                }
                Notify.Info($"Added successfully");
            }

            for (int i = 0; i < C.TradeQueue.Count; i++)
            {
                var x = C.TradeQueue[i];
                ImGui.PushID(x.GUID);

                if (ImGui.ArrowButton("##up", ImGuiDir.Up) && i > 0)
                {
                    (C.TradeQueue[i], C.TradeQueue[i - 1]) = (C.TradeQueue[i - 1], C.TradeQueue[i]);
                }
                ImGui.SameLine();
                if (ImGui.ArrowButton("##down", ImGuiDir.Down) && i < C.TradeQueue.Count - 1)
                {
                    (C.TradeQueue[i], C.TradeQueue[i + 1]) = (C.TradeQueue[i + 1], C.TradeQueue[i]);
                }
                ImGui.SameLine();

                ImGuiEx.Text($"{x.Player}: {x.Gil:N0}");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    var t = i;
                    new TickScheduler(() => C.TradeQueue.RemoveAt(t));
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.StepForward))
                {
                    TradeTask.Enqueue(x);
                }

                ImGui.PopID();
            }
        }
    }
}
