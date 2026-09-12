using System;
using System.IO;
using System.Text.Json;
var result = SCUnity.FlacValidation.Entry.Run(args[0], args[1]);
File.WriteAllText(Path.Combine(args[1], "checks.json"), JsonSerializer.Serialize(result));
Console.WriteLine("PASS: " + result.Count + " FLAC assertions using original .NET 10 package.");
