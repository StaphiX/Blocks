using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortHash : IComparable<ShortHash>
{
    public const UInt16 DEFAULT = 0;
    public UInt16 data;

    public ShortHash(UInt16 value)
    {
        this.data = value;
    }

    public static UInt16 CalculateHash(string hashString)
    {
        UInt64 hashedValue = 3074457345618258791ul;
        for (int i = 0; i < hashString.Length; i++)
        {
            hashedValue += hashString[i];
            hashedValue *= 3074457345618258799ul;
        }

        hashedValue = hashedValue == DEFAULT ? DEFAULT+1 : hashedValue;

        return unchecked((ushort)(hashedValue & 0xffff));
    }

    public override string ToString()
    {
        return data.ToString("X");
    }

    int IComparable<ShortHash>.CompareTo(ShortHash other)
    {
        return data.CompareTo(other.data);
    }

    public static implicit operator ShortHash(int value)
    {
        return new ShortHash(unchecked((UInt16)value));
    }

    public static ShortHash operator +(ShortHash shortHash, int value)
    {
        UInt16 addValue = (UInt16)(value % UInt16.MaxValue);
        UInt16 data = shortHash.data;
        unchecked
        {
            data = (UInt16)(shortHash.data + addValue);
        }

        shortHash.data = data;
        return shortHash;
    }

    public static ShortHash operator ++(ShortHash shortHash)
    {
        unchecked
        {
            shortHash.data++;
        }
        return shortHash;
    }

    public static ShortHash operator -(ShortHash shortHash, int value)
    {
        UInt16 subValue = (UInt16)(value % UInt16.MaxValue);
        UInt16 data = shortHash.data;
        unchecked
        {
            data = (UInt16)(shortHash.data - subValue);
        }

        shortHash.data = data;
        return shortHash;
    }

    public static ShortHash operator --(ShortHash shortHash)
    {
        unchecked
        {
            shortHash.data--;
        }
        return shortHash;
    }
}
