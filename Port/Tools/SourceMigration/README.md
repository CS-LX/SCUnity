# One-time source migration helper

This developer tool uses the pinned .NET 10 SDK's Roslyn syntax and semantic APIs.
It is **not** a Unity compiler, runtime dependency, or build step. Unity compiles
the checked-in source packages directly. Do not run this over the editable port.

The input is a disposable, verified Desktop-profile source tree. The output must
be a separate candidate directory. Reference resolution uses the verified Desktop
DLLs and Unity's reference framework. The tool lowers collection expressions using
their bound target types, captured primary-constructor parameters and field-backed
properties; it retains C# 10 globals. Unknown collection/spread forms fail for review.

```text
dotnet run --project Port/Tools/SourceMigration -- <profile-source> <desktop-plugins> <unity-framework> <syntax-report.json> <candidate-directory>
```

This is a candidate migration, not a general-purpose C# transpiler. C# 11 struct
defaulting, conditional assignments, unsigned shifts, old-compiler overload binding
and platform adaptations require explicit review. The committed port contains those
reviewed changes and has runtime and public-API gates. Never silently regenerate it
from this helper. See `Docs/UnitySourceIntegration.md` and the source branch history.

`--format-primary <candidate-directory> <syntax-report.json>` formats only the
constructors/types identified in that migration report. It is not a project formatter.
