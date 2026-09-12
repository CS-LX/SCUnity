using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using E=Engine.Graphics;
using H=Engine.UnityRuntime.Host;
namespace SCUnity.Runtime {
 public sealed class PortRenderer:IDisposable {
  public static PortRenderer Active;
  readonly List<Action<CommandBuffer,RenderTargetIdentifier>> commands=new List<Action<CommandBuffer,RenderTargetIdentifier>>();
  readonly List<UnityEngine.Object> transient=new List<UnityEngine.Object>();
  readonly Dictionary<int,Texture> textures=new Dictionary<int,Texture>();
  readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
  public long ExecutedDraws;
  public PortRenderer(){Active=this;H.Draw=Draw;H.Clear=Clear;}
  public void Begin(){commands.Clear();foreach(var item in transient)UnityEngine.Object.Destroy(item);transient.Clear();}
  public void Render(CommandBuffer cmd,RenderTargetIdentifier target){foreach(var action in commands)action(cmd,target);}
  static Vector4 V(float[] f,int i=0)=>new Vector4(f[i],f.Length>i+1?f[i+1]:0,f.Length>i+2?f[i+2]:0,f.Length>i+3?f[i+3]:0);
  static Color C(Engine.Color c)=>new Color(c.R/255f,c.G/255f,c.B/255f,c.A/255f);
  static string Hash(string s){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(s.Replace("\r\n","\n").TrimStart('\uFEFF')))).Replace("-","").ToLowerInvariant().Substring(0,16);}
  Texture Texture(E.Texture2D source,E.SamplerState sampler=null){
   if(source==null)return UnityEngine.Texture2D.whiteTexture;
   if(!textures.TryGetValue(source.m_texture,out var result)){
    if(source is E.RenderTarget2D rt){var target=new RenderTexture(rt.Width,rt.Height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);target.Create();result=target;}
    else{
     var bytes=H.GetTextureData(source.m_texture,out var w,out var h,out var fmt,out var type);
     TextureFormat format=fmt==0x1908&&type==0x1401?TextureFormat.RGBA32:fmt==0x1907&&type==0x1401?TextureFormat.RGB24:fmt==0x1908&&type==0x140B?TextureFormat.RGBAHalf:fmt==0x1908&&type==0x1406?TextureFormat.RGBAFloat:throw new NotSupportedException("Unity texture format "+fmt+"/"+type);
     var tex=new UnityEngine.Texture2D(w,h,format,source.MipLevelsCount>1,true);tex.SetPixelData(bytes,0);tex.Apply(source.MipLevelsCount>1,false);result=tex;
    }textures.Add(source.m_texture,result);
   }
   if(sampler!=null){result.filterMode=sampler.FilterMode==E.TextureFilterMode.Point?FilterMode.Point:sampler.FilterMode==E.TextureFilterMode.Linear?FilterMode.Bilinear:FilterMode.Trilinear;result.wrapModeU=sampler.AddressModeU==E.TextureAddressMode.Clamp?TextureWrapMode.Clamp:TextureWrapMode.Repeat;result.wrapModeV=sampler.AddressModeV==E.TextureAddressMode.Clamp?TextureWrapMode.Clamp:TextureWrapMode.Repeat;}
   return result;
  }
  static Matrix4x4 Matrix(float[] values,int offset){
   // Engine stores row-vector matrices; HLSL mul(row, matrix) needs the same numeric matrix.
   var m=new Matrix4x4();for(int row=0;row<4;row++)for(int col=0;col<4;col++)m[row,col]=values[offset+row*4+col];return m;
  }
  Material Material(Engine.UnityRuntime.DrawPacket p){
   string shaderName="SCUnity/Port/"+Hash(p.Shader.m_vertexShaderCode+"\n"+p.Shader.m_pixelShaderCode);
   string key=shaderName+"|"+string.Join(";",p.Shader.m_shaderMacros.Select(x=>x.Name+"="+x.Value))+"|"+(int)p.Rasterizer.CullMode+"|"+(int)p.Blend.ColorSourceBlend+"|"+(int)p.Blend.ColorDestinationBlend+"|"+(int)p.Blend.AlphaSourceBlend+"|"+(int)p.Blend.AlphaDestinationBlend+"|"+p.Depth.DepthBufferTestEnable+"|"+p.Depth.DepthBufferWriteEnable+"|"+(int)p.Depth.DepthBufferFunction;
   if(materials.TryGetValue(key,out var result))return result;
   var shader=UnityEngine.Shader.Find(shaderName);if(shader==null||!shader.isSupported)throw new InvalidOperationException("Missing Unity shader "+shaderName+" type="+p.Shader.GetType());
   result=new Material(shader);foreach(var macro in p.Shader.m_shaderMacros)if(macro.Name!="MAX_INSTANCES_COUNT")result.EnableKeyword(macro.Name);
   result.SetInt("_Cull",p.Rasterizer.CullMode==E.CullMode.None?0:p.Rasterizer.CullMode==E.CullMode.CullClockwise?1:2);
   result.SetInt("_Src",Blend(p.Blend.ColorSourceBlend));result.SetInt("_Dst",Blend(p.Blend.ColorDestinationBlend));result.SetInt("_SrcA",Blend(p.Blend.AlphaSourceBlend));result.SetInt("_DstA",Blend(p.Blend.AlphaDestinationBlend));
   result.SetInt("_ZWrite",p.Depth.DepthBufferWriteEnable?1:0);result.SetInt("_ZTest",p.Depth.DepthBufferTestEnable?new[]{8,1,2,4,3,7,5,6}[(int)p.Depth.DepthBufferFunction]:8);
   materials.Add(key,result);return result;
  }
  static int Blend(E.Blend value){switch(value){case E.Blend.Zero:return 0;case E.Blend.One:return 1;case E.Blend.SourceColor:return 3;case E.Blend.InverseSourceColor:return 7;case E.Blend.DestinationColor:return 2;case E.Blend.InverseDestinationColor:return 4;case E.Blend.SourceAlpha:return 5;case E.Blend.InverseSourceAlpha:return 10;case E.Blend.DestinationAlpha:return 6;case E.Blend.InverseDestinationAlpha:return 8;case E.Blend.SourceAlphaSaturation:return 9;default:throw new NotSupportedException("Constant blend factor");}}
  void Clear(Engine.Color? color,float? depth,int? stencil){var source=E.Display.RenderTarget;var target=source==null?null:(RenderTexture)Texture(source);commands.Add((cmd,backbuffer)=>{cmd.SetRenderTarget(target==null?backbuffer:new RenderTargetIdentifier(target));cmd.DisableScissorRect();cmd.ClearRenderTarget(depth.HasValue,color.HasValue,color.HasValue?C(color.Value):Color.clear,depth??1f);});}
  void Draw(Engine.UnityRuntime.DrawPacket p){
   var mesh=new Mesh{indexFormat=IndexFormat.UInt32};transient.Add(mesh);
   foreach(var pair in p.Attributes){var f=pair.Value;int count=f.Length/4;if(pair.Key=="POSITION"){var v=new Vector3[count];for(int i=0;i<count;i++)v[i]=V(f,i*4);mesh.vertices=v;}
    else if(pair.Key=="COLOR"){var v=new Color[count];for(int i=0;i<count;i++){var c=V(f,i*4);v[i]=new Color(c.x,c.y,c.z,c.w);}mesh.colors=v;}
    else if(pair.Key=="NORMAL"){var v=new Vector3[count];for(int i=0;i<count;i++)v[i]=V(f,i*4);mesh.normals=v;}
    else if(pair.Key.StartsWith("TEXCOORD")){int channel=pair.Key.Length==8?0:int.Parse(pair.Key.Substring(8));var v=new List<Vector4>(count);for(int i=0;i<count;i++)v.Add(V(f,i*4));mesh.SetUVs(channel,v);}
    else throw new NotSupportedException("Vertex semantic "+pair.Key);
   }
   mesh.SetIndices(p.Indices,p.Primitive==4?MeshTopology.Triangles:p.Primitive==1?MeshTopology.Lines:p.Primitive==0?MeshTopology.Points:throw new NotSupportedException("Primitive "+p.Primitive),0,false);
   var block=new MaterialPropertyBlock();
   foreach(var parameter in p.Shader.m_parameters){
    if(parameter.Type==E.ShaderParameterType.Texture2D){var sampler=p.Shader.m_parameters.FirstOrDefault(x=>x.Type==E.ShaderParameterType.Sampler2D);block.SetTexture(parameter.Name,Texture((E.Texture2D)parameter.Resource,(E.SamplerState)sampler?.Resource));}
    else if(parameter.Type==E.ShaderParameterType.Float){if(parameter.Count==1)block.SetFloat(parameter.Name,parameter.Value[0]);else block.SetFloatArray(parameter.Name,parameter.Value);}
    else if(parameter.Type==E.ShaderParameterType.Vector2||parameter.Type==E.ShaderParameterType.Vector3||parameter.Type==E.ShaderParameterType.Vector4){int components=parameter.Value.Length/parameter.Count;if(parameter.Count==1)block.SetVector(parameter.Name,V(parameter.Value));else{var values=new Vector4[parameter.Count];for(int i=0;i<values.Length;i++)values[i]=V(parameter.Value,i*components);block.SetVectorArray(parameter.Name,values);}}
    else if(parameter.Type==E.ShaderParameterType.Matrix){if(parameter.Count==1)block.SetMatrix(parameter.Name,Matrix(parameter.Value,0));else{var values=new Matrix4x4[parameter.Count];for(int i=0;i<values.Length;i++)values[i]=Matrix(parameter.Value,i*16);block.SetMatrixArray(parameter.Name,values);}}
    else if(parameter.Type==E.ShaderParameterType.Int)block.SetInt(parameter.Name,parameter.IntValue[0]);
    else if(parameter.Type!=E.ShaderParameterType.Sampler2D)throw new NotSupportedException("Shader parameter "+parameter.Type);
   }
   var material=Material(p);var target=p.Target==null?null:(RenderTexture)Texture(p.Target);
   int height=target==null?Engine.Window.Size.Y:target.height;
   var viewport=new Rect(p.Viewport.X,height-p.Viewport.Y-p.Viewport.Height,p.Viewport.Width,p.Viewport.Height);
   var scissor=new Rect(p.Scissor.Left,height-p.Scissor.Bottom,p.Scissor.Width,p.Scissor.Height);bool scissorOn=p.Rasterizer.ScissorTestEnable;
   commands.Add((cmd,backbuffer)=>{cmd.SetRenderTarget(target==null?backbuffer:new RenderTargetIdentifier(target));cmd.SetViewport(viewport);if(scissorOn)cmd.EnableScissorRect(scissor);else cmd.DisableScissorRect();cmd.DrawMesh(mesh,Matrix4x4.identity,material,0,0,block);ExecutedDraws++;});
  }
  public void Dispose(){H.Draw=null;H.Clear=null;Active=null;foreach(var item in transient)UnityEngine.Object.Destroy(item);foreach(var item in textures.Values)UnityEngine.Object.Destroy(item);foreach(var item in materials.Values)UnityEngine.Object.Destroy(item);}
 }
}
