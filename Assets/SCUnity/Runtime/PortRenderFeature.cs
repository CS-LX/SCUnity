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
 public sealed class PortRenderFeature:ScriptableRendererFeature {
  sealed class Pass:ScriptableRenderPass {
   sealed class Data{public TextureHandle Color,Depth;public PortRenderer Renderer;}
   public Pass(){renderPassEvent=RenderPassEvent.AfterRendering;}
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData){if(PortRenderer.Active==null)return;using(var builder=graph.AddUnsafePass<Data>("Survivalcraft ordered draws",out var data)){data.Color=frameData.Get<UniversalResourceData>().activeColorTexture;data.Depth=frameData.Get<UniversalResourceData>().activeDepthTexture;data.Renderer=PortRenderer.Active;builder.UseTexture(data.Color,AccessFlags.Write);builder.UseTexture(data.Depth,AccessFlags.ReadWrite);builder.AllowPassCulling(false);builder.SetRenderFunc((Data d,UnsafeGraphContext context)=>{var cmd=CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);d.Renderer.Render(cmd,d.Color,d.Depth);cmd.DisableScissorRect();});}}
  }
  Pass pass;public override void Create(){pass=new Pass();}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData){if(renderingData.cameraData.cameraType==CameraType.Game)renderer.EnqueuePass(pass);}
 }
}
