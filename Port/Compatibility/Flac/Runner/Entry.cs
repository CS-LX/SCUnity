using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.Flac;

namespace SCUnity.FlacValidation
{
    public static class Entry
    {
        static readonly Dictionary<string, bool> Checks = new Dictionary<string, bool>();
        static void Require(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
        }
        static void Check(string name) { Checks.Add(name, true); }
        static byte[] Sentinel(int count) => Enumerable.Repeat((byte)0x5C, count).ToArray();
        static int ReadAndRecord(FlacReader reader, BinaryWriter output, int count)
        {
            var buffer = Sentinel(count + 10);
            int read = reader.Read(buffer, 5, count);
            Require(read >= 0 && read <= count, "Invalid read count");
            Require(buffer.Take(5).All(b => b == 0x5C) && buffer.Skip(count + 5).All(b => b == 0x5C), "Buffer bounds changed");
            output.Write(count); output.Write(read); output.Write(reader.Position);
            // Include the complete requested buffer, even if the original decoder returns 0 after a partial final frame.
            output.Write(buffer);
            return read;
        }
        static byte[] Decode(string path, BinaryWriter output, out long durationTicks)
        {
            var stream = new MemoryStream(File.ReadAllBytes(path));
            byte[] pcm;
            using (var reader = new FlacReader(stream))
            {
                Require(reader.CanSeek && reader.Length > 0 && reader.Length <= 100 * 1024 * 1024, "Invalid decoded length: " + path);
                output.Write(reader.WaveFormat.Channels); output.Write(reader.WaveFormat.SampleRate);
                output.Write(reader.WaveFormat.BitsPerSample); output.Write(reader.Length);
                durationTicks = reader.TotalTime.Ticks;
                output.Write(reader.Metadata.Count);
                int length = checked((int)reader.Length);
                pcm = new byte[length];
                int read = reader.Read(pcm, 0, length);
                Require(read == length, "Full-length read incomplete: " + path + " " + read + "/" + length);
                output.Write(read); output.Write(pcm); output.Write(reader.Position);
                int align = reader.WaveFormat.BlockAlign;
                foreach (long requested in new long[] { 0, length / 3 / align * align, length / 2 / align * align, Math.Max(0, length - align * 17) })
                {
                    reader.Position = requested;
                    output.Write(requested); output.Write(reader.Position);
                    ReadAndRecord(reader, output, align * 17);
                }
                reader.Position = 0;
                foreach (int frames in new[] { 1, 7, 129, 4097, 3, 4097 }) ReadAndRecord(reader, output, align * frames);
            }
            Require(!stream.CanRead, "Decoder did not dispose its stream: " + path);
            return pcm;
        }
        static void InvalidInputs(BinaryWriter output)
        {
            var cases = new[] { new byte[0], new byte[] { 0x66, 0x4c, 0x61 }, new byte[] { 0x66, 0x4c, 0x61, 0x43 }, new byte[] { 1, 2, 3, 4 } };
            foreach (byte[] bytes in cases)
            {
                string error = null;
                try { using (var reader = new FlacReader(new MemoryStream(bytes))) { } }
                catch (EndOfStreamException e) { error = e.GetType().FullName; }
                catch (FlacException e) { error = e.GetType().FullName; }
                Require(error != null, "Invalid FLAC input accepted");
                output.Write(error);
            }
            bool nullRejected = false;
            try { using (var reader = new FlacReader((Stream)null)) { } }
            catch (ArgumentNullException) { nullRejected = true; }
            Require(nullRejected, "Null input accepted");
        }
        public static Dictionary<string, bool> Run(string fixtures, string outputDirectory)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Checks.Clear(); Directory.CreateDirectory(outputDirectory);
            string[] files = Directory.GetFiles(fixtures, "*.flac", SearchOption.AllDirectories)
                .OrderBy(p => p.Substring(fixtures.Length).Replace('\\', '/'), StringComparer.Ordinal).ToArray();
            Require(files.Length > 0, "Missing FLAC fixtures");
            int synthetic = 0; long pcmBytes = 0;
            var durations = new List<string>();
            using (var output = new BinaryWriter(File.Create(Path.Combine(outputDirectory, "corpus.bin"))))
            {
                output.Write(files.Length);
                foreach (string path in files)
                {
                    string name = path.Substring(fixtures.Length).Replace('\\', '/');
                    output.Write(name);
                    byte[] pcm = Decode(path, output, out long ticks); pcmBytes += pcm.Length;
                    durations.Add(name + "\t" + ticks);
                    string expected = Path.ChangeExtension(path, ".pcm");
                    if (File.Exists(expected))
                    {
                        Require(pcm.SequenceEqual(File.ReadAllBytes(expected)), "Known integer PCM differs: " + path);
                        synthetic++;
                    }
                }
                InvalidInputs(output);
            }
            Require(synthetic == 3, "All three known-PCM fixtures are required");
            Check("full-pcm-decode"); Check("known-integer-pcm"); Check("format-length-and-duration");
            Check("seek-behavior-recorded"); Check("chunk-reads-and-buffer-bounds"); Check("stream-disposal"); Check("invalid-inputs-rejected");
            string sample = files.First(p => File.Exists(Path.ChangeExtension(p, ".pcm")));
            bool finished = false;
            using (var reader = new FlacReader(new MemoryStream(File.ReadAllBytes(sample)), FlacPreScanMethodMode.Sync, _ => finished = true))
            {
                Require(finished, "Synchronous prescan callback missing");
                Require(reader.CanSeek && reader.Length > 0, "Synchronous prescan failed");
            }
            Check("sync-prescan-callback");
            // Repeat construction after disposal; catches stale native buffers or shared decoder state.
            using (var output = new BinaryWriter(File.Create(Path.Combine(outputDirectory, "repeat.bin"))))
                Require(Decode(sample, output, out _).SequenceEqual(File.ReadAllBytes(Path.ChangeExtension(sample, ".pcm"))), "Repeat decode differs");
            Check("repeat-construction");
            File.WriteAllText(Path.Combine(outputDirectory, "counts.txt"), "files=" + files.Length + "\nsynthetic=" + synthetic + "\npcmBytes=" + pcmBytes + "\n");
            File.WriteAllText(Path.Combine(outputDirectory, "durations.tsv"), string.Join("\n", durations) + "\n");
            return new Dictionary<string, bool>(Checks);
        }
    }
}
