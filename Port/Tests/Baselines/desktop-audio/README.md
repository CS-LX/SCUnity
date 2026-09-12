# Main-project audio acceptance

Unity 6000.3.12f1 Windows x64 Mono, from a complete main-project snapshot.
Two independent Player runs each pass 12 checks at the actual Unity Listener:
static PCM RMS, looping, quarter volume, pause/resume/stop, pitch timing,
stream refill/EOF, and original decoded music playback/pause. RMS for the
known 1 kHz PCM signal is 0.09965976; 305 and 304 DSP blocks were consumed.
The menu/settings/Escape interaction remains included in both runs.

Unity build has 0 errors and 0 warnings. Two Editor Play/Stop cycles with Domain
reload pass. All 60 installed managed plugin DLLs match the Player copies.
Engine, EntitySystem and Survivalcraft independently rebuild byte-identically.
The 122 GL and 68 AL callback prototypes match installed Silk 2.23 metadata.

Reproduce using `python Port/Build/desktop.py --unity <Unity.exe> --install
--validate --audio-test`, then `python Port/Build/abi_audit.py`. The latter
requires ilspycmd 10.1.0.8386. Detailed logs remain in the artifact directories
recorded by evidence.json; no original user data was used or modified.

The mixer implements the PCM16 mono/stereo commands used by Engine Sound and
StreamingSound, linear sample-rate/pitch conversion, equal-power mono panning,
gain and queued streaming. It does not claim bit-exact OpenAL mixing, HRTF,
Doppler, effects, all output-device configurations, or arbitrary low-level AL
commands. World rendering and full game/mod/API acceptance remain pending.
Retarget MSBuild retains the 11 warnings documented in the desktop README.

Semantics references: [OpenAL 1.1 specification](https://www.openal.org/documentation/openal-1.1-specification.pdf)
and [Unity OnAudioFilterRead](https://docs.unity3d.com/cn/6000.0/ScriptReference/MonoBehaviour.OnAudioFilterRead.html).
Only the managed mixer runs on the DSP callback; Unity object operations stay
on the main thread.
