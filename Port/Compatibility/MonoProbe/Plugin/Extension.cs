namespace SCUnity.Probe;

public sealed class Extension : ProbeExtension
{
    public override string Initialize() => Dependency.Read();
}
