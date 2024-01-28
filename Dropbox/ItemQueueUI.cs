using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Reflection.Metadata.Ecma335;

namespace Dropbox;
public unsafe static class ItemQueueUI
{
    public static List<QueueEntry> TradeQueue = [];
    static Dictionary<ItemDescriptor, Box<int>> ItemQuantities = [];
    static bool OnlySelected = false;
    static string Filter = "";
    public static void Draw()
    {
        if (P.TaskManager.IsBusy)
        {
            if (ImGui.Button("Stop"))
            {
                P.TaskManager.Abort();
            }
            ImGuiEx.Text($"Processing task: \n{P.TaskManager.CurrentTaskName}");
            return;
        }
        ImGuiEx.Text("Select items to trade:");
        ImGuiEx.SetNextItemWidthScaled(200f);
        ImGui.InputTextWithHint("##filter", "Search", ref Filter, 100);
        ImGui.SameLine();
        ImGui.Checkbox("Show only selected", ref OnlySelected);
        List<ImGuiEx.EzTableEntry> Entries = [];
        foreach (var x in GetTradeableItems())
        {
            if (!ItemQuantities.ContainsKey(x.Descriptor)) ItemQuantities[x.Descriptor] = new(0);
            var text = ExcelItemHelper.GetName((uint)x.Descriptor.Id);
            if (x.Descriptor.HQ) text += "";
            if (Filter != "" && !text.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
            if (OnlySelected && ItemQuantities[x.Descriptor].Value <= 0) continue;
            Entries.Add(new("##icon", () =>
            {
                if(ThreadLoadImageHandler.TryGetIconTextureWrap(ExcelItemHelper.Get(x.Descriptor.Id).Icon, false, out var tex)) 
                {
                    ImGui.Image(tex.ImGuiHandle, new Vector2(24));
                }
            }));
            Entries.Add(new("Quantity", () =>
            {
                ImGuiEx.SetNextItemWidthScaled(120f);
                ImGui.DragInt($"##quantity{x.Descriptor}", ref ItemQuantities[x.Descriptor].Value, 1f, 0, (int)x.Count);
                ImGui.SameLine();
                ImGuiEx.Text($"/ {x.Count}");
            }));
            Entries.Add(new("Name", () =>
            {
                ImGuiEx.Text(ItemQuantities[x.Descriptor].Value > 0? ImGuiColors.ParsedGreen:null, text);
            }));
        }
        ImGuiEx.EzTable(Entries);
        PurgeSelection();
        if(Svc.Targets.FocusTarget is PlayerCharacter pc)
        {
            if(ImGui.Button($"Begin trading with {pc.Name}"))
            {
                var quantitiesCopy = ItemQuantities.ToDictionary(x => x.Key, x => x.Value.Clone());
                int gil = 0;
                foreach (var item in quantitiesCopy)
                {
                    if(item.Key.Id == 1)
                    {
                        gil += item.Value.Value;
                        continue;
                    }
                    var im = InventoryManager.Instance();
                    foreach (var type in ValidInventories)
                    {
                        var cont = im->GetInventoryContainer(type);
                        for (int i = 0; i < cont->Size; i++)
                        {
                            var slot = cont->Items[i];
                            if (slot.ItemID == item.Key.Id && slot.Flags.HasFlag(InventoryItem.ItemFlags.HQ) == item.Key.HQ && item.Value.Value > 0)
                            {
                                var quantity = item.Value.Value < slot.Quantity ? item.Value.Value : (int)slot.Quantity;
                                PluginLog.Information($"Enqueueing slot {i} of {type} ({ExcelItemHelper.GetName(slot.ItemID, true)}) with quantity {quantity}");
                                item.Value.Value -= quantity;
                                TradeQueue.Add(new(type, i, quantity));
                            }
                        }
                    }
                }

                while(TradeQueue.Count > 0 || gil > 0)
                {
                    List<QueueEntry> entries = [];
                    for (int i = 0; i < 5; i++)
                    {
                        if (TradeQueue.TryDequeue(out var result))
                        {
                            entries.Add(result);
                        }
                    }
                    var tradeGil = Math.Min(TradeTask.MaxGil, gil);
                    gil -= tradeGil;
                    TaskAddItemsToTrade.Enqueue(entries, tradeGil);
                }
            }
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, "Focus target your trade partner to begin trading.");
        }
    }

    public static void PurgeSelection()
    {
        var items = GetTradeableItems();
        foreach (var x in ItemQuantities)
        {
            if(items.TryGetFirst(z => z.Descriptor == x.Key, out var v))
            {
                if (x.Value.Value > v.Count) x.Value.Value = (int)v.Count;
            }
            else
            {
                new TickScheduler(() => ItemQuantities.Remove(x.Key));
            }
        }
    }

    public static readonly InventoryType[] ValidInventories = [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4, InventoryType.Crystals];

    public static List<ItemRecord> GetTradeableItems()
    {
        var ret = new List<ItemRecord>();
        var im = InventoryManager.Instance();
        ret.Add(new(1, false, (uint)im->GetInventoryItemCount(1)));
        foreach (var inv in ValidInventories)
        {
            var cont = im->GetInventoryContainer(inv);
            for (var i = 0u; i < cont->Size; i++)
            {
                var item = cont->Items[i];
                if(item.ItemID != 0 && item.Spiritbond == 0 && P.TradeableItems.Contains(item.ItemID))
                {
                    if(ret.TryGetFirst(x=> x.Descriptor.Id == item.ItemID && x.Descriptor.HQ == item.Flags.HasFlag(InventoryItem.ItemFlags.HQ), out var itemRecord))
                    {
                        itemRecord.Count += item.Quantity;
                    }
                    else
                    {
                        ret.Add(new(item.ItemID, item.Flags.HasFlag(InventoryItem.ItemFlags.HQ), item.Quantity));
                    }
                }
            }
        }
        return ret;
    }

    public class ItemRecord
    {
        public ItemDescriptor Descriptor;
        public uint Count;

        public ItemRecord(uint item, bool isHQ, uint count)
        {
            this.Descriptor = new(item, isHQ);
            this.Count = count;
        }
    }
}
