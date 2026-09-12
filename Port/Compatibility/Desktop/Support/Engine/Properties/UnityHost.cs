namespace Engine.UnityRuntime {
 public static class Host {
  public static string AppPath, DataPath;
  public static Point2 ScreenSize = new(1280,720), MousePosition, MouseDelta;
  public static int MouseWheel, MouseWheelX;
  public static IntPtr WindowHandle;
  internal static UnityInputContext InputContext=new();
  public static Func<string> GetClipboard;
  public static Action<string> SetClipboard;
  public static Func<int,bool> IsScanCodePressed;
  public static Action<Point2> WarpMouse;
  public static void KeyInput(int key,bool down,int scancode=0)=>InputContext.Keyboard.SetKey((Silk.NET.Input.Key)key,down,scancode);
  public static void MouseInput(int button,bool down)=>InputContext.Mouse.SetButton((Silk.NET.Input.MouseButton)button,down);
  public static void MouseMoveInput(int x,int y,int dx,int dy,float wheelX,float wheelY){MousePosition=new(x,y);MouseDelta=new(dx,dy);InputContext.Mouse.Move(new System.Numerics.Vector2(x,y));InputContext.Mouse.Wheel(wheelX,wheelY);}
  public static bool ImeEnabled;
  public static Point2 ImePosition;
  public static event Action<char> ImeText;
  public static event Action<string,int> ImeComposition;
  public static void TextInput(char c){InputContext.Keyboard.Text(c);if(ImeEnabled)ImeText?.Invoke(c);}
  public static void CompositionInput(string text,int cursor){if(ImeEnabled)ImeComposition?.Invoke(text,cursor);}
  public static void GamePadState(int index,bool connected,Vector2 left,Vector2 right,float leftTrigger,float rightTrigger,bool[] buttons){
   if(index<0||index>=4)throw new ArgumentOutOfRangeException(nameof(index));var s=Input.GamePad.m_states[index];s.IsConnected=connected;if(!connected)return;
   s.Sticks[0]=left;s.Sticks[1]=right;s.Triggers[0]=leftTrigger;s.Triggers[1]=rightTrigger;Array.Copy(buttons,s.Buttons,14);
  }
  public static Action<DrawPacket> Draw;
  public static Action<Color?,float?,int?> Clear;
  public static event Action<string> Trace;
  internal static void Mark(string name)=>Trace?.Invoke(name);
  public static void Configure(string appPath,string dataPath,int width,int height){
   if(Window.IsCreated)throw new InvalidOperationException("Host already running");
   AppPath=Path.GetFullPath(appPath);DataPath=Path.GetFullPath(dataPath);ScreenSize=new(width,height);
   Directory.CreateDirectory(DataPath);
  }
  public static void Step(float delta) {Window.HostFrame(delta);Graphics.UnityCommands.ThrowFailure();Audio.UnityAudioCommands.ThrowFailure();}
  public static void MixAudio(float[] samples,int channels,int rate)=>Audio.UnityAudioCommands.Mix(samples,channels,rate);
  public static long AudioOutputFrames=>Audio.UnityAudioCommands.OutputFrames;
  public static long DrawCount => Graphics.UnityCommands.Draws;
  public static long UploadCount => Graphics.UnityCommands.Uploads;
  public static byte[] GetTextureData(int id,out int width,out int height,out uint format,out uint type)=>Graphics.UnityCommands.TextureBytes(id,out width,out height,out format,out type);
  public static void SetFocus(bool active)=>Window.HostFocus(active);
  public static void Resize(int width,int height)=>Window.HostResize(new(width,height));
  public static void Shutdown()=>Window.HostDispose();
 }
 public sealed class DrawPacket {
  public uint Primitive;
  public int[] Indices;
  public Dictionary<string,float[]> Attributes=new();
  public Graphics.Shader Shader;
  public Graphics.RenderTarget2D Target;
  public Graphics.Viewport Viewport;
  public Rectangle Scissor;
  public Graphics.RasterizerState Rasterizer;
  public Graphics.BlendState Blend;
  public Graphics.DepthStencilState Depth;
 }
}
