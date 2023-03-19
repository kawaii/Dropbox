using ClickLib.Bases;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox
{
    internal unsafe class ClickGeneric : ClickBase<ClickGeneric, AtkUnitBase>
    {
        public ClickGeneric(string name, nint addon) : base(name, addon)
        {
        }

        public void ClickButton(AtkComponentButton* button)
        {
            this.ClickAddonButton(button, 0);
        }
    }
}
