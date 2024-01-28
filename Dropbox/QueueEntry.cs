using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox;
public record struct QueueEntry
{
    public InventoryType Type;
    public int SlotID;
    public int Quantity;

    public QueueEntry(InventoryType type, int slotID, int quantity)
    {
        this.Type = type;
        this.SlotID = slotID;
        this.Quantity = quantity;
    }
}
