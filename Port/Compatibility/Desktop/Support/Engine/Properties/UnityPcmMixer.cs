using System.Runtime.InteropServices;

namespace Engine.Audio {
    // Pure managed command state. Only Unity's DSP callback advances sample cursors.
    internal static partial class UnityAudioCommands {
        const int Initial = 0x1011, Playing = 0x1012, Paused = 0x1013, Stopped = 0x1014;
        static readonly object gate = new();
        static uint next = 1;
        static Exception failure;
        static float masterGain = 1f;
        static long outputFrames;

        sealed class Buffer {
            internal byte[] Data = Array.Empty<byte>();
            internal int Channels, Rate;
            internal int Frames => Channels == 0 ? 0 : Data.Length / (Channels * 2);
            internal float Sample(int frame, int channel) {
                int offset = (frame * Channels + channel) * 2;
                return (short)(Data[offset] | Data[offset + 1] << 8) / 32768f;
            }
        }
        sealed class Source {
            internal int State = Initial, QueueIndex;
            internal bool Loop;
            internal double Cursor;
            internal uint Buffer;
            internal readonly List<uint> Queue = new();
            internal float Gain = 1f, Pitch = 1f, X, Y, Z;
        }
        static readonly Dictionary<uint, Buffer> buffers = new();
        static readonly Dictionary<uint, Source> sources = new();
        internal static long OutputFrames { get { lock (gate) return outputFrames; } }
        internal static void RecordFailure(string name, Exception error) {
            lock (gate) failure ??= new InvalidOperationException(name, error);
        }
        internal static void ThrowFailure() { lock (gate) { if (failure != null) throw failure; } }
        static int I(object value) => Convert.ToInt32(value);
        static uint U(object value) => Convert.ToUInt32(value);
        static float F(object value) => Convert.ToSingle(value);
        static IntPtr P(object value) => (IntPtr)value;
        static void Write(object pointer, int value) => Marshal.WriteInt32(P(pointer), value);
        static void WriteFloat(object pointer, float value) => Marshal.Copy(new[] { value }, 0, P(pointer), 1);
        static int Processed(Source source) => source.Loop ? 0 : source.State == Stopped ? source.Queue.Count : source.QueueIndex;
        static Buffer Current(Source source) {
            if (source.Buffer != 0) return buffers[source.Buffer];
            return source.QueueIndex < source.Queue.Count ? buffers[source.Queue[source.QueueIndex]] : null;
        }

        static bool Advance(Source source) {
            if (source.State != Playing) return false;
            var buffer = Current(source);
            if (source.Buffer != 0) {
                if (buffer.Frames > 0 && source.Cursor >= buffer.Frames && source.Loop) source.Cursor %= buffer.Frames;
                if (buffer.Frames > 0 && source.Cursor < buffer.Frames) return true;
                source.State = Stopped;
                return false;
            }
            int emptyBuffers = 0;
            while (buffer != null && source.Cursor >= buffer.Frames) {
                source.Cursor -= buffer.Frames;
                source.QueueIndex++;
                if (source.QueueIndex == source.Queue.Count && source.Loop) source.QueueIndex = 0;
                if (buffer.Frames == 0 && ++emptyBuffers >= source.Queue.Count) break;
                buffer = Current(source);
            }
            if (buffer != null && buffer.Frames > 0) return true;
            source.State = Stopped;
            return false;
        }
        static float NextSample(Source source, Buffer buffer, int frame, int channel) {
            if (frame + 1 < buffer.Frames) return buffer.Sample(frame + 1, channel);
            if (source.Buffer != 0) return source.Loop ? buffer.Sample(0, channel) : 0;
            int index = source.QueueIndex + 1;
            if (index == source.Queue.Count && source.Loop) index = 0;
            if (index >= source.Queue.Count) return 0;
            var nextBuffer = buffers[source.Queue[index]];
            return nextBuffer.Frames > 0 ? nextBuffer.Sample(0, channel) : 0;
        }

