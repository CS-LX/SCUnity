Shader "SCUnity/Port/54a1c290af99bf21" {
 Properties { _Cull("Cull",Float)=0 _Src("Source",Float)=1 _Dst("Destination",Float)=0 _SrcA("Source alpha",Float)=1 _DstA("Destination alpha",Float)=0 _ZWrite("Depth write",Float)=0 _ZTest("Depth test",Float)=8 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline"} Pass {
 Cull [_Cull] Blend [_Src] [_Dst], [_SrcA] [_DstA] ZWrite [_ZWrite] ZTest [_ZTest]
 HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex SCVert
 #pragma fragment SCFrag
 #pragma multi_compile _ USE_ADDITIVECOLOR
#pragma multi_compile _ USE_ALPHATHRESHOLD
#pragma multi_compile _ USE_TEXTURE
#pragma multi_compile _ USE_VERTEXCOLOR
 #define HLSL
 #define MAX_INSTANCES_COUNT 32
 #if defined(SHADER_STAGE_VERTEX)
 #ifdef HLSL

float4x4 u_worldViewProjectionMatrix;
float4 u_color;

void SCVert(
	in float3 a_position: POSITION,
#ifdef USE_VERTEXCOLOR
	in float4 a_color: COLOR,
#endif
#ifdef USE_TEXTURE
	in float2 a_texcoord: TEXCOORD,
#endif
#ifdef USE_TEXTURE
	out float2 v_texcoord : TEXCOORD,
#endif
	out float4 v_color : COLOR,
	out float4 sv_position: SV_POSITION
)
{
	// Color
	v_color = u_color;
#ifdef USE_VERTEXCOLOR
	v_color *= a_color;
#endif

	// Texcoord
#ifdef USE_TEXTURE
	v_texcoord = a_texcoord;
#endif

	// Position
	sv_position = mul(float4(a_position.xyz, 1.0), u_worldViewProjectionMatrix);
}

#endif
#ifdef GLSL

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='COLOR' Attribute='a_color' />
// <Semantic Name='TEXCOORD' Attribute='a_texcoord' />

uniform mat4 u_worldViewProjectionMatrix;
uniform vec4 u_color;

attribute vec3 a_position;
#ifdef USE_VERTEXCOLOR
attribute vec4 a_color;
#endif
#ifdef USE_TEXTURE
attribute vec2 a_texcoord;
#endif
#ifdef USE_TEXTURE
varying vec2 v_texcoord;
#endif
varying vec4 v_color;

void SCVert()
{
	// Color
	v_color = u_color;
#ifdef USE_VERTEXCOLOR
	v_color *= a_color;
#endif

	// Texcoord
#ifdef USE_TEXTURE
	v_texcoord = a_texcoord;
#endif

	// Position
	gl_Position = u_worldViewProjectionMatrix * vec4(a_position.xyz, 1.0);

	// Fix gl_Position
	OPENGL_POSITION_FIX;
}

#endif

 #endif
 #if defined(SHADER_STAGE_FRAGMENT)
 #ifdef HLSL

#ifdef USE_TEXTURE
Texture2D u_texture;
SamplerState sampler_u_texture;
#endif
#ifdef USE_ADDITIVECOLOR
float4 u_additiveColor;
#endif
#ifdef USE_ALPHATHRESHOLD
float u_alphaThreshold;
#endif

void SCFrag(
#ifdef USE_TEXTURE
	in float2 v_texcoord: TEXCOORD,
#endif
	in float4 v_color : COLOR,
	out float4 svTarget: SV_TARGET
)
{
	// Color
	float4 result = v_color;

	// Texture
#ifdef USE_TEXTURE
	result *= u_texture.Sample(sampler_u_texture, v_texcoord);
#endif

#ifdef USE_ADDITIVECOLOR
	result += u_additiveColor;
#endif

	// Alpha threshold
#ifdef USE_ALPHATHRESHOLD
	if (result.a <= u_alphaThreshold)
		discard;
#endif

	// Return
	svTarget = result;
}

#endif
#ifdef GLSL

// <Sampler Name='sampler_u_texture' Texture='u_texture' />

#ifdef GL_ES
precision mediump float;
#endif

#ifdef USE_TEXTURE
uniform sampler2D u_texture;
#endif
#ifdef USE_ADDITIVECOLOR
uniform vec4 u_additiveColor;
#endif
#ifdef USE_ALPHATHRESHOLD
uniform float u_alphaThreshold;
#endif

#ifdef USE_TEXTURE
varying vec2 v_texcoord;
#endif
varying vec4 v_color;

void SCFrag()
{
	// Color
	vec4 result = v_color;

	// Texture
#ifdef USE_TEXTURE
	result *= texture2D(u_texture, v_texcoord);
#endif

#ifdef USE_ADDITIVECOLOR
	result += u_additiveColor;
#endif

	// Alpha threshold
#ifdef USE_ALPHATHRESHOLD
	if (result.a <= u_alphaThreshold)
		discard;
#endif

	// On some devices using gl_FragColor in calculations causes a compile fail (Kindle Fire 1)
	gl_FragColor = result;
}

#endif

 #endif
 ENDHLSL
 } } }
