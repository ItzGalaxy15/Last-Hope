#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float FadeAmount = 0.0f;

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;

	// Only apply the fade to visible pixels so the transparent pixels are skipped.
	if (color.a > 0)
	{
		float2 uv = input.TextureCoordinates;
		float dist = distance(uv, float2(0.5, 0.5));
		float vignette = smoothstep(0.95, 0.25, dist);
		float shine = smoothstep(0.55, 0.0, dist); // Shine area covers the center of the sprite

		float3 darkColor = float3(0.08, 0.08, 0.08);
		float3 warmTint = float3(0.34, 0.28, 0.12);
		float3 targetColor = lerp(darkColor, warmTint, 0.35) * color.a;

		color.rgb = lerp(color.rgb, targetColor, FadeAmount);
		color.rgb += shine * FadeAmount * float3(0.25, 0.20, 0.10); // strength of the glow
		color.rgb *= lerp(0.75, 1.0, vignette); // Darken the edges with a vignette effect
	}

	return color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