        internal static void Mix(float[] output, int channels, int rate) {
            Array.Clear(output, 0, output.Length);
            try {
                if (channels < 1 || rate < 1 || output.Length % channels != 0)
                    throw new ArgumentException("Invalid Unity PCM output layout.");
                lock (gate) {
                    int frames = output.Length / channels;
                    foreach (Source source in sources.Values) {
                        if (source.State != Playing) continue;
                        float length = MathF.Sqrt(source.X * source.X + source.Y * source.Y + source.Z * source.Z);
                        float pan = length > 0 ? Math.Clamp(source.X / length, -1f, 1f) : 0;
                        float leftGain = MathF.Sqrt((1f - pan) * 0.5f) * source.Gain * masterGain;
                        float rightGain = MathF.Sqrt((1f + pan) * 0.5f) * source.Gain * masterGain;
                        for (int frame = 0; frame < frames && Advance(source); frame++) {
                            Buffer buffer = Current(source);
                            int index = (int)source.Cursor;
                            float fraction = (float)(source.Cursor - index);
                            float left = buffer.Sample(index, 0);
                            left += (NextSample(source, buffer, index, 0) - left) * fraction;
                            float right = left;
                            if (buffer.Channels == 2) {
                                right = buffer.Sample(index, 1);
                                right += (NextSample(source, buffer, index, 1) - right) * fraction;
                                // Stereo buffers are not spatialized by the original OpenAL path.
                                left *= source.Gain * masterGain;
                                right *= source.Gain * masterGain;
                            }
                            else { left *= leftGain; right *= rightGain; }
                            if (channels == 1) output[frame] += (left + right) * 0.5f;
                            else {
                                output[frame * channels] += left;
                                output[frame * channels + 1] += right;
                            }
                            source.Cursor += buffer.Rate / (double)rate * source.Pitch;
                        }
                        Advance(source);
                    }
                    for (int i = 0; i < output.Length; i++) output[i] = Math.Clamp(output[i], -1f, 1f);
                    outputFrames += frames;
                }
            }
            catch (Exception error) { Array.Clear(output, 0, output.Length); RecordFailure("Unity PCM output", error); }
        }

        static void Control(Source source, string command) {
            switch (command) {
                case "alSourcePlay":
                    if (source.State != Paused) { source.Cursor = 0; source.QueueIndex = 0; }
                    source.State = Playing; Advance(source); break;
                case "alSourcePause": if (source.State == Playing) source.State = Paused; break;
                case "alSourceStop": source.State = Stopped; source.Cursor = 0; source.QueueIndex = source.Queue.Count; break;
                case "alSourceRewind": source.State = Initial; source.Cursor = 0; source.QueueIndex = 0; break;
                default: throw new NotSupportedException(command);
            }
        }
        static float Offset(Source source, bool seconds) {
            if (source.State != Playing && source.State != Paused) return 0;
            Buffer buffer = Current(source);
            if (buffer == null) return 0;
            double frames = source.Cursor;
            for (int i = 0; i < source.QueueIndex; i++) frames += buffers[source.Queue[i]].Frames;
            return (float)(seconds ? frames / buffer.Rate : frames);
        }

