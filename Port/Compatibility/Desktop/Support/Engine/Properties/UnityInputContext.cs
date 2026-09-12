using Silk.NET.Input;
using Silk.NET.Core;
using NVector2=System.Numerics.Vector2;
namespace Engine.UnityRuntime {
 internal sealed class UnityInputContext:IInputContext {
  internal readonly UnityKeyboard Keyboard=new();internal readonly UnityMouse Mouse=new();
  public IntPtr Handle=>Host.WindowHandle;
  public IReadOnlyList<IGamepad> Gamepads{get;}=Array.Empty<IGamepad>();
  public IReadOnlyList<IJoystick> Joysticks{get;}=Array.Empty<IJoystick>();
  public IReadOnlyList<IKeyboard> Keyboards{get;}
  public IReadOnlyList<IMouse> Mice{get;}
  public IReadOnlyList<IInputDevice> OtherDevices{get;}=Array.Empty<IInputDevice>();
  public event Action<IInputDevice,bool> ConnectionChanged;
  internal UnityInputContext(){Keyboards=new IKeyboard[]{Keyboard};Mice=new IMouse[]{Mouse};}
  public void Dispose(){Keyboard.Dispose();Mouse.Dispose();}
 }
 internal sealed class UnityKeyboard:IKeyboard {
  readonly HashSet<Key> keys=new();
  public string Name=>"Unity keyboard";public int Index=>0;public bool IsConnected=>true;
  public IReadOnlyList<Key> SupportedKeys{get;}=(Key[])Enum.GetValues(typeof(Key));
  public string ClipboardText {get=>Host.GetClipboard?.Invoke()??string.Empty;set=>Host.SetClipboard?.Invoke(value);}
  public event Action<IKeyboard,Key,int> KeyDown,KeyUp;
  public event Action<IKeyboard,char> KeyChar;
  public bool IsKeyPressed(Key key)=>keys.Contains(key);
  public bool IsScancodePressed(int scancode)=>Host.IsScanCodePressed?.Invoke(scancode)??false;
  internal void SetKey(Key key,bool pressed,int scancode){if(pressed){if(keys.Add(key))KeyDown?.Invoke(this,key,scancode);}else if(keys.Remove(key))KeyUp?.Invoke(this,key,scancode);}
  internal void Text(char c)=>KeyChar?.Invoke(this,c);
  public void BeginInput(){}public void EndInput(){}public void Dispose(){keys.Clear();}
 }
 internal sealed class UnityCursor:ICursor {
  public CursorType Type{get;set;}
  public StandardCursor StandardCursor{get;set;}
  public CursorMode CursorMode{get;set;}
  public bool IsConfined{get;set;}
  public int HotspotX{get;set;}public int HotspotY{get;set;}
  public RawImage Image{get;set;}
  public bool IsSupported(CursorMode mode)=>mode==CursorMode.Normal||mode==CursorMode.Raw||mode==CursorMode.Hidden||mode==CursorMode.Disabled;
  public bool IsSupported(StandardCursor cursor)=>true;
 }
 internal sealed class UnityMouse:IMouse {
  readonly HashSet<MouseButton> buttons=new();NVector2 position;readonly ScrollWheel[] wheels=new ScrollWheel[1];
  double lastClick;MouseButton lastButton;NVector2 lastPosition;
  public string Name=>"Unity mouse";public int Index=>0;public bool IsConnected=>true;
  public IReadOnlyList<MouseButton> SupportedButtons{get;}=new[]{MouseButton.Left,MouseButton.Right,MouseButton.Middle,MouseButton.Button4,MouseButton.Button5};
  public IReadOnlyList<ScrollWheel> ScrollWheels=>wheels;
  public NVector2 Position{get=>position;set{position=value;Host.WarpMouse?.Invoke(new Point2((int)value.X,(int)value.Y));}}
  public ICursor Cursor{get;}=new UnityCursor();
  public int DoubleClickTime{get;set;}=500;public int DoubleClickRange{get;set;}=4;
  public event Action<IMouse,MouseButton> MouseDown,MouseUp;
  public event Action<IMouse,MouseButton,NVector2> Click,DoubleClick;
  public event Action<IMouse,NVector2> MouseMove;
  public event Action<IMouse,ScrollWheel> Scroll;
  public bool IsButtonPressed(MouseButton button)=>buttons.Contains(button);
  internal void Move(NVector2 value){if(position!=value){position=value;MouseMove?.Invoke(this,value);}}
  internal void Wheel(float x,float y){wheels[0]=new ScrollWheel(x,y);if(x!=0||y!=0)Scroll?.Invoke(this,wheels[0]);}
  internal void SetButton(MouseButton button,bool pressed){if(pressed){if(buttons.Add(button))MouseDown?.Invoke(this,button);}else if(buttons.Remove(button)){MouseUp?.Invoke(this,button);Click?.Invoke(this,button,position);double now=Time.RealTime;if(button==lastButton&&(now-lastClick)*1000<=DoubleClickTime&&NVector2.Distance(position,lastPosition)<=DoubleClickRange)DoubleClick?.Invoke(this,button,position);lastClick=now;lastButton=button;lastPosition=position;}}
  public void Dispose(){buttons.Clear();}
 }
}
