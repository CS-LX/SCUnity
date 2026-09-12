using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SCUnity.Validation
{
    // C# 9 or earlier: this bridge is compiled by Unity, not external Roslyn.
    public sealed class FoundationBehaviour : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name; public bool passed; }
        [Serializable] public sealed class Result
        {
            public string schema = "scunity-foundation-v1";
            public string unityVersion;
            public string platform;
            public bool isEditor;
            public bool isMono;
            public int pointerSize;
            public bool passed;
            public string error;
            public List<Check> checks = new List<Check>();
        }

        void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            var result = new Result
            {
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                isEditor = Application.isEditor,
                isMono = Type.GetType("Mono.Runtime") != null,
                pointerSize = IntPtr.Size
            };
            try
            {
                if (result.isEditor || !result.isMono || result.platform != "WindowsPlayer" || result.pointerSize != 8)
                    throw new InvalidOperationException("Requires a real Windows x64 Mono Player.");
                int payloadIndex = Array.IndexOf(args, "-portProbePayload");
                string payload = payloadIndex < 0 ? Path.Combine(Application.streamingAssetsPath, "Probe") : args[payloadIndex + 1];
                int corpusIndex = Array.IndexOf(args, "-foundationCorpus");
                if (corpusIndex < 0) throw new InvalidOperationException("Missing -foundationCorpus.");
                foreach (var check in Foundation.Entry.Run(args[corpusIndex + 1], Path.Combine(payload, "SCUnity.Foundation.Payload.dll.bytes")))
                    result.checks.Add(new Check { name = check.Key, passed = check.Value });
                result.passed = result.checks.Count > 0 && result.checks.TrueForAll(c => c.passed);
            }
            catch (Exception e) { result.error = e.ToString(); }
            int index = Array.IndexOf(args, "-portProbeResult");
            if (index < 0 || index + 1 >= args.Length)
                throw new InvalidOperationException("Missing -portProbeResult output path.");
            File.WriteAllText(args[index + 1], JsonUtility.ToJson(result, true));
            Debug.Log("SCUnity foundation: " + (result.passed ? "PASS" : "FAIL") + " " + result.error);
            Application.Quit(result.passed ? 0 : 1);
        }
    }
}
