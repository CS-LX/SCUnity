using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ApiFixture;

public static class Initializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        string? marker = Environment.GetEnvironmentVariable("SC_API_SNAPSHOT_PROBE");
        if (marker != null) File.WriteAllText(marker, "Game code was executed!");
    }
}

public interface IProbe { void Invoke(); }

public class Probe<T> : IProbe where T : class, new()
{
    public class Nested { }
    protected class DerivedVisible { }
    private class Hidden { public int Leak; }
#if MUTATION
    public long Value;
    public int ReadOnly { get; set; }
    public virtual long Transform(ref int value, string label = "changed") => value;
    public U Create<U>() where U : struct => default;
#else
    public int Value;
    public int ReadOnly { get; private set; }
    public virtual int Transform(ref int value, string label = "original") => value;
    public U Create<U>() where U : class, new() => new();
#endif
    public event Action? Changed;
    public int this[int index] => index;
    protected virtual void ForDerived() { }
    private protected void AssemblyOnly() { }
    private void HiddenMethod() { }
    void IProbe.Invoke() { }
    public unsafe void Pointer(delegate* unmanaged[Cdecl]<int, int> callback) { }
    public void Out(out int value) => value = 1;
    public void In(in int value) { }
    public void RefReadOnly(ref readonly int value) { }
    public void Variadic(params string[] values) { }
    public decimal DecimalDefault(decimal amount = 1.25m) => amount;
    public int Implementation()
    {
#if PRIVATE_CHANGE
        return 200;
#else
        return 100;
#endif
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 2, Size = 16)]
public struct Packed
{
    [FieldOffset(0)] public int Value;
#if MUTATION
    [FieldOffset(8)] private long storage;
#else
    [FieldOffset(4)] private long storage;
#endif
}

public enum Mode : long
{
#if MUTATION
    Answer = 43
#else
    Answer = 42
#endif
}

internal class InternalContainer { public class HiddenNested { } }

[StructLayout(LayoutKind.Sequential)]
public struct Sequential
{
#if MUTATION
    private long second;
    public int first;
#else
    public int first;
    private long second;
#endif
}
