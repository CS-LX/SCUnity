using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SixLabors.ImageSharp {
internal static class MonoCompat {
    public static void ThrowIfNull([System.Diagnostics.CodeAnalysis.NotNull] object? value, [CallerArgumentExpression("value")] string? name=null) { if(value is null) throw new ArgumentNullException(name); }
    public static ref T GetArrayDataReference<T>(T[] array) => ref MemoryMarshal.GetReference(array.AsSpan());
    public static byte[] HashData(byte[] data) { using(var md5=MD5.Create()) return md5.ComputeHash(data); }
    public static Vector<TTo> As<TFrom,TTo>(this Vector<TFrom> value) where TFrom:struct where TTo:struct => Unsafe.As<Vector<TFrom>,Vector<TTo>>(ref value);
    public static void Clear(Array array,int start,int length) => Array.Clear(array,start,length);
    public static void Clear(Array array) => Array.Clear(array,0,array.Length);
    public static void Sort<T>(this Span<T> values) { var a=values.ToArray(); Array.Sort(a); a.AsSpan().CopyTo(values); }
    public static void Sort<T>(this Span<T> values, Comparison<T> compare) { var a=values.ToArray(); Array.Sort(a,compare); a.AsSpan().CopyTo(values); }
    public static ushort PackHalf(float value) {
        uint v=Unsafe.As<float,uint>(ref value), sign=(v>>16)&0x8000, mantissa=v&0x7fffff; int exponent=(int)((v>>23)&255);
        if(exponent==255) return (ushort)(sign|0x7c00|(mantissa==0?0:(mantissa>>13)|0x200));
        if(exponent>142) return (ushort)(sign|0x7c00);
        if(exponent<113) {
            if(exponent<102) return (ushort)sign;
            mantissa|=0x800000; int shift=126-exponent; uint half=mantissa>>shift, remainder=mantissa&((1u<<shift)-1), midpoint=1u<<(shift-1);
            if(remainder>midpoint || (remainder==midpoint && (half&1)!=0))half++;
            return (ushort)(sign|half);
        }
        uint result=(uint)(exponent-112)<<10 | (mantissa>>13), tail=mantissa&0x1fff;
        if(tail>0x1000 || (tail==0x1000 && (result&1)!=0))result++;
        return (ushort)(sign|result);
    }
    public static float UnpackHalf(ushort value) {
        uint sign=(uint)(value&0x8000)<<16,mantissa=(uint)value&1023; int exponent=(value>>10)&31; uint bits;
        if(exponent==0) {
            if(mantissa==0)bits=sign;
            else {int e=-14; while((mantissa&1024)==0){mantissa<<=1;e--;} bits=sign | ((uint)(e+127)<<23) | ((mantissa&1023)<<13);}
        } else if(exponent==31) bits=sign|0x7f800000|(mantissa<<13)|(mantissa==0?0:0x400000u);
        else bits=sign|((uint)(exponent+112)<<23)|(mantissa<<13);
        return Unsafe.As<uint,float>(ref bits);
    }
}
internal struct MonoMemoryInfo {
    public long TotalAvailableMemoryBytes,MemoryLoadBytes,HighMemoryLoadThresholdBytes;
    [StructLayout(LayoutKind.Sequential)] struct Status {public uint Length,Load;public ulong Total,Available,TotalPage,AvailablePage,TotalVirtual,AvailableVirtual,Extended;}
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GlobalMemoryStatusEx(ref Status data);
    public static MonoMemoryInfo Get() {
        var s=new Status {Length=(uint)Marshal.SizeOf(typeof(Status))};
        if(!GlobalMemoryStatusEx(ref s)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return new MonoMemoryInfo{TotalAvailableMemoryBytes=(long)s.Total,MemoryLoadBytes=(long)(s.Total-s.Available),HighMemoryLoadThresholdBytes=(long)(s.Total*9/10)};
    }
}
}
namespace System.Numerics {
internal static class BitOperations {
    public static int LeadingZeroCount(uint value) {int n=0; if(value==0)return 32; while((value&0x80000000)==0){n++;value<<=1;}return n;}
    public static int LeadingZeroCount(ulong value) => (value>>32)==0?32+LeadingZeroCount((uint)value):LeadingZeroCount((uint)(value>>32));
    public static int TrailingZeroCount(int value) => TrailingZeroCount((uint)value);
    public static int TrailingZeroCount(uint value) {int n=0; if(value==0)return 32;while((value&1)==0){n++;value>>=1;}return n;}
    public static int TrailingZeroCount(ulong value) => (uint)value==0?32+TrailingZeroCount((uint)(value>>32)):TrailingZeroCount((uint)value);
    public static int Log2(uint value) => value==0?0:31-LeadingZeroCount(value);
    public static int Log2(ulong value) => value==0?0:63-LeadingZeroCount(value);
    public static uint RotateLeft(uint value,int shift) => (value<<(shift&31))|(value>>((-shift)&31));
    public static uint RotateRight(uint value,int shift) => (value>>(shift&31))|(value<<((-shift)&31));
    public static int PopCount(uint value) {int n=0;while(value!=0){n++;value&=value-1;}return n;}
}
}
