// Diagnostic command backend. Shader reflection only; no GPU compilation is claimed here.
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
namespace Engine.Graphics {
 internal static partial class UnityCommands {
  internal static Exception Failure;
  internal static long Draws, Uploads;
  static uint next = 1, currentProgram;
  static readonly Dictionary<string,object[]> states = new();
  static readonly Dictionary<uint,uint> boundBuffers = new();
  static readonly Dictionary<uint,byte[]> buffers = new();
  static readonly Dictionary<uint,string> shaderSources = new();
  static readonly Dictionary<uint,uint> shaderTypes = new();
  static readonly Dictionary<uint,ProgramInfo> programs = new();
  static readonly HashSet<uint> enabled = new();
  static readonly Dictionary<uint,IntPtr> strings = new();
  static readonly Dictionary<(int,uint),uint> boundTextures = new();
  static int textureUnit;
  sealed class VertexSource {internal int Size,Type,Stride;internal bool Normalized;internal IntPtr Pointer;internal uint Buffer;}
  static readonly Dictionary<int,VertexSource> vertices=new();
  static readonly HashSet<int> vertexEnabled=new();
  internal static readonly Dictionary<uint,TextureInfo> Textures = new();
  internal sealed class TextureInfo { internal int Width,Height,Depth; internal uint Format,Type; internal byte[] Bytes; }
  internal sealed class Variable { internal string Name; internal uint Type; internal int Size = 1; }
  internal sealed class ProgramInfo { internal List<uint> Shaders = new(); internal List<Variable> Uniforms = new(), Attributes = new(); internal Dictionary<int,object[]> Values = new(); }
  internal static void RecordFailure(string name, Exception e) { Failure ??= new InvalidOperationException(name + ": " + e.Message,e); }
  internal static void ThrowFailure() { if(Failure != null) throw Failure; }
  static uint U(object o)=>Convert.ToUInt32(o);
  static int I(object o)=> o is IntPtr p ? checked((int)p.ToInt64()) : o is UIntPtr up ? checked((int)up.ToUInt64()) : checked(Convert.ToInt32(o));
  static IntPtr P(object o)=>(IntPtr)o;
  static void Write(object p,int n) { if(P(p)!=IntPtr.Zero) Marshal.WriteInt32(P(p),n); }
  static byte[] Copy(IntPtr p,int n) { var b=new byte[n]; if(p!=IntPtr.Zero&&n>0)Marshal.Copy(p,b,0,n);return b; }
  static string ReadString(object p)=>Marshal.PtrToStringAnsi(P(p));
  static void WriteString(string s, object size, object length, object output) { var b=Encoding.ASCII.GetBytes(s); int n=Math.Min(b.Length,Math.Max(0,I(size)-1));if(n>0)Marshal.Copy(b,0,P(output),n);if(I(size)>0)Marshal.WriteByte(P(output),n,0); Write(length,n); }
  static int PixelSize(uint f,uint t) { int c=f switch {0x1908=>4,0x1907=>3,0x1903=>1,0x8227=>2,_=>throw new NotSupportedException("Pixel format "+f)};return t switch {0x1401=>c,0x1406=>c*4,0x140B=>c*2,0x8363 or 0x8033 or 0x8034=>2,_=>throw new NotSupportedException("Pixel type "+t)}; }
  internal static object Dispatch(string name, object[] a) {
   switch(name) {
    case "glGetError": return 0;
    case "glGetString": {
     uint key=U(a[0]); if(!strings.TryGetValue(key,out var p)) { string text=key switch {0x1F00=>"SCUnity",0x1F01=>"Managed command validation (no GPU)",0x1F02=>"OpenGL ES 3.0 command front-end",0x1F03=>"GL_OES_packed_depth_stencil",_=>""};strings.Add(key,p=Marshal.StringToHGlobalAnsi(text)); } return p;
    }
    case "glGetIntegerv": Write(a[1],U(a[0]) switch {0x0D33=>8192,0x8DFB=>1024,0x8B4D=>32,0x0D52 or 0x0D53 or 0x0D54 or 0x0D55=>8,0x0D56=>24,0x0D57=>8,0x87FE or 0x8B9A=>0,_=>0});return null;
    case "glGetFloatv": Marshal.Copy(new[]{1f},0,P(a[1]),1);return null;
    case "glGenBuffers": case "glGenTextures": case "glGenFramebuffers": case "glGenRenderbuffers":
     for(int i=0;i<I(a[0]);i++)Marshal.WriteInt32(P(a[1]),i*4,(int)next++);return null;
    case "glCreateShader": {uint id=next++;shaderTypes[id]=U(a[0]);return id;}
    case "glCreateProgram": {uint id=next++;programs[id]=new();return id;}
    case "glShaderSource": { var s=new StringBuilder();for(int i=0;i<I(a[1]);i++){var ptr=Marshal.ReadIntPtr(P(a[2]),i*IntPtr.Size);int len=P(a[3])==IntPtr.Zero?-1:Marshal.ReadInt32(P(a[3]),i*4);s.Append(len<0?Marshal.PtrToStringAnsi(ptr):Encoding.UTF8.GetString(Copy(ptr,len)));} shaderSources[U(a[0])]=s.ToString();return null; }
    case "glCompileShader": Preprocess(shaderSources[U(a[0])]);return null;
    case "glGetShaderiv": Write(a[2],U(a[1])==0x8B81?1:0);return null;
    case "glGetShaderInfoLog": case "glGetProgramInfoLog": WriteString("",a[1],a[2],a[3]);return null;
    case "glAttachShader": programs[U(a[0])].Shaders.Add(U(a[1]));return null;
    case "glLinkProgram": Reflect(programs[U(a[0])]);return null;
    case "glGetProgramiv": {var pr=programs[U(a[0])];Write(a[2],U(a[1]) switch {0x8B82=>1,0x8B89=>pr.Attributes.Count,0x8B86=>pr.Uniforms.Count,0x8B87 or 0x8B8A=>256,_=>0});return null;}
    case "glGetActiveAttrib": case "glGetActiveUniform": {var pr=programs[U(a[0])];var v=(name=="glGetActiveAttrib"?pr.Attributes:pr.Uniforms)[I(a[1])];Write(a[4],v.Size);Write(a[5],(int)v.Type);WriteString(v.Name+(v.Size>1?"[0]":""),a[2],a[3],a[6]);return null;}
    case "glGetAttribLocation": case "glGetUniformLocation": {var pr=programs[U(a[0])];var n=ReadString(a[1]).Split('[')[0];return (name=="glGetAttribLocation"?pr.Attributes:pr.Uniforms).FindIndex(v=>v.Name==n);}
    case "glUseProgram": currentProgram=U(a[0]);return null;
    case "glDeleteProgram": programs.Remove(U(a[0]));return null;
    case "glDeleteShader": shaderSources.Remove(U(a[0]));shaderTypes.Remove(U(a[0]));return null;
    case "glDetachShader": programs[U(a[0])].Shaders.Remove(U(a[1]));return null;
    case "glGetUniformBlockIndex": return uint.MaxValue;
    case "glEnable": enabled.Add(U(a[0]));return null;
    case "glDisable": enabled.Remove(U(a[0]));return null;
    case "glIsEnabled": return (byte)(enabled.Contains(U(a[0]))?1:0);
    case "glBindBuffer": boundBuffers[U(a[0])]=U(a[1]);return null;
    case "glBufferData": buffers[boundBuffers[U(a[0])]]=Copy(P(a[2]),I(a[1]));Uploads++;return null;
    case "glBufferSubData": {var data=buffers[boundBuffers[U(a[0])]];Marshal.Copy(P(a[3]),data,I(a[1]),I(a[2]));Uploads++;return null;}
    case "glDeleteBuffers": for(int i=0;i<I(a[0]);i++)buffers.Remove((uint)Marshal.ReadInt32(P(a[1]),i*4));return null;
    case "glActiveTexture": textureUnit=I(a[0])-0x84C0;return null;
    case "glBindTexture": boundTextures[(textureUnit,U(a[0]))]=U(a[1]);return null;
    case "glTexImage2D": case "glTexImage3D": {
     bool d=name=="glTexImage3D";int w=I(a[3]),h=I(a[4]),depth=d?I(a[5]):1;uint f=U(a[d?7:6]),t=U(a[d?8:7]);
     if(I(a[1])==0)Textures[boundTextures[(textureUnit,U(a[0]))]]=new(){Width=w,Height=h,Depth=depth,Format=f,Type=t,Bytes=Copy(P(a[d?9:8]),checked(w*h*depth*PixelSize(f,t)))};Uploads++;return null;
    }
    case "glTexSubImage2D": {
     if(I(a[1])!=0)return null;var tex=Textures[boundTextures[(textureUnit,U(a[0]))]];int x=I(a[2]),y=I(a[3]),w=I(a[4]),h=I(a[5]),ps=PixelSize(U(a[6]),U(a[7]));
     if(tex.Format!=U(a[6])||tex.Type!=U(a[7]))throw new NotSupportedException("Pixel conversion");
     for(int row=0;row<h;row++)Marshal.Copy(IntPtr.Add(P(a[8]),row*w*ps),tex.Bytes,((row+y)*tex.Width+x)*ps,w*ps);Uploads++;return null;
    }
    case "glDeleteTextures": for(int i=0;i<I(a[0]);i++)Textures.Remove((uint)Marshal.ReadInt32(P(a[1]),i*4));return null;
    case "glCheckFramebufferStatus": return 0x8CD5;
    case "glVertexAttribPointer": vertices[I(a[0])]=new(){Size=I(a[1]),Type=I(a[2]),Normalized=I(a[3])!=0,Stride=I(a[4]),Pointer=P(a[5]),Buffer=boundBuffers.TryGetValue(0x8892,out var vb)?vb:0};return null;
    case "glEnableVertexAttribArray":vertexEnabled.Add(I(a[0]));return null;
    case "glDisableVertexAttribArray":vertexEnabled.Remove(I(a[0]));return null;
    case "glDrawArrays": case "glDrawElements": if(currentProgram==0)throw new InvalidOperationException("Draw without program");EmitDraw(name,a);Draws++;return null;
    case "glClear": {
     int mask=I(a[0]);Color? color=null;float? depth=null;int? stencil=null;
     if((mask&0x4000)!=0){var c=states["glClearColor"];color=new Color(Convert.ToSingle(c[0]),Convert.ToSingle(c[1]),Convert.ToSingle(c[2]),Convert.ToSingle(c[3]));}
     if((mask&0x100)!=0)depth=Convert.ToSingle(states["glClearDepthf"][0]);
     if((mask&0x400)!=0)stencil=I(states["glClearStencil"][0]);
     UnityRuntime.Host.Clear?.Invoke(color,depth,stencil);return null;
    }
   }
   if(name.StartsWith("glUniform")) {
    object[] value=(object[])a.Clone();
    if(value.Last() is IntPtr p){int components=name.StartsWith("glUniformMatrix")?(name.Contains("3")?9:16):(int)char.GetNumericValue(name[9]);int count=I(a[1])*components;value[value.Length-1]=Copy(p,count*4);}
    programs[currentProgram].Values[I(a[0])]=value;return null;
   }
   switch(name) {
    case "glClearColor": case "glClearDepthf": case "glClearStencil": case "glColorMask": case "glDepthFunc": case "glDepthMask": case "glDepthRangef": case "glCullFace": case "glFrontFace": case "glBlendColor": case "glBlendEquation": case "glBlendEquationSeparate": case "glBlendFunc": case "glBlendFuncSeparate": case "glPolygonOffset": case "glLineWidth": case "glViewport": case "glScissor": case "glTexParameteri": case "glTexParameterf": case "glGenerateMipmap": case "glBindFramebuffer": case "glBindRenderbuffer": case "glRenderbufferStorage": case "glFramebufferRenderbuffer": case "glFramebufferTexture2D": case "glDeleteFramebuffers": case "glDeleteRenderbuffers": case "glPixelStorei": states[name]=a;return null;
    default: throw new NotSupportedException("Command backend: "+name);
   }
  }
  internal static void EmitDraw(string name,object[] a){
   if(UnityRuntime.Host.Draw==null)return;
   var p=new UnityRuntime.DrawPacket{Primitive=U(a[0]),Shader=GLWrapper.m_lastShader,Target=Display.RenderTarget,Viewport=Display.Viewport,Scissor=Display.ScissorRectangle,Rasterizer=Display.RasterizerState,Blend=Display.BlendState,Depth=Display.DepthStencilState};
   int count=I(a[name=="glDrawArrays"?2:1]);p.Indices=new int[count];int min=int.MaxValue,max=0;
   for(int i=0;i<count;i++){int index;if(name=="glDrawArrays")index=I(a[1])+i;else{
    int bytes=I(a[2])==0x1405?4:I(a[2])==0x1403?2:1;var ptr=IntPtr.Add(P(a[3]),i*bytes);uint buf=boundBuffers.TryGetValue(0x8893,out var bi)?bi:0;
    var data=buf==0?Copy(ptr,bytes):buffers[buf];int offset=buf==0?0:ptr.ToInt32();index=bytes==4?BitConverter.ToInt32(data,offset):bytes==2?BitConverter.ToUInt16(data,offset):data[offset];
   }p.Indices[i]=index;min=Math.Min(min,index);max=Math.Max(max,index);}
   if(count==0)return;for(int i=0;i<count;i++)p.Indices[i]-=min;
   foreach(var attr in p.Shader.m_shaderAttributeData){if(!vertexEnabled.Contains(attr.Location))continue;var v=vertices[attr.Location];int unit=v.Type==0x1406||v.Type==0x1404||v.Type==0x1405?4:v.Type==0x1402||v.Type==0x1403?2:1;int stride=v.Stride==0?v.Size*unit:v.Stride;int total=max-min+1;
    int offset=v.Buffer==0?0:checked((int)v.Pointer.ToInt64())+min*stride;
    var data=v.Buffer==0?Copy(IntPtr.Add(v.Pointer,min*stride),(total-1)*stride+v.Size*unit):buffers[v.Buffer];var values=new float[total*4];
    for(int vertex=0;vertex<total;vertex++){values[vertex*4+3]=1;for(int k=0;k<v.Size;k++){int pos=offset+vertex*stride+k*unit;float value=v.Type switch{0x1406=>BitConverter.ToSingle(data,pos),0x1401=>data[pos],0x1400=>(sbyte)data[pos],0x1403=>BitConverter.ToUInt16(data,pos),0x1402=>BitConverter.ToInt16(data,pos),0x1404=>BitConverter.ToInt32(data,pos),_=>throw new NotSupportedException("Vertex type "+v.Type)};if(v.Normalized)value=v.Type switch{0x1401=>value/255f,0x1400=>Math.Max(value/127f,-1),0x1403=>value/65535f,0x1402=>Math.Max(value/32767f,-1),_=>value};values[vertex*4+k]=value;}}
    p.Attributes[attr.Semantic]=values;
   }
   UnityRuntime.Host.Draw(p);
  }
  public static byte[] TextureBytes(int id,out int width,out int height,out uint format,out uint type){var t=Textures[(uint)id];width=t.Width;height=t.Height;format=t.Format;type=t.Type;return t.Bytes;}
  static void Reflect(ProgramInfo pr) {
   pr.Attributes.Clear();pr.Uniforms.Clear();
   foreach(uint shader in pr.Shaders){var s=Preprocess(shaderSources[shader]);foreach(Match m in Regex.Matches(s,@"\b(uniform|attribute|in)\s+(?:(?:lowp|mediump|highp)\s+)?(\w+)\s+(\w+)\s*(?:\[\s*(\d+)\s*\])?\s*;")){
    bool uniform=m.Groups[1].Value=="uniform";if(!uniform&&shaderTypes[shader]!=0x8B31)continue;
    var list=uniform?pr.Uniforms:pr.Attributes;string n=m.Groups[3].Value;if(list.Any(v=>v.Name==n))continue;
    // GLSL removes declarations which are never used. This is a reflection probe, not a shader compiler.
    if(Regex.Matches(s,@"\b"+Regex.Escape(n)+@"\b").Count<2)continue;
    uint type=m.Groups[2].Value switch {"float"=>0x1406,"vec2"=>0x8B50,"vec3"=>0x8B51,"vec4"=>0x8B52,"int"=>0x1404,"ivec2"=>0x8B53,"ivec3"=>0x8B54,"ivec4"=>0x8B55,"bool"=>0x8B56,"mat3"=>0x8B5B,"mat4"=>0x8B5C,"sampler2D"=>0x8B5E,"sampler2DArray"=>0x8DC1,_=>throw new NotSupportedException("GLSL type "+m.Groups[2].Value)};
    list.Add(new(){Name=n,Type=type,Size=m.Groups[4].Success?int.Parse(m.Groups[4].Value):1});
   }}
  }
  internal static string Preprocess(string input) {
   var defs=new Dictionary<string,string>{{"GL_ES","1"}};var stack=new Stack<(bool parent,bool taken)>();bool active=true;var output=new StringBuilder();
   foreach(string line in Regex.Replace(input,@"/\*.*?\*/","",RegexOptions.Singleline).Split('\n')){
    var m=Regex.Match(line,@"^\s*#\s*(\w+)\s*(.*)$");if(!m.Success){if(active)output.AppendLine(line.Split(new[]{"//"},StringSplitOptions.None)[0]);continue;}
    string op=m.Groups[1].Value,arg=m.Groups[2].Value.Trim();
    switch(op){
     case "ifdef":case "ifndef":case "if":{bool cond=op=="ifdef"?defs.ContainsKey(arg):op=="ifndef"?!defs.ContainsKey(arg):Expression(arg,defs);stack.Push((active,cond));active=active&&cond;break;}
     case "else":{var f=stack.Pop();active=f.parent&&!f.taken;stack.Push((f.parent,true));break;}
     case "elif":{var f=stack.Pop();bool cond=!f.taken&&Expression(arg,defs);active=f.parent&&cond;stack.Push((f.parent,f.taken||cond));break;}
     case "endif":active=stack.Pop().parent;break;
     case "define": if(active){var split=Regex.Match(arg,@"^(\w+)\s*(.*)$");defs[split.Groups[1].Value]=split.Groups[2].Value;}break;
     case "undef":if(active)defs.Remove(arg);break;
     case "version":case "line":case "extension":case "pragma":break;
     default:if(active)throw new NotSupportedException("Shader directive "+op);break;
    }
   }
   if(stack.Count!=0)throw new InvalidOperationException("Unbalanced shader conditional");
   string result=output.ToString();for(int i=0;i<5;i++) {string old=result;result=Regex.Replace(result,@"\b\w+\b",m=>defs.TryGetValue(m.Value,out var value)?value:m.Value);if(result==old)break;}return result;
  }
  static bool Expression(string value,Dictionary<string,string> defs){value=Regex.Replace(value,@"defined\s*\(?\s*(\w+)\s*\)?",m=>defs.ContainsKey(m.Groups[1].Value)?"1":"0");value=Regex.Replace(value,@"\b[A-Za-z_]\w*\b",m=>defs.TryGetValue(m.Value,out var n)?(n==""?"1":n):"0");if(int.TryParse(value,out var num))return num!=0;throw new NotSupportedException("Shader conditional "+value);}
 }
}