        internal static object Dispatch(string name, object[] args) {
            lock (gate) {
                switch (name) {
                    case "alGetError": return (Silk.NET.OpenAL.AudioError)0;
                    case "alGenBuffers": case "alGenSources":
                        for (int i = 0; i < I(args[0]); i++) {
                            uint id = next++;
                            Marshal.WriteInt32(P(args[1]), i * 4, (int)id);
                            if (name == "alGenSources") sources[id] = new Source(); else buffers[id] = new Buffer();
                        }
                        return null;
                    case "alDeleteBuffers": case "alDeleteSources":
                        for (int i = 0; i < I(args[0]); i++) {
                            uint id = (uint)Marshal.ReadInt32(P(args[1]), i * 4);
                            if (name == "alDeleteSources") sources.Remove(id);
                            else {
                                if (sources.Values.Any(source => source.Buffer == id || source.Queue.Contains(id)))
                                    throw new InvalidOperationException("Cannot delete an attached audio buffer.");
                                buffers.Remove(id);
                            }
                        }
                        return null;
                    case "alBufferData": {
                        int format = I(args[1]);
                        if (format != 0x1101 && format != 0x1103) throw new NotSupportedException("PCM format " + format);
                        int channels = format == 0x1101 ? 1 : 2, size = I(args[3]), rate = I(args[4]);
                        if (size < 0 || size % (channels * 2) != 0 || rate < 1) throw new ArgumentException("Invalid PCM buffer.");
                        var buffer = new Buffer { Channels = channels, Rate = rate, Data = new byte[size] };
                        Marshal.Copy(P(args[2]), buffer.Data, 0, size);
                        buffers[U(args[0])] = buffer;
                        return null;
                    }
                    case "alSourcePlay": case "alSourcePause": case "alSourceStop": case "alSourceRewind":
                        Control(sources[U(args[0])], name); return null;
                    case "alSourcePlayv": case "alSourcePausev": case "alSourceStopv": case "alSourceRewindv":
                        for (int i = 0; i < I(args[0]); i++) Control(sources[(uint)Marshal.ReadInt32(P(args[1]), i * 4)], name.TrimEnd('v'));
                        return null;
                    case "alGetSourcei": case "alGetSourceiv": {
                        Source source = sources[U(args[0])]; Advance(source);
                        Write(args[2], I(args[1]) switch {
                            0x1010 => source.State, 0x1015 => source.Buffer != 0 ? 1 : source.Queue.Count,
                            0x1016 => Processed(source), 0x1009 => (int)source.Buffer, 0x1007 => source.Loop ? 1 : 0,
                            0x1025 => (int)Offset(source, false), 0x1026 => (int)Offset(source, false) * (Current(source)?.Channels ?? 1) * 2,
                            _ => throw new NotSupportedException("AL source property " + I(args[1]))
                        }); return null;
                    }
                    case "alGetSourcef": case "alGetSourcefv": {
                        Source source = sources[U(args[0])]; Advance(source);
                        WriteFloat(args[2], I(args[1]) switch {
                            0x1003 => source.Pitch, 0x100A => source.Gain, 0x1024 => Offset(source, true),
                            0x1025 => Offset(source, false), _ => throw new NotSupportedException("AL float source property " + I(args[1]))
                        }); return null;
                    }
                    case "alSourceQueueBuffers": {
                        Source source = sources[U(args[0])];
                        if (source.Buffer != 0) throw new InvalidOperationException("Static source cannot queue buffers.");
                        for (int i = 0; i < I(args[1]); i++) {
                            uint id = (uint)Marshal.ReadInt32(P(args[2]), i * 4); Buffer buffer = buffers[id];
                            if (source.Queue.Count > 0) {
                                Buffer first = buffers[source.Queue[0]];
                                if (first.Channels != buffer.Channels || first.Rate != buffer.Rate)
                                    throw new InvalidOperationException("Queued PCM formats must match.");
                            }
                            source.Queue.Add(id);
                        }
                        return null;
                    }
                    case "alSourceUnqueueBuffers": {
                        Source source = sources[U(args[0])]; int count = I(args[1]);
                        if (count < 0 || count > Processed(source)) throw new InvalidOperationException("Unprocessed audio buffer.");
                        for (int i = 0; i < count; i++) {
                            Marshal.WriteInt32(P(args[2]), i * 4, (int)source.Queue[0]);
                            source.Queue.RemoveAt(0); source.QueueIndex = Math.Max(0, source.QueueIndex - 1);
                        }
                        return null;
                    }
                    case "alSourcei": {
                        Source source = sources[U(args[0])]; int property = I(args[1]);
                        if (property == 0x1009) {
                            uint id = U(args[2]); if (id != 0 && !buffers.ContainsKey(id)) throw new ArgumentException("Unknown audio buffer.");
                            source.Buffer = id; source.Queue.Clear(); source.Cursor = 0; source.QueueIndex = 0;
                        }
                        else if (property == 0x1007) source.Loop = I(args[2]) != 0;
                        else if (property != 0x202) throw new NotSupportedException("AL integer source property " + property);
                        return null;
                    }
                    case "alSourcef": {
                        Source source = sources[U(args[0])]; int property = I(args[1]); float value = F(args[2]);
                        if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Non-finite audio property.");
                        if (property == 0x1003 && value > 0) source.Pitch = value;
                        else if (property == 0x100A && value >= 0) source.Gain = value;
                        else throw new NotSupportedException("AL float source property " + property);
                        return null;
                    }
                    case "alSource3f": {
                        if (I(args[1]) != 0x1004) throw new NotSupportedException("AL vector source property " + I(args[1]));
                        Source source = sources[U(args[0])]; source.X = F(args[2]); source.Y = F(args[3]); source.Z = F(args[4]); return null;
                    }
                    case "alDistanceModel":
                        if (I(args[0]) != 0) throw new NotSupportedException("Engine audio expects distance model None.");
                        return null;
                    case "alListenerf":
                        if (I(args[0]) != 0x100A) throw new NotSupportedException("AL listener property " + I(args[0]));
                        masterGain = F(args[1]); return null;
                    default: throw new NotSupportedException("Unity audio command: " + name);
                }
            }
        }
    }
}
