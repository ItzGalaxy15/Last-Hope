// Radial sweep technique based on: https://bgolus.medium.com/progressing-in-circles-13452434fdb9

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

// 0 = ready, 1 = fully on cooldown
float CooldownPercent = 0.0f;

static const float TWO_PI = 6.28318530718f;

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;

	if (color.a > 0 && CooldownPercent > 0.005f)
	{
		// Angle from center: 0 = 12 o'clock, sweeps clockwise
		float2 uv = input.TextureCoordinates - float2(0.5f, 0.5f);
		float angle = atan2(uv.x, -uv.y);
		if (angle < 0.0f) angle += TWO_PI;

		// Pixels inside the remaining-cooldown arc get darkened
		if (angle < CooldownPercent * TWO_PI)
		{
			color.rgb *= 0.22f;
		}
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
