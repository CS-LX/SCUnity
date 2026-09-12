using System;
using System.Threading;
using UnityEngine;
using H = Engine.UnityRuntime.Host;

namespace SCUnity.Runtime {
    [RequireComponent(typeof(AudioSource))]
    public sealed class PortAudio : MonoBehaviour {
        AudioSource output;
        AudioClip clockClip;
        AudioListener ownedListener;
        int rate;
        volatile bool running;
        long blocks;
        float level;
        public long Blocks => Interlocked.Read(ref blocks);
        public float Level => Volatile.Read(ref level);

        public void Initialize() {
            rate = AudioSettings.outputSampleRate;
            if (rate < 1) throw new InvalidOperationException("Unity audio output has no sample rate.");
            if (FindFirstObjectByType<AudioListener>() == null) {
                // A filter cannot belong to a source and a listener on the same object.
                var listenerObject = new GameObject("Survivalcraft listener");
                listenerObject.transform.SetParent(transform.parent, false);
                ownedListener = listenerObject.AddComponent<AudioListener>();
            }
            output = GetComponent<AudioSource>();
            output.playOnAwake = false;
            output.spatialBlend = 0;
            output.volume = 1;
            output.pitch = 1;
            output.loop = true;
            output.priority = 0;
            clockClip = AudioClip.Create(
                "Survivalcraft PCM clock",
                rate,
                2,
                rate,
                false
            );
            output.clip = clockClip;
            running = true;
            output.Play();
            AudioSettings.OnAudioConfigurationChanged += ConfigurationChanged;
        }

        void ConfigurationChanged(bool deviceChanged) {
            rate = AudioSettings.outputSampleRate;
        }

        void OnAudioFilterRead(float[] samples, int channels) {
            if (!running) {
                Array.Clear(samples, 0, samples.Length);
                return;
            }
            H.MixAudio(samples, channels, Volatile.Read(ref rate));
            double sum = 0;
            for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
            Volatile.Write(ref level, samples.Length == 0 ? 0 : (float)Math.Sqrt(sum / samples.Length));
            Interlocked.Increment(ref blocks);
        }

        public void StopOutput() {
            running = false;
            AudioSettings.OnAudioConfigurationChanged -= ConfigurationChanged;
            if (output != null) {
                output.Stop();
                output.clip = null;
            }
            if (clockClip != null) Destroy(clockClip);
            if (ownedListener != null) Destroy(ownedListener.gameObject);
        }

        void OnDestroy() => StopOutput();
    }
}