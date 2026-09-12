using Engine.Graphics;
using Silk.NET.OpenXR;
namespace Engine {
 // Preserve the shared subclass contract; this desktop host never creates an XR session.
 public abstract unsafe class OpenXrVrBackend:IVrBackend {
  protected abstract StructureType GraphicsBindingType {get;}
  protected abstract int GetGraphicsBindingSize();
  protected abstract void PopulateGraphicsBinding(void* bindingPtr);
  public bool IsAvailable {get;private set;}
  public bool IsStarted {get;private set;}
  public VrControllerType ControllerType {get;private set;}=VrControllerType.Unknown;
  public Matrix HmdMatrix=>default;
  public Matrix HmdMatrixInverted=>default;
  public Vector3 HmdMatrixYpr=>default;
  public Matrix HmdLastMatrix=>default;
  public Matrix HmdLastMatrixInverted=>default;
  public Vector3 HmdLastMatrixYpr=>default;
  public Vector2 HeadMove=>default;
  public int SwapchainWidth=>0;
  public int SwapchainHeight=>0;
  public RenderTarget2D VrRenderTarget=>null;
  public Vector2 WalkingVelocity=>Vector2.Zero;
  public void Initialize(){}
  public void StartVr()=>throw new PlatformNotSupportedException("VR is outside this Windows desktop port.");
  public bool BeginFrame()=>false;
  public EyeFrame GetEyeFrame(VrEye eye)=>throw new InvalidOperationException("No active VR frame.");
  public void ReleaseEye(VrEye eye){}
  public void EndFrame(){}
  public void EndFrameEmpty(){}
  public bool IsControllerPresent(VrController controller)=>false;
  public Matrix GetControllerMatrix(VrController controller)=>default;
  public Vector2 GetStickPosition(VrController controller,float deadZone=0f)=>Vector2.Zero;
  public Vector2? GetTouchpadPosition(VrController controller,float deadZone=0f)=>null;
  public float GetTriggerPosition(VrController controller,float deadZone=0f)=>0f;
  public bool IsButtonDown(VrController controller,VrControllerButton button)=>false;
  public bool IsButtonDownOnce(VrController controller,VrControllerButton button)=>false;
  public Matrix GetEyeToHeadTransform(VrEye eye)=>default;
  public Matrix GetProjectionMatrix(VrEye eye,float near,float far)=>default;
  public void Update(){}
  public void StopVr(){}
  public void Dispose(){}
 }
}
