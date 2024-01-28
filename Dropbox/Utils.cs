using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox;
public static unsafe class Utils
{
    public static InventoryItem GetSlot(InventoryType type, int slot)
    {
        var im = InventoryManager.Instance();
        var cont = im->GetInventoryContainer(type);
        return cont->Items[slot];
    }
}
