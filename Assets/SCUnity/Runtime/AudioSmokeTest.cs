using System;
using System.Collections.Generic;
using UnityEngine;
using Engine.Audio;
using H = Engine.UnityRuntime.Host;

namespace SCUnity.Runtime
{
    internal sealed class AudioSmokeTest : IDisposable
    {
        readonly SurvivalcraftGame game;
        readonly float[] left = new float[1024], right = new float[1024];
        readonly short[] tone = new short[4800];
        readonly double started = UnityEngine.Time.realtimeSinceStartupAsDouble;
        double next;
        int phase;
        float fullLevel;
        long pitchStartFrames;
        SoundBuffer buffer;
        Sound sound;
        StreamingSound streaming;
        public bool Complete { get; private set; }
        public readonly List<string> Checks = new List<string>();

        public AudioSmokeTest(SurvivalcraftGame game)
        {
            this.game = game;
            Game.SettingsManager.MusicVolume = 0;
            Game.MusicManager.CurrentMix = Game.MusicManager.Mix.None;
            Game.MusicManager.StopMusic();
            Mixer.MasterVolume = 1;
            for (int i = 0; i < tone.Length; i++) tone[i] = (short)(Math.Sin(2 * Math.PI * 1000 * i / 48000) * 6553);
            next = started + .5;
        }

        float Level()
        {
            AudioListener.GetOutputData(left, 0);
            AudioListener.GetOutputData(right, 1);
            double sum = 0;
            for (int i = 0; i < left.Length; i++) sum += left[i] * left[i] + right[i] * right[i];
            return (float)Math.Sqrt(sum / (left.Length * 2));
        }

        void Check(bool passed, string name)
        {
            if (!passed) throw new Exception("Unity audio assertion: " + name);
            Checks.Add(name);
            Debug.Log("AUDIO PASS: " + name);
        }

        public void Tick()
        {
            if (Complete) return;
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            if (now - started > 15) throw new TimeoutException("Audio smoke test timed out in phase " + phase);
            if (now < next) return;
            next = now + .3;
            switch (phase++)
            {
                case 0:
                    Check(game.AudioBlocks > 0, "Unity DSP callback is active");
                    Level(); // Start the listener's rolling sample capture before measuring.
                    buffer = new SoundBuffer(tone, 0, tone.Length, 1, 48000);
                    sound = new Sound(buffer, isLooped: true);
                    sound.Play();
                    break;
                case 1:
                    fullLevel = Level();
                    Debug.Log("AUDIO LEVEL: filter=" + game.AudioLevel + " listener=" + fullLevel + " sound=" + sound.State + " listenerGain=" + AudioListener.volume + " paused=" + AudioListener.pause);
                    Check(fullLevel > .06f && fullLevel < .14f, "Static PCM reached the Unity listener (RMS " + fullLevel + ")");
                    Check(sound.State == SoundState.Playing, "Static loop survived multiple buffer lengths");
                    sound.Volume = .25f;
                    break;
                case 2:
                    float quarter = Level();
                    Check(quarter / fullLevel > .18f && quarter / fullLevel < .32f, "Volume scales actual output by one quarter");
                    sound.Pause();
                    break;
                case 3:
                    Check(sound.State == SoundState.Paused && Level() < .001f, "Pause preserves state and silences output");
                    sound.Play();
                    break;
                case 4:
                    Check(sound.State == SoundState.Playing && Level() > .015f, "Resume produces PCM again");
                    sound.Stop();
                    break;
                case 5:
                    Check(sound.State == SoundState.Stopped && Level() < .001f, "Stop silences output");
                    sound.Dispose();
                    sound = new Sound(buffer, volume: .25f, pitch: 2);
                    pitchStartFrames = H.AudioOutputFrames;
                    sound.Play();
                    next = now;
                    break;
                case 6:
                    if (sound.State != SoundState.Stopped) { phase--; next = now; break; }
                    double played = (H.AudioOutputFrames - pitchStartFrames) / (double)AudioSettings.outputSampleRate;
                    Check(played >= .04 && played < .13, "Pitch 2 finishes a 100 ms buffer in about 50 ms of DSP time");
                    sound.Dispose(); sound = null;
                    streaming = new StreamingSound(new ToneStream(tone, 6), volume: .5f, bufferDuration: .06f);
                    streaming.Play();
                    break;
                case 7:
                    Check(streaming.State == SoundState.Playing && Level() > .025f, "Streaming worker refills queued PCM while Unity consumes it");
                    next = now + .6;
                    break;
                case 8:
                    Check(streaming.State == SoundState.Stopped && Level() < .001f, "Streaming EOF reaches stopped state without a wall clock simulation");
                    streaming.Dispose(); streaming = null;
                    buffer.Dispose(); buffer = null;
                    var music = Game.ContentManager.Get<Engine.Media.StreamingSource>("Music/NativeAmericanFluteSpirit").Duplicate();
                    music.Position = music.BytesCount / music.ChannelsCount / 8;
                    streaming = new StreamingSound(music, volume: .5f, bufferDuration: .12f);
                    streaming.Play();
                    next = now + .6;
                    break;
                case 9:
                    Check(streaming.State == SoundState.Playing && Level() > .0001f, "Original decoded music resource reaches Unity output");
                    streaming.Pause();
                    break;
                case 10:
                    Check(streaming.State == SoundState.Paused && Level() < .001f, "Original streaming music pauses without leaking queued audio");
                    streaming.Dispose(); streaming = null;
                    Complete = true;
                    break;
            }
        }

        public void Dispose() { sound?.Dispose(); streaming?.Dispose(); buffer?.Dispose(); }

        sealed class ToneStream : Engine.Media.StreamingSource
        {
            readonly byte[] data;
            int offset;
            public ToneStream(short[] tone, int repeats)
            {
                data = new byte[tone.Length * 2 * repeats];
                for (int i = 0; i < data.Length / 2; i++) { short sample = tone[i % tone.Length]; data[2 * i] = (byte)sample; data[2 * i + 1] = (byte)(sample >> 8); }
            }
            ToneStream(byte[] data) { this.data = data; }
            public override int ChannelsCount => 1;
            public override int SamplingFrequency => 48000;
            public override long BytesCount => data.Length;
            public override long Position { get => offset / 2; set => offset = checked((int)value * 2); }
            public override int Read(byte[] buffer, int start, int count)
            {
                int read = Math.Min(count, data.Length - offset);
                Array.Copy(data, offset, buffer, start, read); offset += read; return read;
            }
            public override Engine.Media.StreamingSource Duplicate() => new ToneStream(data);
        }
    }
}
