#ifndef DXSPRITEBATCH_H_
#define DXSPRITEBATCH_H_

#include <d3d11.h>
#include <queue>
#include <DirectXMath.h>
#include <collection.h>
#include "Color.h"
#include "Vector2.h"
#include "Rectangle.h"

using namespace DirectX;
using namespace std;
using namespace Microsoft::WRL;

namespace EmulatorComponent
{
	struct SpriteInfo
	{
		XMFLOAT4 TargetArea;
		XMFLOAT4 SourceArea;
		Color Color;
		float Rotation;
		float Depth;
		ComPtr<ID3D11ShaderResourceView> TextureSRV;
		ComPtr<ID3D11Texture2D> Texture;
		/*ResourceID TextureID;
		ContentType ContentType;*/
	};

	struct SpriteVertex
	{
		XMFLOAT3 Position;
		Color Color;
		XMFLOAT2 TexCoord;

	public:
		SpriteVertex() { }

		SpriteVertex(float x, float y, float z, float tx, float ty)
			: Position(XMFLOAT3(x,y,z)), TexCoord(XMFLOAT2(tx, ty))
		{ }
	};

	// filters which require feature level 9_3 come last
	enum SamplingMode
	{
		Nearest = 0,
		Linear,
		HQ2x,
		HQ3x,
		HQ4x,
		xBR2,
		xBR3,
		xBR4,
		xBR5
	};

	class DXSpriteBatch
	{
	private:
		ComPtr<ID3D11Device1> device;
		ComPtr<ID3D11DeviceContext1> context;
		ComPtr<ID3D11PixelShader> ps;
		ComPtr<ID3D11PixelShader> customPS;
		ComPtr<ID3D11VertexShader> vs;
		ComPtr<ID3D11InputLayout> inputLayout;
		ComPtr<ID3D11Buffer> onFrameBuffer;
		ComPtr<ID3D11Buffer> onResizeBuffer;
		ComPtr<ID3D11Buffer> textureInfoBuffer;
		ComPtr<ID3D11Buffer> vertexBuffer;
		ComPtr<ID3D11Buffer> indexBuffer;

		ComPtr<ID3D11BlendState> alphaBlendState;
		ComPtr<ID3D11BlendState> additiveBlendState;
		ComPtr<ID3D11DepthStencilState> depthStencilState;
		ComPtr<ID3D11RasterizerState> rasterizerState;
		ComPtr<ID3D11SamplerState> samplerStates[9];

		D3D_FEATURE_LEVEL supportedfeatureLevel;
		SamplingMode samplingMode;

		// hqx
		bool filtersLoaded;
		bool loadingFiltersFailed;
		ComPtr<ID3D11Resource> hq2xLookup;
		ComPtr<ID3D11ShaderResourceView> hq2xLookupSRV;
		ComPtr<ID3D11PixelShader> hq2xPS;
		ComPtr<ID3D11VertexShader> hq2xVS;

		ComPtr<ID3D11Resource> hq3xLookup;
		ComPtr<ID3D11ShaderResourceView> hq3xLookupSRV;
		ComPtr<ID3D11PixelShader> hq3xPS;
		ComPtr<ID3D11VertexShader> hq3xVS;

		ComPtr<ID3D11Resource> hq4xLookup;
		ComPtr<ID3D11ShaderResourceView> hq4xLookupSRV;
		ComPtr<ID3D11PixelShader> hq4xPS;
		ComPtr<ID3D11VertexShader> hq4xVS;

		ComPtr<ID3D11PixelShader> xbr2PS;
		ComPtr<ID3D11VertexShader> xbr2VS;

		ComPtr<ID3D11PixelShader> xbr3PS;
		ComPtr<ID3D11VertexShader> xbr3VS;

		ComPtr<ID3D11PixelShader> xbr4PS;
		ComPtr<ID3D11VertexShader> xbr4VS;

		ComPtr<ID3D11PixelShader> xbr5PS;
		ComPtr<ID3D11VertexShader> xbr5VS;

		bool AllowsFilters();
		void LoadFilters();
		void ResetFilters(bool failed);

		bool beginCalled;
		UINT batchedSprites;
		vector<SpriteInfo> queuedSprites;
		bool (*heapSort)(const SpriteInfo &, const SpriteInfo &);

		void LoadShaders(void);
		void UpdateProjectionMatrix(float width, float height);
		void InitializeBuffers(void);
		void CreateStates(void);
		void FlushBatch(void);
		void RenderBatch(UINT start, UINT end, SpriteInfo &spriteInfo);
		void QueueSprite(SpriteInfo &info);
	public:
		DXSpriteBatch(ID3D11Device1 *device, ID3D11DeviceContext1 *context, D3D_FEATURE_LEVEL supportedfeatureLevel, float width, float height);
		~DXSpriteBatch(void);

		void OnResize(float width, float height);
		void SetCustomPixelShader(void *customPS);

		void SetTextureSampling(SamplingMode mode);
		SamplingMode GetTextureSampling();

		void SetGameTextureSize(float width, float height);

		void Begin(XMMATRIX &world);
		void Draw(const Rectangle &targetArea, const Rectangle *sourceArea, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, float depth, float rotation, Color &color);
		void Draw(const Rectangle &target, const Rectangle *source, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, float depth, Color &color);
		void Draw(const Rectangle &target, const Rectangle *source, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, Color &color);
		void Draw(const Rectangle &target, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, float depth, float rotation, Color &color);
		void Draw(const Rectangle &target, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, float depth, Color &color);
		void Draw(const Rectangle &target, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, Color &color);
		void Draw(const Vector2 &target, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, float depth, float rotation, Color &color);
		void Draw(const Vector2 &target, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, float depth, Color &color);
		void Draw(const Vector2 &target, ID3D11ShaderResourceView *textureSRV, ID3D11Texture2D *texture, Color &color);
		void End(void);
	};

	bool HeapCompareByTexture(const SpriteInfo &info1, const SpriteInfo &info2);
	bool HeapCompareBackToFront(const SpriteInfo &info1, const SpriteInfo &info2);
	bool HeapCompareFrontToBack(const SpriteInfo &info1, const SpriteInfo &info2);
}

#endif