// Compatibility events remain available to mods; this host does not raise all of them.
#pragma warning disable CS0067
using Engine.Audio;
using Engine.Graphics;
using Engine.Input;
using System.Runtime.InteropServices;
using Silk.NET.Windowing;
using Silk.NET.Input;
using H = Engine.UnityRuntime.Host;
namespace Engine {
 public static class Window {
  public const string InputLibrary="Silk.NET.Input.Glfw", WindowingLibrary="Silk.NET.Windowing.Glfw";
  public static IView m_view;
  public static IWindow m_gameWindow;
  public static IInputContext m_inputContext;
  public static string m_titlePrefix=string.Empty,m_titleSuffix=string.Empty;
  public static float m_lastRenderDelta;
  static Point2 size,position;
  static WindowMode mode;
  static bool closing,restarting;
  public static Point2 ScreenSize=>H.ScreenSize;
  public static Point2 Position {get{Verify();return position;}set{if(IsCreated)position=value;}}
  public static Point2 Size {get{Verify();return size;}set{if(IsCreated)HostResize(value);}}
  public static WindowMode WindowMode {get{Verify();return mode;}set{if(IsCreated)mode=value;}}
  public static float Scale {get;set;}=1f;
  public static bool HasWideNotch {get;set;}
  public static Vector4 DisplayCutoutInsets {get;set;}=Vector4.Zero;
  public static string TitlePrefix {get{Verify();return m_titlePrefix;}set{if(IsCreated)m_titlePrefix=value;}}
  public static string TitleSuffix {get{Verify();return m_titleSuffix;}set{if(IsCreated)m_titleSuffix=value;}}
  public static string Title {get{Verify();return m_titlePrefix+m_titleSuffix;}set{if(IsCreated){m_titlePrefix=value;m_titleSuffix=string.Empty;}}}
  public static int PresentationInterval {get;set;}=1;
  public static int ScreenRefreshRate=>60;
  public static IntPtr Handle=>H.WindowHandle;
  static bool created; public static bool IsCreated => created;
  static bool active; public static bool IsActive => active;
  public static event Action Created,Resized,Activated,Deactivated,Closed,ToRestart,Frame,LowMemory;
  public static event Action<Vector4,bool> DisplayCutoutInsetsChanged;
  public static event Action<UnhandledExceptionInfo> UnhandledException;
  public static event Action<Uri> HandleUri;
  public static event Action<List<(Stream stream,string fileName)>> FileDropped;
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int MessageBox(IntPtr hWnd,string text,string caption,uint type);
  public static void Run(int width=0,int height=0,WindowMode windowMode=WindowMode.Resizable,string title=""){
   if(IsCreated)throw new InvalidOperationException("Window already opened.");
   if(H.AppPath==null||H.DataPath==null)throw new InvalidOperationException("Unity host must configure storage before Run.");
   size=new(width>0?width:H.ScreenSize.X,height>0?height:H.ScreenSize.Y);mode=windowMode;m_titlePrefix=title;
   created=true;active=true;
   m_inputContext=H.InputContext;Dispatcher.Initialize();Display.Initialize();Keyboard.Initialize();Mouse.Initialize();Touch.Initialize();GamePad.Initialize();Mixer.Initialize();
   Created?.Invoke();Activated?.Invoke();
  }
  public static void Close(){closing=true;}
  public static void Restart(){restarting=true;closing=true;}
  public static void DisplayCutoutInsetsChangedHandler(Vector4 insets,bool hasWideNotch){DisplayCutoutInsets=insets;HasWideNotch=hasWideNotch;DisplayCutoutInsetsChanged?.Invoke(insets,hasWideNotch);}
  static void Verify(){if(!IsCreated)throw new InvalidOperationException("Window not opened.");}
  internal static void HostFrame(float delta){
   Verify();m_lastRenderDelta=delta;
   H.Mark("BeforeFrame.Time");Time.BeforeFrame();H.Mark("BeforeFrame.Dispatcher");Dispatcher.BeforeFrame();H.Mark("BeforeFrame.Display");Display.BeforeFrame();
   H.Mark("BeforeFrame.Keyboard");Keyboard.BeforeFrame();H.Mark("BeforeFrame.Mouse");Mouse.BeforeFrame();H.Mark("BeforeFrame.Touch");Touch.BeforeFrame();H.Mark("BeforeFrame.GamePad");GamePad.BeforeFrame();H.Mark("BeforeFrame.Mixer");Mixer.BeforeFrame();
   H.Mark("Frame");Frame?.Invoke();
   H.Mark("AfterFrame.Time");Time.AfterFrame();H.Mark("AfterFrame.Dispatcher");Dispatcher.AfterFrame();H.Mark("AfterFrame.Display");Display.AfterFrame();
   H.Mark("AfterFrame.Keyboard");Keyboard.AfterFrame();H.Mark("AfterFrame.Mouse");Mouse.AfterFrame();H.Mark("AfterFrame.Touch");Touch.AfterFrame();H.Mark("AfterFrame.GamePad");GamePad.AfterFrame();H.Mark("AfterFrame.Mixer");Mixer.AfterFrame();
   if(closing){HostDispose();if(restarting)ToRestart?.Invoke();}
  }
  internal static void HostFocus(bool active){if(IsActive==active)return;Window.active=active;if(active)Activated?.Invoke();else{Keyboard.Clear();Mouse.Clear();Touch.Clear();GamePad.Clear();Deactivated?.Invoke();}}
  internal static void HostResize(Point2 value){if(size==value)return;size=value;Resized?.Invoke();}
  internal static void HostDispose(){if(!IsCreated)return;Closed?.Invoke();Display.Dispose();Keyboard.Dispose();Mouse.Dispose();Touch.Dispose();GamePad.Dispose();Mixer.Dispose();Log.Dispose();created=false;active=false;}
 }
}

#pragma warning restore CS0067
