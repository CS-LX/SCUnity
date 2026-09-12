using System;using System.Runtime.InteropServices;using System.Collections.Generic;using Silk.NET.OpenAL;namespace Engine.Audio {internal static partial class UnityAudioCommands {
static readonly Dictionary<string,Delegate> callbacks = new();static readonly Dictionary<string,IntPtr> pointers = new();
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate byte D_alIsExtensionPresent(IntPtr p0);
static byte C_alIsExtensionPresent(IntPtr p0){try{return (byte)Dispatch("alIsExtensionPresent",new object[]{p0});}catch(Exception e){RecordFailure("alIsExtensionPresent",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr D_alGetProcAddress(IntPtr p0);
static IntPtr C_alGetProcAddress(IntPtr p0){try{return (IntPtr)Dispatch("alGetProcAddress",new object[]{p0});}catch(Exception e){RecordFailure("alGetProcAddress",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int D_alGetEnumValue(IntPtr p0);
static int C_alGetEnumValue(IntPtr p0){try{return (int)Dispatch("alGetEnumValue",new object[]{p0});}catch(Exception e){RecordFailure("alGetEnumValue",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGenBuffers(int p0, IntPtr p1);
static void C_alGenBuffers(int p0, IntPtr p1){try{Dispatch("alGenBuffers",new object[]{p0, p1});}catch(Exception e){RecordFailure("alGenBuffers",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alDeleteBuffers(int p0, IntPtr p1);
static void C_alDeleteBuffers(int p0, IntPtr p1){try{Dispatch("alDeleteBuffers",new object[]{p0, p1});}catch(Exception e){RecordFailure("alDeleteBuffers",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate byte D_alIsBuffer(uint p0);
static byte C_alIsBuffer(uint p0){try{return (byte)Dispatch("alIsBuffer",new object[]{p0});}catch(Exception e){RecordFailure("alIsBuffer",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBufferData(uint p0, BufferFormat p1, IntPtr p2, int p3, int p4);
static void C_alBufferData(uint p0, BufferFormat p1, IntPtr p2, int p3, int p4){try{Dispatch("alBufferData",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alBufferData",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBufferf(uint p0, BufferFloat p1, float p2);
static void C_alBufferf(uint p0, BufferFloat p1, float p2){try{Dispatch("alBufferf",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alBufferf",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBuffer3f(uint p0, BufferVector3 p1, float p2, float p3, float p4);
static void C_alBuffer3f(uint p0, BufferVector3 p1, float p2, float p3, float p4){try{Dispatch("alBuffer3f",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alBuffer3f",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBufferfv(uint p0, BufferVector3 p1, IntPtr p2);
static void C_alBufferfv(uint p0, BufferVector3 p1, IntPtr p2){try{Dispatch("alBufferfv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alBufferfv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBufferi(uint p0, BufferInteger p1, int p2);
static void C_alBufferi(uint p0, BufferInteger p1, int p2){try{Dispatch("alBufferi",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alBufferi",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBuffer3i(uint p0, BufferInteger p1, int p2, int p3, int p4);
static void C_alBuffer3i(uint p0, BufferInteger p1, int p2, int p3, int p4){try{Dispatch("alBuffer3i",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alBuffer3i",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alBufferiv(uint p0, BufferInteger p1, IntPtr p2);
static void C_alBufferiv(uint p0, BufferInteger p1, IntPtr p2){try{Dispatch("alBufferiv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alBufferiv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetBufferf(uint p0, BufferFloat p1, IntPtr p2);
static void C_alGetBufferf(uint p0, BufferFloat p1, IntPtr p2){try{Dispatch("alGetBufferf",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetBufferf",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetBuffer3f(uint p0, BufferVector3 p1, IntPtr p2, IntPtr p3, IntPtr p4);
static void C_alGetBuffer3f(uint p0, BufferVector3 p1, IntPtr p2, IntPtr p3, IntPtr p4){try{Dispatch("alGetBuffer3f",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alGetBuffer3f",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetBufferfv(uint p0, BufferFloat p1, IntPtr p2);
static void C_alGetBufferfv(uint p0, BufferFloat p1, IntPtr p2){try{Dispatch("alGetBufferfv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetBufferfv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetBufferi(uint p0, GetBufferInteger p1, IntPtr p2);
static void C_alGetBufferi(uint p0, GetBufferInteger p1, IntPtr p2){try{Dispatch("alGetBufferi",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetBufferi",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetBuffer3i(uint p0, GetBufferInteger p1, IntPtr p2, IntPtr p3, IntPtr p4);
static void C_alGetBuffer3i(uint p0, GetBufferInteger p1, IntPtr p2, IntPtr p3, IntPtr p4){try{Dispatch("alGetBuffer3i",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alGetBuffer3i",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetBufferiv(uint p0, GetBufferInteger p1, IntPtr p2);
static void C_alGetBufferiv(uint p0, GetBufferInteger p1, IntPtr p2){try{Dispatch("alGetBufferiv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetBufferiv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate AudioError D_alGetError();
static AudioError C_alGetError(){try{return (AudioError)Dispatch("alGetError",new object[]{});}catch(Exception e){RecordFailure("alGetError",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alListenerf(ListenerFloat p0, float p1);
static void C_alListenerf(ListenerFloat p0, float p1){try{Dispatch("alListenerf",new object[]{p0, p1});}catch(Exception e){RecordFailure("alListenerf",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alListener3f(ListenerVector3 p0, float p1, float p2, float p3);
static void C_alListener3f(ListenerVector3 p0, float p1, float p2, float p3){try{Dispatch("alListener3f",new object[]{p0, p1, p2, p3});}catch(Exception e){RecordFailure("alListener3f",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alListenerfv(ListenerFloatArray p0, IntPtr p1);
static void C_alListenerfv(ListenerFloatArray p0, IntPtr p1){try{Dispatch("alListenerfv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alListenerfv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alListeneri(ListenerInteger p0, int p1);
static void C_alListeneri(ListenerInteger p0, int p1){try{Dispatch("alListeneri",new object[]{p0, p1});}catch(Exception e){RecordFailure("alListeneri",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alListener3i(ListenerInteger p0, int p1, int p2, int p3);
static void C_alListener3i(ListenerInteger p0, int p1, int p2, int p3){try{Dispatch("alListener3i",new object[]{p0, p1, p2, p3});}catch(Exception e){RecordFailure("alListener3i",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alListeneriv(ListenerInteger p0, IntPtr p1);
static void C_alListeneriv(ListenerInteger p0, IntPtr p1){try{Dispatch("alListeneriv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alListeneriv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetListenerf(ListenerFloat p0, IntPtr p1);
static void C_alGetListenerf(ListenerFloat p0, IntPtr p1){try{Dispatch("alGetListenerf",new object[]{p0, p1});}catch(Exception e){RecordFailure("alGetListenerf",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetListener3f(ListenerVector3 p0, IntPtr p1, IntPtr p2, IntPtr p3);
static void C_alGetListener3f(ListenerVector3 p0, IntPtr p1, IntPtr p2, IntPtr p3){try{Dispatch("alGetListener3f",new object[]{p0, p1, p2, p3});}catch(Exception e){RecordFailure("alGetListener3f",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetListenerfv(ListenerFloatArray p0, IntPtr p1);
static void C_alGetListenerfv(ListenerFloatArray p0, IntPtr p1){try{Dispatch("alGetListenerfv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alGetListenerfv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetListeneri(ListenerInteger p0, IntPtr p1);
static void C_alGetListeneri(ListenerInteger p0, IntPtr p1){try{Dispatch("alGetListeneri",new object[]{p0, p1});}catch(Exception e){RecordFailure("alGetListeneri",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetListener3i(ListenerInteger p0, IntPtr p1, IntPtr p2, IntPtr p3);
static void C_alGetListener3i(ListenerInteger p0, IntPtr p1, IntPtr p2, IntPtr p3){try{Dispatch("alGetListener3i",new object[]{p0, p1, p2, p3});}catch(Exception e){RecordFailure("alGetListener3i",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetListeneriv(ListenerInteger p0, IntPtr p1);
static void C_alGetListeneriv(ListenerInteger p0, IntPtr p1){try{Dispatch("alGetListeneriv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alGetListeneriv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGenSources(int p0, IntPtr p1);
static void C_alGenSources(int p0, IntPtr p1){try{Dispatch("alGenSources",new object[]{p0, p1});}catch(Exception e){RecordFailure("alGenSources",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alDeleteSources(int p0, IntPtr p1);
static void C_alDeleteSources(int p0, IntPtr p1){try{Dispatch("alDeleteSources",new object[]{p0, p1});}catch(Exception e){RecordFailure("alDeleteSources",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate byte D_alIsSource(uint p0);
static byte C_alIsSource(uint p0){try{return (byte)Dispatch("alIsSource",new object[]{p0});}catch(Exception e){RecordFailure("alIsSource",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcef(uint p0, SourceFloat p1, float p2);
static void C_alSourcef(uint p0, SourceFloat p1, float p2){try{Dispatch("alSourcef",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alSourcef",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSource3f(uint p0, SourceVector3 p1, float p2, float p3, float p4);
static void C_alSource3f(uint p0, SourceVector3 p1, float p2, float p3, float p4){try{Dispatch("alSource3f",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alSource3f",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcefv(uint p0, SourceVector3 p1, IntPtr p2);
static void C_alSourcefv(uint p0, SourceVector3 p1, IntPtr p2){try{Dispatch("alSourcefv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alSourcefv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcei(uint p0, SourceInteger p1, uint p2);
static void C_alSourcei(uint p0, SourceInteger p1, uint p2){try{Dispatch("alSourcei",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alSourcei",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSource3i(uint p0, SourceInteger p1, uint p2, uint p3, uint p4);
static void C_alSource3i(uint p0, SourceInteger p1, uint p2, uint p3, uint p4){try{Dispatch("alSource3i",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alSource3i",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceiv(uint p0, SourceInteger p1, IntPtr p2);
static void C_alSourceiv(uint p0, SourceInteger p1, IntPtr p2){try{Dispatch("alSourceiv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alSourceiv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetSourcef(uint p0, SourceFloat p1, IntPtr p2);
static void C_alGetSourcef(uint p0, SourceFloat p1, IntPtr p2){try{Dispatch("alGetSourcef",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetSourcef",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetSourcei(uint p0, GetSourceInteger p1, IntPtr p2);
static void C_alGetSourcei(uint p0, GetSourceInteger p1, IntPtr p2){try{Dispatch("alGetSourcei",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetSourcei",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetSource3f(uint p0, SourceVector3 p1, IntPtr p2, IntPtr p3, IntPtr p4);
static void C_alGetSource3f(uint p0, SourceVector3 p1, IntPtr p2, IntPtr p3, IntPtr p4){try{Dispatch("alGetSource3f",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alGetSource3f",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetSourcefv(uint p0, SourceFloat p1, IntPtr p2);
static void C_alGetSourcefv(uint p0, SourceFloat p1, IntPtr p2){try{Dispatch("alGetSourcefv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetSourcefv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetSource3i(uint p0, GetSourceInteger p1, IntPtr p2, IntPtr p3, IntPtr p4);
static void C_alGetSource3i(uint p0, GetSourceInteger p1, IntPtr p2, IntPtr p3, IntPtr p4){try{Dispatch("alGetSource3i",new object[]{p0, p1, p2, p3, p4});}catch(Exception e){RecordFailure("alGetSource3i",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alGetSourceiv(uint p0, GetSourceInteger p1, IntPtr p2);
static void C_alGetSourceiv(uint p0, GetSourceInteger p1, IntPtr p2){try{Dispatch("alGetSourceiv",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alGetSourceiv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcePlay(uint p0);
static void C_alSourcePlay(uint p0){try{Dispatch("alSourcePlay",new object[]{p0});}catch(Exception e){RecordFailure("alSourcePlay",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcePlayv(int p0, IntPtr p1);
static void C_alSourcePlayv(int p0, IntPtr p1){try{Dispatch("alSourcePlayv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alSourcePlayv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcePause(uint p0);
static void C_alSourcePause(uint p0){try{Dispatch("alSourcePause",new object[]{p0});}catch(Exception e){RecordFailure("alSourcePause",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourcePausev(int p0, IntPtr p1);
static void C_alSourcePausev(int p0, IntPtr p1){try{Dispatch("alSourcePausev",new object[]{p0, p1});}catch(Exception e){RecordFailure("alSourcePausev",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceStop(uint p0);
static void C_alSourceStop(uint p0){try{Dispatch("alSourceStop",new object[]{p0});}catch(Exception e){RecordFailure("alSourceStop",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceStopv(int p0, IntPtr p1);
static void C_alSourceStopv(int p0, IntPtr p1){try{Dispatch("alSourceStopv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alSourceStopv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceRewind(uint p0);
static void C_alSourceRewind(uint p0){try{Dispatch("alSourceRewind",new object[]{p0});}catch(Exception e){RecordFailure("alSourceRewind",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceRewindv(int p0, IntPtr p1);
static void C_alSourceRewindv(int p0, IntPtr p1){try{Dispatch("alSourceRewindv",new object[]{p0, p1});}catch(Exception e){RecordFailure("alSourceRewindv",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceQueueBuffers(uint p0, int p1, IntPtr p2);
static void C_alSourceQueueBuffers(uint p0, int p1, IntPtr p2){try{Dispatch("alSourceQueueBuffers",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alSourceQueueBuffers",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSourceUnqueueBuffers(uint p0, int p1, IntPtr p2);
static void C_alSourceUnqueueBuffers(uint p0, int p1, IntPtr p2){try{Dispatch("alSourceUnqueueBuffers",new object[]{p0, p1, p2});}catch(Exception e){RecordFailure("alSourceUnqueueBuffers",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alEnable(Capability p0);
static void C_alEnable(Capability p0){try{Dispatch("alEnable",new object[]{p0});}catch(Exception e){RecordFailure("alEnable",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alDisable(Capability p0);
static void C_alDisable(Capability p0){try{Dispatch("alDisable",new object[]{p0});}catch(Exception e){RecordFailure("alDisable",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate byte D_alIsEnabled(Capability p0);
static byte C_alIsEnabled(Capability p0){try{return (byte)Dispatch("alIsEnabled",new object[]{p0});}catch(Exception e){RecordFailure("alIsEnabled",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate byte D_alGetBoolean(StateBoolean p0);
static byte C_alGetBoolean(StateBoolean p0){try{return (byte)Dispatch("alGetBoolean",new object[]{p0});}catch(Exception e){RecordFailure("alGetBoolean",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate double D_alGetDouble(StateDouble p0);
static double C_alGetDouble(StateDouble p0){try{return (double)Dispatch("alGetDouble",new object[]{p0});}catch(Exception e){RecordFailure("alGetDouble",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate float D_alGetFloat(StateFloat p0);
static float C_alGetFloat(StateFloat p0){try{return (float)Dispatch("alGetFloat",new object[]{p0});}catch(Exception e){RecordFailure("alGetFloat",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int D_alGetInteger(StateInteger p0);
static int C_alGetInteger(StateInteger p0){try{return (int)Dispatch("alGetInteger",new object[]{p0});}catch(Exception e){RecordFailure("alGetInteger",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr D_alGetString(StateString p0);
static IntPtr C_alGetString(StateString p0){try{return (IntPtr)Dispatch("alGetString",new object[]{p0});}catch(Exception e){RecordFailure("alGetString",e);return default;}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alDistanceModel(DistanceModel p0);
static void C_alDistanceModel(DistanceModel p0){try{Dispatch("alDistanceModel",new object[]{p0});}catch(Exception e){RecordFailure("alDistanceModel",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alDopplerFactor(float p0);
static void C_alDopplerFactor(float p0){try{Dispatch("alDopplerFactor",new object[]{p0});}catch(Exception e){RecordFailure("alDopplerFactor",e);}}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void D_alSpeedOfSound(float p0);
static void C_alSpeedOfSound(float p0){try{Dispatch("alSpeedOfSound",new object[]{p0});}catch(Exception e){RecordFailure("alSpeedOfSound",e);}}
internal static IntPtr Resolve(string name){if(pointers.TryGetValue(name,out var p))return p;Delegate d;switch(name){
case "alIsExtensionPresent":d=new D_alIsExtensionPresent(C_alIsExtensionPresent);break;
case "alGetProcAddress":d=new D_alGetProcAddress(C_alGetProcAddress);break;
case "alGetEnumValue":d=new D_alGetEnumValue(C_alGetEnumValue);break;
case "alGenBuffers":d=new D_alGenBuffers(C_alGenBuffers);break;
case "alDeleteBuffers":d=new D_alDeleteBuffers(C_alDeleteBuffers);break;
case "alIsBuffer":d=new D_alIsBuffer(C_alIsBuffer);break;
case "alBufferData":d=new D_alBufferData(C_alBufferData);break;
case "alBufferf":d=new D_alBufferf(C_alBufferf);break;
case "alBuffer3f":d=new D_alBuffer3f(C_alBuffer3f);break;
case "alBufferfv":d=new D_alBufferfv(C_alBufferfv);break;
case "alBufferi":d=new D_alBufferi(C_alBufferi);break;
case "alBuffer3i":d=new D_alBuffer3i(C_alBuffer3i);break;
case "alBufferiv":d=new D_alBufferiv(C_alBufferiv);break;
case "alGetBufferf":d=new D_alGetBufferf(C_alGetBufferf);break;
case "alGetBuffer3f":d=new D_alGetBuffer3f(C_alGetBuffer3f);break;
case "alGetBufferfv":d=new D_alGetBufferfv(C_alGetBufferfv);break;
case "alGetBufferi":d=new D_alGetBufferi(C_alGetBufferi);break;
case "alGetBuffer3i":d=new D_alGetBuffer3i(C_alGetBuffer3i);break;
case "alGetBufferiv":d=new D_alGetBufferiv(C_alGetBufferiv);break;
case "alGetError":d=new D_alGetError(C_alGetError);break;
case "alListenerf":d=new D_alListenerf(C_alListenerf);break;
case "alListener3f":d=new D_alListener3f(C_alListener3f);break;
case "alListenerfv":d=new D_alListenerfv(C_alListenerfv);break;
case "alListeneri":d=new D_alListeneri(C_alListeneri);break;
case "alListener3i":d=new D_alListener3i(C_alListener3i);break;
case "alListeneriv":d=new D_alListeneriv(C_alListeneriv);break;
case "alGetListenerf":d=new D_alGetListenerf(C_alGetListenerf);break;
case "alGetListener3f":d=new D_alGetListener3f(C_alGetListener3f);break;
case "alGetListenerfv":d=new D_alGetListenerfv(C_alGetListenerfv);break;
case "alGetListeneri":d=new D_alGetListeneri(C_alGetListeneri);break;
case "alGetListener3i":d=new D_alGetListener3i(C_alGetListener3i);break;
case "alGetListeneriv":d=new D_alGetListeneriv(C_alGetListeneriv);break;
case "alGenSources":d=new D_alGenSources(C_alGenSources);break;
case "alDeleteSources":d=new D_alDeleteSources(C_alDeleteSources);break;
case "alIsSource":d=new D_alIsSource(C_alIsSource);break;
case "alSourcef":d=new D_alSourcef(C_alSourcef);break;
case "alSource3f":d=new D_alSource3f(C_alSource3f);break;
case "alSourcefv":d=new D_alSourcefv(C_alSourcefv);break;
case "alSourcei":d=new D_alSourcei(C_alSourcei);break;
case "alSource3i":d=new D_alSource3i(C_alSource3i);break;
case "alSourceiv":d=new D_alSourceiv(C_alSourceiv);break;
case "alGetSourcef":d=new D_alGetSourcef(C_alGetSourcef);break;
case "alGetSourcei":d=new D_alGetSourcei(C_alGetSourcei);break;
case "alGetSource3f":d=new D_alGetSource3f(C_alGetSource3f);break;
case "alGetSourcefv":d=new D_alGetSourcefv(C_alGetSourcefv);break;
case "alGetSource3i":d=new D_alGetSource3i(C_alGetSource3i);break;
case "alGetSourceiv":d=new D_alGetSourceiv(C_alGetSourceiv);break;
case "alSourcePlay":d=new D_alSourcePlay(C_alSourcePlay);break;
case "alSourcePlayv":d=new D_alSourcePlayv(C_alSourcePlayv);break;
case "alSourcePause":d=new D_alSourcePause(C_alSourcePause);break;
case "alSourcePausev":d=new D_alSourcePausev(C_alSourcePausev);break;
case "alSourceStop":d=new D_alSourceStop(C_alSourceStop);break;
case "alSourceStopv":d=new D_alSourceStopv(C_alSourceStopv);break;
case "alSourceRewind":d=new D_alSourceRewind(C_alSourceRewind);break;
case "alSourceRewindv":d=new D_alSourceRewindv(C_alSourceRewindv);break;
case "alSourceQueueBuffers":d=new D_alSourceQueueBuffers(C_alSourceQueueBuffers);break;
case "alSourceUnqueueBuffers":d=new D_alSourceUnqueueBuffers(C_alSourceUnqueueBuffers);break;
case "alEnable":d=new D_alEnable(C_alEnable);break;
case "alDisable":d=new D_alDisable(C_alDisable);break;
case "alIsEnabled":d=new D_alIsEnabled(C_alIsEnabled);break;
case "alGetBoolean":d=new D_alGetBoolean(C_alGetBoolean);break;
case "alGetDouble":d=new D_alGetDouble(C_alGetDouble);break;
case "alGetFloat":d=new D_alGetFloat(C_alGetFloat);break;
case "alGetInteger":d=new D_alGetInteger(C_alGetInteger);break;
case "alGetString":d=new D_alGetString(C_alGetString);break;
case "alDistanceModel":d=new D_alDistanceModel(C_alDistanceModel);break;
case "alDopplerFactor":d=new D_alDopplerFactor(C_alDopplerFactor);break;
case "alSpeedOfSound":d=new D_alSpeedOfSound(C_alSpeedOfSound);break;
default:return IntPtr.Zero;}callbacks.Add(name,d);p=Marshal.GetFunctionPointerForDelegate(d);pointers.Add(name,p);return p;}}}