using System.Reflection;
using System.Text.Json;
// Resolve original reference dependencies without launching the original application.
Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "Engine.dll"));
var checks = SCUnity.Foundation.Entry.Run(args[0], args[1]);
File.WriteAllText(Path.Combine(args[0], "checks.json"), JsonSerializer.Serialize(checks));
Console.WriteLine("PASS: " + checks.Count + " original-assembly assertions.");
