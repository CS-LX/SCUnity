using System.Runtime.InteropServices;
using System.Diagnostics;
namespace Engine.Audio {
 internal static partial class UnityAudioCommands {
  static readonly object gate=new();static uint next=1;internal static Exception Failure;
  sealed class Buffer {internal byte[] Data;internal int Channels,Rate;internal double Duration=>Data.Length/(double)(Channels*2*Rate);}
  sealed class Source {internal int State=0x1011;internal bool Loop;internal double Started,Position;internal uint Buffer;internal List<uint> Queue=new();internal Dictionary<int,object[]> Properties=new();}
  static readonly Dictionary<uint,Buffer> buffers=new();static readonly Dictionary<uint,Source> sources=new();
  static double Now=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
  internal static void RecordFailure(string name,Exception e){Failure??=new InvalidOperationException(name,e);}
  static int I(object o)=>Convert.ToInt32(o);static uint U(object o)=>Convert.ToUInt32(o);static IntPtr P(object o)=>(IntPtr)o;
  static void Write(object p,int n){Marshal.WriteInt32(P(p),n);}
  static double Duration(Source s)=>s.Buffer!=0?buffers[s.Buffer].Duration:s.Queue.Sum(id=>buffers[id].Duration);
  static double Position(Source s)=>s.Position+(s.State==0x1012?Now-s.Started:0);
  static int Processed(Source s){if(s.State==0x1014)return s.Queue.Count;double time=Position(s);int n=0;foreach(uint b in s.Queue){time-=buffers[b].Duration;if(time<0)break;n++;}return n;}
  static void Update(Source s){if(s.State==0x1012&&!s.Loop&&Position(s)>=Duration(s))s.State=0x1014;}
  internal static object Dispatch(string name,object[] a){lock(gate){
   switch(name){
    case "alGetError":return (Silk.NET.OpenAL.AudioError)0;
    case "alGenBuffers":case "alGenSources": for(int i=0;i<I(a[0]);i++){uint id=next++;Marshal.WriteInt32(P(a[1]),i*4,(int)id);if(name=="alGenSources")sources[id]=new();}return null;
    case "alDeleteBuffers":case "alDeleteSources": for(int i=0;i<I(a[0]);i++){uint id=(uint)Marshal.ReadInt32(P(a[1]),i*4);if(name=="alDeleteSources")sources.Remove(id);else buffers.Remove(id);}return null;
    case "alBufferData":{int fmt=I(a[1]);if(fmt!=0x1101&&fmt!=0x1103)throw new NotSupportedException("PCM format "+fmt);var b=new Buffer{Channels=fmt==0x1101?1:2,Rate=I(a[4]),Data=new byte[I(a[3])]};Marshal.Copy(P(a[2]),b.Data,0,b.Data.Length);buffers[U(a[0])]=b;return null;}
    case "alSourcePlay":{var s=sources[U(a[0])];if(s.State!=0x1013)s.Position=0;s.Started=Now;s.State=0x1012;return null;}
    case "alSourcePause":{var s=sources[U(a[0])];s.Position=Position(s);s.State=0x1013;return null;}
    case "alSourceStop":{var s=sources[U(a[0])];s.Position=Duration(s);s.State=0x1014;return null;}
    case "alSourceRewind":{var s=sources[U(a[0])];s.Position=0;s.State=0x1011;return null;}
    case "alGetSourcei":case "alGetSourceiv":{var s=sources[U(a[0])];Update(s);Write(a[2],I(a[1]) switch{0x1010=>s.State,0x1015=>s.Queue.Count,0x1016=>Processed(s),0x1009=>(int)s.Buffer,_=>throw new NotSupportedException("AL source property "+I(a[1]))});return null;}
    case "alSourceQueueBuffers":{var s=sources[U(a[0])];for(int i=0;i<I(a[1]);i++)s.Queue.Add((uint)Marshal.ReadInt32(P(a[2]),i*4));return null;}
    case "alSourceUnqueueBuffers":{var s=sources[U(a[0])];if(I(a[1])>Processed(s))throw new InvalidOperationException("Unprocessed audio buffer");for(int i=0;i<I(a[1]);i++){uint id=s.Queue[0];s.Queue.RemoveAt(0);s.Position-=buffers[id].Duration;Marshal.WriteInt32(P(a[2]),i*4,(int)id);}return null;}
    case "alSourcei":{var s=sources[U(a[0])];if(I(a[1])==0x1009){s.Buffer=U(a[2]);s.Queue.Clear();s.Position=0;}else if(I(a[1])==0x1007)s.Loop=I(a[2])!=0;else s.Properties[I(a[1])]=a;return null;}
    case "alSourcef":case "alSource3f":sources[U(a[0])].Properties[I(a[1])]=a;return null;
    case "alDistanceModel":case "alListenerf":return null;
    default:throw new NotSupportedException("Audio command backend: "+name);
   }
  }}
 }
}
