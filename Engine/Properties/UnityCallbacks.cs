// Generated from Silk.NET.OpenGLES 2.22.0 native entry-point metadata.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace Engine.Graphics { internal static partial class UnityCommands {
static readonly Dictionary<string, Delegate> callbacks = new Dictionary<string, Delegate>();
static readonly Dictionary<string, IntPtr> pointers = new Dictionary<string, IntPtr>();
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glActiveTexture(System.Int32 p0);
static void C_glActiveTexture(System.Int32 p0) { try { Dispatch("glActiveTexture", new object[]{p0}); } catch(Exception e) { RecordFailure("glActiveTexture", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glAttachShader(System.UInt32 p0, System.UInt32 p1);
static void C_glAttachShader(System.UInt32 p0, System.UInt32 p1) { try { Dispatch("glAttachShader", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glAttachShader", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBindBuffer(System.Int32 p0, System.UInt32 p1);
static void C_glBindBuffer(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glBindBuffer", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBindBuffer", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBindBufferBase(System.Int32 p0, System.UInt32 p1, System.UInt32 p2);
static void C_glBindBufferBase(System.Int32 p0, System.UInt32 p1, System.UInt32 p2) { try { Dispatch("glBindBufferBase", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glBindBufferBase", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBindFramebuffer(System.Int32 p0, System.UInt32 p1);
static void C_glBindFramebuffer(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glBindFramebuffer", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBindFramebuffer", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBindRenderbuffer(System.Int32 p0, System.UInt32 p1);
static void C_glBindRenderbuffer(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glBindRenderbuffer", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBindRenderbuffer", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBindTexture(System.Int32 p0, System.UInt32 p1);
static void C_glBindTexture(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glBindTexture", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBindTexture", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendColor(System.Single p0, System.Single p1, System.Single p2, System.Single p3);
static void C_glBlendColor(System.Single p0, System.Single p1, System.Single p2, System.Single p3) { try { Dispatch("glBlendColor", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glBlendColor", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendEquation(System.Int32 p0);
static void C_glBlendEquation(System.Int32 p0) { try { Dispatch("glBlendEquation", new object[]{p0}); } catch(Exception e) { RecordFailure("glBlendEquation", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendEquationi(System.UInt32 p0, System.Int32 p1);
static void C_glBlendEquationi(System.UInt32 p0, System.Int32 p1) { try { Dispatch("glBlendEquationi", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBlendEquationi", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendEquationSeparate(System.Int32 p0, System.Int32 p1);
static void C_glBlendEquationSeparate(System.Int32 p0, System.Int32 p1) { try { Dispatch("glBlendEquationSeparate", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBlendEquationSeparate", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendEquationSeparatei(System.UInt32 p0, System.Int32 p1, System.Int32 p2);
static void C_glBlendEquationSeparatei(System.UInt32 p0, System.Int32 p1, System.Int32 p2) { try { Dispatch("glBlendEquationSeparatei", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glBlendEquationSeparatei", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendFunc(System.Int32 p0, System.Int32 p1);
static void C_glBlendFunc(System.Int32 p0, System.Int32 p1) { try { Dispatch("glBlendFunc", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glBlendFunc", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendFunci(System.UInt32 p0, System.Int32 p1, System.Int32 p2);
static void C_glBlendFunci(System.UInt32 p0, System.Int32 p1, System.Int32 p2) { try { Dispatch("glBlendFunci", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glBlendFunci", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendFuncSeparate(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3);
static void C_glBlendFuncSeparate(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3) { try { Dispatch("glBlendFuncSeparate", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glBlendFuncSeparate", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlendFuncSeparatei(System.UInt32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4);
static void C_glBlendFuncSeparatei(System.UInt32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4) { try { Dispatch("glBlendFuncSeparatei", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glBlendFuncSeparatei", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBlitFramebuffer(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4, System.Int32 p5, System.Int32 p6, System.Int32 p7, System.UInt32 p8, System.Int32 p9);
static void C_glBlitFramebuffer(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4, System.Int32 p5, System.Int32 p6, System.Int32 p7, System.UInt32 p8, System.Int32 p9) { try { Dispatch("glBlitFramebuffer", new object[]{p0, p1, p2, p3, p4, p5, p6, p7, p8, p9}); } catch(Exception e) { RecordFailure("glBlitFramebuffer", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBufferData(System.Int32 p0, System.UIntPtr p1, IntPtr p2, System.Int32 p3);
static void C_glBufferData(System.Int32 p0, System.UIntPtr p1, IntPtr p2, System.Int32 p3) { try { Dispatch("glBufferData", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glBufferData", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glBufferSubData(System.Int32 p0, System.IntPtr p1, System.UIntPtr p2, IntPtr p3);
static void C_glBufferSubData(System.Int32 p0, System.IntPtr p1, System.UIntPtr p2, IntPtr p3) { try { Dispatch("glBufferSubData", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glBufferSubData", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.Int32 D_glCheckFramebufferStatus(System.Int32 p0);
static System.Int32 C_glCheckFramebufferStatus(System.Int32 p0) { try { return (System.Int32)Dispatch("glCheckFramebufferStatus", new object[]{p0}); } catch(Exception e) { RecordFailure("glCheckFramebufferStatus", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glClear(System.UInt32 p0);
static void C_glClear(System.UInt32 p0) { try { Dispatch("glClear", new object[]{p0}); } catch(Exception e) { RecordFailure("glClear", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glClearColor(System.Single p0, System.Single p1, System.Single p2, System.Single p3);
static void C_glClearColor(System.Single p0, System.Single p1, System.Single p2, System.Single p3) { try { Dispatch("glClearColor", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glClearColor", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glClearDepthf(System.Single p0);
static void C_glClearDepthf(System.Single p0) { try { Dispatch("glClearDepthf", new object[]{p0}); } catch(Exception e) { RecordFailure("glClearDepthf", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glClearStencil(System.Int32 p0);
static void C_glClearStencil(System.Int32 p0) { try { Dispatch("glClearStencil", new object[]{p0}); } catch(Exception e) { RecordFailure("glClearStencil", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glColorMask(byte p0, byte p1, byte p2, byte p3);
static void C_glColorMask(byte p0, byte p1, byte p2, byte p3) { try { Dispatch("glColorMask", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glColorMask", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glColorMaski(System.UInt32 p0, byte p1, byte p2, byte p3, byte p4);
static void C_glColorMaski(System.UInt32 p0, byte p1, byte p2, byte p3, byte p4) { try { Dispatch("glColorMaski", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glColorMaski", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glCompileShader(System.UInt32 p0);
static void C_glCompileShader(System.UInt32 p0) { try { Dispatch("glCompileShader", new object[]{p0}); } catch(Exception e) { RecordFailure("glCompileShader", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glCompressedTexImage2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.UInt32 p4, System.Int32 p5, System.UInt32 p6, IntPtr p7);
static void C_glCompressedTexImage2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.UInt32 p4, System.Int32 p5, System.UInt32 p6, IntPtr p7) { try { Dispatch("glCompressedTexImage2D", new object[]{p0, p1, p2, p3, p4, p5, p6, p7}); } catch(Exception e) { RecordFailure("glCompressedTexImage2D", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.UInt32 D_glCreateProgram();
static System.UInt32 C_glCreateProgram() { try { return (System.UInt32)Dispatch("glCreateProgram", new object[]{}); } catch(Exception e) { RecordFailure("glCreateProgram", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.UInt32 D_glCreateShader(System.Int32 p0);
static System.UInt32 C_glCreateShader(System.Int32 p0) { try { return (System.UInt32)Dispatch("glCreateShader", new object[]{p0}); } catch(Exception e) { RecordFailure("glCreateShader", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glCullFace(System.Int32 p0);
static void C_glCullFace(System.Int32 p0) { try { Dispatch("glCullFace", new object[]{p0}); } catch(Exception e) { RecordFailure("glCullFace", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDeleteBuffers(System.UInt32 p0, IntPtr p1);
static void C_glDeleteBuffers(System.UInt32 p0, IntPtr p1) { try { Dispatch("glDeleteBuffers", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDeleteBuffers", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDeleteFramebuffers(System.UInt32 p0, IntPtr p1);
static void C_glDeleteFramebuffers(System.UInt32 p0, IntPtr p1) { try { Dispatch("glDeleteFramebuffers", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDeleteFramebuffers", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDeleteProgram(System.UInt32 p0);
static void C_glDeleteProgram(System.UInt32 p0) { try { Dispatch("glDeleteProgram", new object[]{p0}); } catch(Exception e) { RecordFailure("glDeleteProgram", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDeleteRenderbuffers(System.UInt32 p0, IntPtr p1);
static void C_glDeleteRenderbuffers(System.UInt32 p0, IntPtr p1) { try { Dispatch("glDeleteRenderbuffers", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDeleteRenderbuffers", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDeleteShader(System.UInt32 p0);
static void C_glDeleteShader(System.UInt32 p0) { try { Dispatch("glDeleteShader", new object[]{p0}); } catch(Exception e) { RecordFailure("glDeleteShader", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDeleteTextures(System.UInt32 p0, IntPtr p1);
static void C_glDeleteTextures(System.UInt32 p0, IntPtr p1) { try { Dispatch("glDeleteTextures", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDeleteTextures", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDepthFunc(System.Int32 p0);
static void C_glDepthFunc(System.Int32 p0) { try { Dispatch("glDepthFunc", new object[]{p0}); } catch(Exception e) { RecordFailure("glDepthFunc", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDepthMask(byte p0);
static void C_glDepthMask(byte p0) { try { Dispatch("glDepthMask", new object[]{p0}); } catch(Exception e) { RecordFailure("glDepthMask", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDepthRangef(System.Single p0, System.Single p1);
static void C_glDepthRangef(System.Single p0, System.Single p1) { try { Dispatch("glDepthRangef", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDepthRangef", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDetachShader(System.UInt32 p0, System.UInt32 p1);
static void C_glDetachShader(System.UInt32 p0, System.UInt32 p1) { try { Dispatch("glDetachShader", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDetachShader", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDisable(System.Int32 p0);
static void C_glDisable(System.Int32 p0) { try { Dispatch("glDisable", new object[]{p0}); } catch(Exception e) { RecordFailure("glDisable", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDisablei(System.Int32 p0, System.UInt32 p1);
static void C_glDisablei(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glDisablei", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glDisablei", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDisableVertexAttribArray(System.UInt32 p0);
static void C_glDisableVertexAttribArray(System.UInt32 p0) { try { Dispatch("glDisableVertexAttribArray", new object[]{p0}); } catch(Exception e) { RecordFailure("glDisableVertexAttribArray", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDrawArrays(System.Int32 p0, System.Int32 p1, System.UInt32 p2);
static void C_glDrawArrays(System.Int32 p0, System.Int32 p1, System.UInt32 p2) { try { Dispatch("glDrawArrays", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glDrawArrays", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glDrawElements(System.Int32 p0, System.UInt32 p1, System.Int32 p2, IntPtr p3);
static void C_glDrawElements(System.Int32 p0, System.UInt32 p1, System.Int32 p2, IntPtr p3) { try { Dispatch("glDrawElements", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glDrawElements", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glEnable(System.Int32 p0);
static void C_glEnable(System.Int32 p0) { try { Dispatch("glEnable", new object[]{p0}); } catch(Exception e) { RecordFailure("glEnable", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glEnablei(System.Int32 p0, System.UInt32 p1);
static void C_glEnablei(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glEnablei", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glEnablei", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glEnableVertexAttribArray(System.UInt32 p0);
static void C_glEnableVertexAttribArray(System.UInt32 p0) { try { Dispatch("glEnableVertexAttribArray", new object[]{p0}); } catch(Exception e) { RecordFailure("glEnableVertexAttribArray", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glFramebufferRenderbuffer(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3);
static void C_glFramebufferRenderbuffer(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3) { try { Dispatch("glFramebufferRenderbuffer", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glFramebufferRenderbuffer", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glFramebufferTexture2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.Int32 p4);
static void C_glFramebufferTexture2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.Int32 p4) { try { Dispatch("glFramebufferTexture2D", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glFramebufferTexture2D", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glFrontFace(System.Int32 p0);
static void C_glFrontFace(System.Int32 p0) { try { Dispatch("glFrontFace", new object[]{p0}); } catch(Exception e) { RecordFailure("glFrontFace", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGenBuffers(System.UInt32 p0, IntPtr p1);
static void C_glGenBuffers(System.UInt32 p0, IntPtr p1) { try { Dispatch("glGenBuffers", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGenBuffers", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGenerateMipmap(System.Int32 p0);
static void C_glGenerateMipmap(System.Int32 p0) { try { Dispatch("glGenerateMipmap", new object[]{p0}); } catch(Exception e) { RecordFailure("glGenerateMipmap", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGenFramebuffers(System.UInt32 p0, IntPtr p1);
static void C_glGenFramebuffers(System.UInt32 p0, IntPtr p1) { try { Dispatch("glGenFramebuffers", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGenFramebuffers", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGenRenderbuffers(System.UInt32 p0, IntPtr p1);
static void C_glGenRenderbuffers(System.UInt32 p0, IntPtr p1) { try { Dispatch("glGenRenderbuffers", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGenRenderbuffers", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGenTextures(System.UInt32 p0, IntPtr p1);
static void C_glGenTextures(System.UInt32 p0, IntPtr p1) { try { Dispatch("glGenTextures", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGenTextures", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetActiveAttrib(System.UInt32 p0, System.UInt32 p1, System.UInt32 p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6);
static void C_glGetActiveAttrib(System.UInt32 p0, System.UInt32 p1, System.UInt32 p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6) { try { Dispatch("glGetActiveAttrib", new object[]{p0, p1, p2, p3, p4, p5, p6}); } catch(Exception e) { RecordFailure("glGetActiveAttrib", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetActiveUniform(System.UInt32 p0, System.UInt32 p1, System.UInt32 p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6);
static void C_glGetActiveUniform(System.UInt32 p0, System.UInt32 p1, System.UInt32 p2, IntPtr p3, IntPtr p4, IntPtr p5, IntPtr p6) { try { Dispatch("glGetActiveUniform", new object[]{p0, p1, p2, p3, p4, p5, p6}); } catch(Exception e) { RecordFailure("glGetActiveUniform", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.Int32 D_glGetAttribLocation(System.UInt32 p0, IntPtr p1);
static System.Int32 C_glGetAttribLocation(System.UInt32 p0, IntPtr p1) { try { return (System.Int32)Dispatch("glGetAttribLocation", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGetAttribLocation", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.Int32 D_glGetError();
static System.Int32 C_glGetError() { try { return (System.Int32)Dispatch("glGetError", new object[]{}); } catch(Exception e) { RecordFailure("glGetError", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetFloatv(System.Int32 p0, IntPtr p1);
static void C_glGetFloatv(System.Int32 p0, IntPtr p1) { try { Dispatch("glGetFloatv", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGetFloatv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetIntegeri_v(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glGetIntegeri_v(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glGetIntegeri_v", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glGetIntegeri_v", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetIntegerv(System.Int32 p0, IntPtr p1);
static void C_glGetIntegerv(System.Int32 p0, IntPtr p1) { try { Dispatch("glGetIntegerv", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGetIntegerv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetProgramBinary(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3, IntPtr p4);
static void C_glGetProgramBinary(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3, IntPtr p4) { try { Dispatch("glGetProgramBinary", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glGetProgramBinary", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetProgramInfoLog(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3);
static void C_glGetProgramInfoLog(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3) { try { Dispatch("glGetProgramInfoLog", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glGetProgramInfoLog", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetProgramiv(System.UInt32 p0, System.Int32 p1, IntPtr p2);
static void C_glGetProgramiv(System.UInt32 p0, System.Int32 p1, IntPtr p2) { try { Dispatch("glGetProgramiv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glGetProgramiv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetShaderInfoLog(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3);
static void C_glGetShaderInfoLog(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3) { try { Dispatch("glGetShaderInfoLog", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glGetShaderInfoLog", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glGetShaderiv(System.UInt32 p0, System.Int32 p1, IntPtr p2);
static void C_glGetShaderiv(System.UInt32 p0, System.Int32 p1, IntPtr p2) { try { Dispatch("glGetShaderiv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glGetShaderiv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr D_glGetString(System.Int32 p0);
static IntPtr C_glGetString(System.Int32 p0) { try { return (IntPtr)Dispatch("glGetString", new object[]{p0}); } catch(Exception e) { RecordFailure("glGetString", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr D_glGetStringi(System.Int32 p0, System.UInt32 p1);
static IntPtr C_glGetStringi(System.Int32 p0, System.UInt32 p1) { try { return (IntPtr)Dispatch("glGetStringi", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGetStringi", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.UInt32 D_glGetUniformBlockIndex(System.UInt32 p0, IntPtr p1);
static System.UInt32 C_glGetUniformBlockIndex(System.UInt32 p0, IntPtr p1) { try { return (System.UInt32)Dispatch("glGetUniformBlockIndex", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGetUniformBlockIndex", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate System.Int32 D_glGetUniformLocation(System.UInt32 p0, IntPtr p1);
static System.Int32 C_glGetUniformLocation(System.UInt32 p0, IntPtr p1) { try { return (System.Int32)Dispatch("glGetUniformLocation", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glGetUniformLocation", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate byte D_glIsEnabled(System.Int32 p0);
static byte C_glIsEnabled(System.Int32 p0) { try { return (byte)Dispatch("glIsEnabled", new object[]{p0}); } catch(Exception e) { RecordFailure("glIsEnabled", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate byte D_glIsEnabledi(System.Int32 p0, System.UInt32 p1);
static byte C_glIsEnabledi(System.Int32 p0, System.UInt32 p1) { try { return (byte)Dispatch("glIsEnabledi", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glIsEnabledi", e); return default; } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glLineWidth(System.Single p0);
static void C_glLineWidth(System.Single p0) { try { Dispatch("glLineWidth", new object[]{p0}); } catch(Exception e) { RecordFailure("glLineWidth", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glLinkProgram(System.UInt32 p0);
static void C_glLinkProgram(System.UInt32 p0) { try { Dispatch("glLinkProgram", new object[]{p0}); } catch(Exception e) { RecordFailure("glLinkProgram", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glPixelStorei(System.Int32 p0, System.Int32 p1);
static void C_glPixelStorei(System.Int32 p0, System.Int32 p1) { try { Dispatch("glPixelStorei", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glPixelStorei", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glPolygonOffset(System.Single p0, System.Single p1);
static void C_glPolygonOffset(System.Single p0, System.Single p1) { try { Dispatch("glPolygonOffset", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glPolygonOffset", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glProgramBinary(System.UInt32 p0, System.Int32 p1, IntPtr p2, System.UInt32 p3);
static void C_glProgramBinary(System.UInt32 p0, System.Int32 p1, IntPtr p2, System.UInt32 p3) { try { Dispatch("glProgramBinary", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glProgramBinary", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glReadPixels(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3, System.Int32 p4, System.Int32 p5, IntPtr p6);
static void C_glReadPixels(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3, System.Int32 p4, System.Int32 p5, IntPtr p6) { try { Dispatch("glReadPixels", new object[]{p0, p1, p2, p3, p4, p5, p6}); } catch(Exception e) { RecordFailure("glReadPixels", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glRenderbufferStorage(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3);
static void C_glRenderbufferStorage(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3) { try { Dispatch("glRenderbufferStorage", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glRenderbufferStorage", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glScissor(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3);
static void C_glScissor(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3) { try { Dispatch("glScissor", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glScissor", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glShaderSource(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3);
static void C_glShaderSource(System.UInt32 p0, System.UInt32 p1, IntPtr p2, IntPtr p3) { try { Dispatch("glShaderSource", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glShaderSource", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexImage2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.UInt32 p4, System.Int32 p5, System.Int32 p6, System.Int32 p7, IntPtr p8);
static void C_glTexImage2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.UInt32 p4, System.Int32 p5, System.Int32 p6, System.Int32 p7, IntPtr p8) { try { Dispatch("glTexImage2D", new object[]{p0, p1, p2, p3, p4, p5, p6, p7, p8}); } catch(Exception e) { RecordFailure("glTexImage2D", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexImage3D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.UInt32 p4, System.UInt32 p5, System.Int32 p6, System.Int32 p7, System.Int32 p8, IntPtr p9);
static void C_glTexImage3D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.UInt32 p3, System.UInt32 p4, System.UInt32 p5, System.Int32 p6, System.Int32 p7, System.Int32 p8, IntPtr p9) { try { Dispatch("glTexImage3D", new object[]{p0, p1, p2, p3, p4, p5, p6, p7, p8, p9}); } catch(Exception e) { RecordFailure("glTexImage3D", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexParameterf(System.Int32 p0, System.Int32 p1, System.Single p2);
static void C_glTexParameterf(System.Int32 p0, System.Int32 p1, System.Single p2) { try { Dispatch("glTexParameterf", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glTexParameterf", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexParameterfv(System.Int32 p0, System.Int32 p1, IntPtr p2);
static void C_glTexParameterfv(System.Int32 p0, System.Int32 p1, IntPtr p2) { try { Dispatch("glTexParameterfv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glTexParameterfv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexParameteri(System.Int32 p0, System.Int32 p1, System.Int32 p2);
static void C_glTexParameteri(System.Int32 p0, System.Int32 p1, System.Int32 p2) { try { Dispatch("glTexParameteri", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glTexParameteri", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexParameteriv(System.Int32 p0, System.Int32 p1, IntPtr p2);
static void C_glTexParameteriv(System.Int32 p0, System.Int32 p1, IntPtr p2) { try { Dispatch("glTexParameteriv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glTexParameteriv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexSubImage2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.UInt32 p4, System.UInt32 p5, System.Int32 p6, System.Int32 p7, IntPtr p8);
static void C_glTexSubImage2D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.UInt32 p4, System.UInt32 p5, System.Int32 p6, System.Int32 p7, IntPtr p8) { try { Dispatch("glTexSubImage2D", new object[]{p0, p1, p2, p3, p4, p5, p6, p7, p8}); } catch(Exception e) { RecordFailure("glTexSubImage2D", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glTexSubImage3D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4, System.UInt32 p5, System.UInt32 p6, System.UInt32 p7, System.Int32 p8, System.Int32 p9, IntPtr p10);
static void C_glTexSubImage3D(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4, System.UInt32 p5, System.UInt32 p6, System.UInt32 p7, System.Int32 p8, System.Int32 p9, IntPtr p10) { try { Dispatch("glTexSubImage3D", new object[]{p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10}); } catch(Exception e) { RecordFailure("glTexSubImage3D", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform1f(System.Int32 p0, System.Single p1);
static void C_glUniform1f(System.Int32 p0, System.Single p1) { try { Dispatch("glUniform1f", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glUniform1f", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform1fv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform1fv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform1fv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform1fv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform1i(System.Int32 p0, System.Int32 p1);
static void C_glUniform1i(System.Int32 p0, System.Int32 p1) { try { Dispatch("glUniform1i", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glUniform1i", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform1iv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform1iv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform1iv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform1iv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform1ui(System.Int32 p0, System.UInt32 p1);
static void C_glUniform1ui(System.Int32 p0, System.UInt32 p1) { try { Dispatch("glUniform1ui", new object[]{p0, p1}); } catch(Exception e) { RecordFailure("glUniform1ui", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform1uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform1uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform1uiv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform1uiv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform2f(System.Int32 p0, System.Single p1, System.Single p2);
static void C_glUniform2f(System.Int32 p0, System.Single p1, System.Single p2) { try { Dispatch("glUniform2f", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform2f", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform2fv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform2fv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform2fv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform2fv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform2i(System.Int32 p0, System.Int32 p1, System.Int32 p2);
static void C_glUniform2i(System.Int32 p0, System.Int32 p1, System.Int32 p2) { try { Dispatch("glUniform2i", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform2i", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform2iv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform2iv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform2iv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform2iv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform2ui(System.Int32 p0, System.UInt32 p1, System.UInt32 p2);
static void C_glUniform2ui(System.Int32 p0, System.UInt32 p1, System.UInt32 p2) { try { Dispatch("glUniform2ui", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform2ui", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform2uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform2uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform2uiv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform2uiv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform3f(System.Int32 p0, System.Single p1, System.Single p2, System.Single p3);
static void C_glUniform3f(System.Int32 p0, System.Single p1, System.Single p2, System.Single p3) { try { Dispatch("glUniform3f", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glUniform3f", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform3fv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform3fv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform3fv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform3fv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform3i(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3);
static void C_glUniform3i(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3) { try { Dispatch("glUniform3i", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glUniform3i", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform3iv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform3iv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform3iv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform3iv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform3ui(System.Int32 p0, System.UInt32 p1, System.UInt32 p2, System.UInt32 p3);
static void C_glUniform3ui(System.Int32 p0, System.UInt32 p1, System.UInt32 p2, System.UInt32 p3) { try { Dispatch("glUniform3ui", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glUniform3ui", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform3uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform3uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform3uiv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform3uiv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform4f(System.Int32 p0, System.Single p1, System.Single p2, System.Single p3, System.Single p4);
static void C_glUniform4f(System.Int32 p0, System.Single p1, System.Single p2, System.Single p3, System.Single p4) { try { Dispatch("glUniform4f", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glUniform4f", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform4fv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform4fv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform4fv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform4fv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform4i(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4);
static void C_glUniform4i(System.Int32 p0, System.Int32 p1, System.Int32 p2, System.Int32 p3, System.Int32 p4) { try { Dispatch("glUniform4i", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glUniform4i", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform4iv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform4iv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform4iv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform4iv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform4ui(System.Int32 p0, System.UInt32 p1, System.UInt32 p2, System.UInt32 p3, System.UInt32 p4);
static void C_glUniform4ui(System.Int32 p0, System.UInt32 p1, System.UInt32 p2, System.UInt32 p3, System.UInt32 p4) { try { Dispatch("glUniform4ui", new object[]{p0, p1, p2, p3, p4}); } catch(Exception e) { RecordFailure("glUniform4ui", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniform4uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2);
static void C_glUniform4uiv(System.Int32 p0, System.UInt32 p1, IntPtr p2) { try { Dispatch("glUniform4uiv", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniform4uiv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniformBlockBinding(System.UInt32 p0, System.UInt32 p1, System.UInt32 p2);
static void C_glUniformBlockBinding(System.UInt32 p0, System.UInt32 p1, System.UInt32 p2) { try { Dispatch("glUniformBlockBinding", new object[]{p0, p1, p2}); } catch(Exception e) { RecordFailure("glUniformBlockBinding", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniformMatrix3fv(System.Int32 p0, System.UInt32 p1, byte p2, IntPtr p3);
static void C_glUniformMatrix3fv(System.Int32 p0, System.UInt32 p1, byte p2, IntPtr p3) { try { Dispatch("glUniformMatrix3fv", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glUniformMatrix3fv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUniformMatrix4fv(System.Int32 p0, System.UInt32 p1, byte p2, IntPtr p3);
static void C_glUniformMatrix4fv(System.Int32 p0, System.UInt32 p1, byte p2, IntPtr p3) { try { Dispatch("glUniformMatrix4fv", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glUniformMatrix4fv", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glUseProgram(System.UInt32 p0);
static void C_glUseProgram(System.UInt32 p0) { try { Dispatch("glUseProgram", new object[]{p0}); } catch(Exception e) { RecordFailure("glUseProgram", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glVertexAttribPointer(System.UInt32 p0, System.Int32 p1, System.Int32 p2, byte p3, System.UInt32 p4, IntPtr p5);
static void C_glVertexAttribPointer(System.UInt32 p0, System.Int32 p1, System.Int32 p2, byte p3, System.UInt32 p4, IntPtr p5) { try { Dispatch("glVertexAttribPointer", new object[]{p0, p1, p2, p3, p4, p5}); } catch(Exception e) { RecordFailure("glVertexAttribPointer", e);  } }
[UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void D_glViewport(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3);
static void C_glViewport(System.Int32 p0, System.Int32 p1, System.UInt32 p2, System.UInt32 p3) { try { Dispatch("glViewport", new object[]{p0, p1, p2, p3}); } catch(Exception e) { RecordFailure("glViewport", e);  } }
internal static IntPtr Resolve(string name) { if (pointers.TryGetValue(name, out var p)) return p; Delegate d; switch(name) {
case "glActiveTexture": d = new D_glActiveTexture(C_glActiveTexture); break;
case "glAttachShader": d = new D_glAttachShader(C_glAttachShader); break;
case "glBindBuffer": d = new D_glBindBuffer(C_glBindBuffer); break;
case "glBindBufferBase": d = new D_glBindBufferBase(C_glBindBufferBase); break;
case "glBindFramebuffer": d = new D_glBindFramebuffer(C_glBindFramebuffer); break;
case "glBindRenderbuffer": d = new D_glBindRenderbuffer(C_glBindRenderbuffer); break;
case "glBindTexture": d = new D_glBindTexture(C_glBindTexture); break;
case "glBlendColor": d = new D_glBlendColor(C_glBlendColor); break;
case "glBlendEquation": d = new D_glBlendEquation(C_glBlendEquation); break;
case "glBlendEquationi": d = new D_glBlendEquationi(C_glBlendEquationi); break;
case "glBlendEquationSeparate": d = new D_glBlendEquationSeparate(C_glBlendEquationSeparate); break;
case "glBlendEquationSeparatei": d = new D_glBlendEquationSeparatei(C_glBlendEquationSeparatei); break;
case "glBlendFunc": d = new D_glBlendFunc(C_glBlendFunc); break;
case "glBlendFunci": d = new D_glBlendFunci(C_glBlendFunci); break;
case "glBlendFuncSeparate": d = new D_glBlendFuncSeparate(C_glBlendFuncSeparate); break;
case "glBlendFuncSeparatei": d = new D_glBlendFuncSeparatei(C_glBlendFuncSeparatei); break;
case "glBlitFramebuffer": d = new D_glBlitFramebuffer(C_glBlitFramebuffer); break;
case "glBufferData": d = new D_glBufferData(C_glBufferData); break;
case "glBufferSubData": d = new D_glBufferSubData(C_glBufferSubData); break;
case "glCheckFramebufferStatus": d = new D_glCheckFramebufferStatus(C_glCheckFramebufferStatus); break;
case "glClear": d = new D_glClear(C_glClear); break;
case "glClearColor": d = new D_glClearColor(C_glClearColor); break;
case "glClearDepthf": d = new D_glClearDepthf(C_glClearDepthf); break;
case "glClearStencil": d = new D_glClearStencil(C_glClearStencil); break;
case "glColorMask": d = new D_glColorMask(C_glColorMask); break;
case "glColorMaski": d = new D_glColorMaski(C_glColorMaski); break;
case "glCompileShader": d = new D_glCompileShader(C_glCompileShader); break;
case "glCompressedTexImage2D": d = new D_glCompressedTexImage2D(C_glCompressedTexImage2D); break;
case "glCreateProgram": d = new D_glCreateProgram(C_glCreateProgram); break;
case "glCreateShader": d = new D_glCreateShader(C_glCreateShader); break;
case "glCullFace": d = new D_glCullFace(C_glCullFace); break;
case "glDeleteBuffers": d = new D_glDeleteBuffers(C_glDeleteBuffers); break;
case "glDeleteFramebuffers": d = new D_glDeleteFramebuffers(C_glDeleteFramebuffers); break;
case "glDeleteProgram": d = new D_glDeleteProgram(C_glDeleteProgram); break;
case "glDeleteRenderbuffers": d = new D_glDeleteRenderbuffers(C_glDeleteRenderbuffers); break;
case "glDeleteShader": d = new D_glDeleteShader(C_glDeleteShader); break;
case "glDeleteTextures": d = new D_glDeleteTextures(C_glDeleteTextures); break;
case "glDepthFunc": d = new D_glDepthFunc(C_glDepthFunc); break;
case "glDepthMask": d = new D_glDepthMask(C_glDepthMask); break;
case "glDepthRangef": d = new D_glDepthRangef(C_glDepthRangef); break;
case "glDetachShader": d = new D_glDetachShader(C_glDetachShader); break;
case "glDisable": d = new D_glDisable(C_glDisable); break;
case "glDisablei": d = new D_glDisablei(C_glDisablei); break;
case "glDisableVertexAttribArray": d = new D_glDisableVertexAttribArray(C_glDisableVertexAttribArray); break;
case "glDrawArrays": d = new D_glDrawArrays(C_glDrawArrays); break;
case "glDrawElements": d = new D_glDrawElements(C_glDrawElements); break;
case "glEnable": d = new D_glEnable(C_glEnable); break;
case "glEnablei": d = new D_glEnablei(C_glEnablei); break;
case "glEnableVertexAttribArray": d = new D_glEnableVertexAttribArray(C_glEnableVertexAttribArray); break;
case "glFramebufferRenderbuffer": d = new D_glFramebufferRenderbuffer(C_glFramebufferRenderbuffer); break;
case "glFramebufferTexture2D": d = new D_glFramebufferTexture2D(C_glFramebufferTexture2D); break;
case "glFrontFace": d = new D_glFrontFace(C_glFrontFace); break;
case "glGenBuffers": d = new D_glGenBuffers(C_glGenBuffers); break;
case "glGenerateMipmap": d = new D_glGenerateMipmap(C_glGenerateMipmap); break;
case "glGenFramebuffers": d = new D_glGenFramebuffers(C_glGenFramebuffers); break;
case "glGenRenderbuffers": d = new D_glGenRenderbuffers(C_glGenRenderbuffers); break;
case "glGenTextures": d = new D_glGenTextures(C_glGenTextures); break;
case "glGetActiveAttrib": d = new D_glGetActiveAttrib(C_glGetActiveAttrib); break;
case "glGetActiveUniform": d = new D_glGetActiveUniform(C_glGetActiveUniform); break;
case "glGetAttribLocation": d = new D_glGetAttribLocation(C_glGetAttribLocation); break;
case "glGetError": d = new D_glGetError(C_glGetError); break;
case "glGetFloatv": d = new D_glGetFloatv(C_glGetFloatv); break;
case "glGetIntegeri_v": d = new D_glGetIntegeri_v(C_glGetIntegeri_v); break;
case "glGetIntegerv": d = new D_glGetIntegerv(C_glGetIntegerv); break;
case "glGetProgramBinary": d = new D_glGetProgramBinary(C_glGetProgramBinary); break;
case "glGetProgramInfoLog": d = new D_glGetProgramInfoLog(C_glGetProgramInfoLog); break;
case "glGetProgramiv": d = new D_glGetProgramiv(C_glGetProgramiv); break;
case "glGetShaderInfoLog": d = new D_glGetShaderInfoLog(C_glGetShaderInfoLog); break;
case "glGetShaderiv": d = new D_glGetShaderiv(C_glGetShaderiv); break;
case "glGetString": d = new D_glGetString(C_glGetString); break;
case "glGetStringi": d = new D_glGetStringi(C_glGetStringi); break;
case "glGetUniformBlockIndex": d = new D_glGetUniformBlockIndex(C_glGetUniformBlockIndex); break;
case "glGetUniformLocation": d = new D_glGetUniformLocation(C_glGetUniformLocation); break;
case "glIsEnabled": d = new D_glIsEnabled(C_glIsEnabled); break;
case "glIsEnabledi": d = new D_glIsEnabledi(C_glIsEnabledi); break;
case "glLineWidth": d = new D_glLineWidth(C_glLineWidth); break;
case "glLinkProgram": d = new D_glLinkProgram(C_glLinkProgram); break;
case "glPixelStorei": d = new D_glPixelStorei(C_glPixelStorei); break;
case "glPolygonOffset": d = new D_glPolygonOffset(C_glPolygonOffset); break;
case "glProgramBinary": d = new D_glProgramBinary(C_glProgramBinary); break;
case "glReadPixels": d = new D_glReadPixels(C_glReadPixels); break;
case "glRenderbufferStorage": d = new D_glRenderbufferStorage(C_glRenderbufferStorage); break;
case "glScissor": d = new D_glScissor(C_glScissor); break;
case "glShaderSource": d = new D_glShaderSource(C_glShaderSource); break;
case "glTexImage2D": d = new D_glTexImage2D(C_glTexImage2D); break;
case "glTexImage3D": d = new D_glTexImage3D(C_glTexImage3D); break;
case "glTexParameterf": d = new D_glTexParameterf(C_glTexParameterf); break;
case "glTexParameterfv": d = new D_glTexParameterfv(C_glTexParameterfv); break;
case "glTexParameteri": d = new D_glTexParameteri(C_glTexParameteri); break;
case "glTexParameteriv": d = new D_glTexParameteriv(C_glTexParameteriv); break;
case "glTexSubImage2D": d = new D_glTexSubImage2D(C_glTexSubImage2D); break;
case "glTexSubImage3D": d = new D_glTexSubImage3D(C_glTexSubImage3D); break;
case "glUniform1f": d = new D_glUniform1f(C_glUniform1f); break;
case "glUniform1fv": d = new D_glUniform1fv(C_glUniform1fv); break;
case "glUniform1i": d = new D_glUniform1i(C_glUniform1i); break;
case "glUniform1iv": d = new D_glUniform1iv(C_glUniform1iv); break;
case "glUniform1ui": d = new D_glUniform1ui(C_glUniform1ui); break;
case "glUniform1uiv": d = new D_glUniform1uiv(C_glUniform1uiv); break;
case "glUniform2f": d = new D_glUniform2f(C_glUniform2f); break;
case "glUniform2fv": d = new D_glUniform2fv(C_glUniform2fv); break;
case "glUniform2i": d = new D_glUniform2i(C_glUniform2i); break;
case "glUniform2iv": d = new D_glUniform2iv(C_glUniform2iv); break;
case "glUniform2ui": d = new D_glUniform2ui(C_glUniform2ui); break;
case "glUniform2uiv": d = new D_glUniform2uiv(C_glUniform2uiv); break;
case "glUniform3f": d = new D_glUniform3f(C_glUniform3f); break;
case "glUniform3fv": d = new D_glUniform3fv(C_glUniform3fv); break;
case "glUniform3i": d = new D_glUniform3i(C_glUniform3i); break;
case "glUniform3iv": d = new D_glUniform3iv(C_glUniform3iv); break;
case "glUniform3ui": d = new D_glUniform3ui(C_glUniform3ui); break;
case "glUniform3uiv": d = new D_glUniform3uiv(C_glUniform3uiv); break;
case "glUniform4f": d = new D_glUniform4f(C_glUniform4f); break;
case "glUniform4fv": d = new D_glUniform4fv(C_glUniform4fv); break;
case "glUniform4i": d = new D_glUniform4i(C_glUniform4i); break;
case "glUniform4iv": d = new D_glUniform4iv(C_glUniform4iv); break;
case "glUniform4ui": d = new D_glUniform4ui(C_glUniform4ui); break;
case "glUniform4uiv": d = new D_glUniform4uiv(C_glUniform4uiv); break;
case "glUniformBlockBinding": d = new D_glUniformBlockBinding(C_glUniformBlockBinding); break;
case "glUniformMatrix3fv": d = new D_glUniformMatrix3fv(C_glUniformMatrix3fv); break;
case "glUniformMatrix4fv": d = new D_glUniformMatrix4fv(C_glUniformMatrix4fv); break;
case "glUseProgram": d = new D_glUseProgram(C_glUseProgram); break;
case "glVertexAttribPointer": d = new D_glVertexAttribPointer(C_glVertexAttribPointer); break;
case "glViewport": d = new D_glViewport(C_glViewport); break;
default: return IntPtr.Zero; } callbacks.Add(name,d); p=Marshal.GetFunctionPointerForDelegate(d); pointers.Add(name,p); return p; } } }
