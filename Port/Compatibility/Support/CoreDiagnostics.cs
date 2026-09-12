namespace SCUnity.Compatibility
{
    // The foundation harness has no window. Retain the log title diagnostic until
    // the full Unity window host connects this side effect to its desktop title.
    internal static class CoreDiagnostics
    {
        internal static string TitleSuffix;
    }
}
