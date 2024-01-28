using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropbox;
public struct ItemDescriptor : IEquatable<ItemDescriptor>
{
    public int Id;
    public bool HQ;

    public ItemDescriptor(int id, bool hQ)
    {
        this.Id = id;
        this.HQ = hQ;
    }
    public ItemDescriptor(uint id, bool hQ)
    {
        this.Id = (int)id;
        this.HQ = hQ;
    }

    public override bool Equals(object obj)
    {
        return obj is ItemDescriptor descriptor && this.Equals(descriptor);
    }

    public bool Equals(ItemDescriptor other)
    {
        return this.Id == other.Id &&
               this.HQ == other.HQ;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Id, this.HQ);
    }

    public override readonly string ToString()
    {
        return $"[{ExcelItemHelper.GetName((uint)this.Id, true)},{this.HQ}]";
    }

    public static bool operator ==(ItemDescriptor left, ItemDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemDescriptor left, ItemDescriptor right)
    {
        return !(left == right);
    }
}
