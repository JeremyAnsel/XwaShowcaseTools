using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.GameWindow;
using System.Runtime.InteropServices;

namespace XwaSizeComparison
{
    class OptRenderer
    {
        private readonly DeviceResources deviceResources;
        private readonly MainResources mainResources;
        private readonly GroundResources groundResources;
        private readonly OptResources optResources;

        private D3dConstantBufferGlobalData constantData = default;

        public OptRenderer(DeviceResources deviceResources, MainResources mainResources, GroundResources groundResources, OptResources optResources)
        {
            this.deviceResources = deviceResources;
            this.mainResources = mainResources;
            this.groundResources = groundResources;
            this.optResources = optResources;
        }

        public void Render(in D3dConstantBufferGlobalData constantData)
        {
            bool showShadowVolumes = false;
            var context = this.deviceResources.D3DContext;

            this.constantData = constantData;

            context.UpdateSubresource(this.mainResources.ConstantBufferGlobal, 0, null, this.constantData, 0, 0);

            context.VertexShaderSetConstantBuffers(0, new[] { this.mainResources.ConstantBufferGlobal });
            context.GeometryShaderSetConstantBuffers(0, new[] { this.mainResources.ConstantBufferGlobal });
            context.PixelShaderSetConstantBuffers(0, new[] { this.mainResources.ConstantBufferGlobal });
            context.PixelShaderSetSamplers(0, new[] { this.mainResources.Sampler });

            context.InputAssemblerSetInputLayout(this.mainResources.InputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.VertexShaderSetShaderResources(0, new[] { this.optResources.PositionsSRV, this.optResources.NormalsSRV, this.optResources.TextureCoordinatesSRV });
            context.PixelShaderSetShaderResources(10, this.optResources.Textures);

            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Depth, 1.0f, 0);

            // wireframe
            //this.RenderSceneSolid(
            //    this.mainResources.ShaderVSMain,
            //    null,
            //    this.mainResources.ShaderPSMain,
            //    this.mainResources.NoBlendingBlendState,
            //    this.mainResources.DisableDepthWriteDepthStencilState,
            //    0,
            //    this.mainResources.RasterizerStateWireframe);

            this.RenderSceneGround(
                this.groundResources.ShaderVSGround,
                null,
                null, //this.mainResources.ShaderPSDepth,
                this.mainResources.NoBlendingBlendState,
                this.mainResources.EnableDepthDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState);

            this.RenderSceneSolid(
                this.mainResources.ShaderVSMain,
                null,
                null, //this.mainResources.ShaderPSDepth,
                this.mainResources.NoBlendingBlendState,
                this.mainResources.EnableDepthDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState);

            context.ClearDepthStencilView(this.deviceResources.D3DDepthStencilView, D3D11ClearOptions.Stencil, 1.0f, 0);

            if (showShadowVolumes)
            {
                this.RenderSceneSolid(
                    this.mainResources.ShaderVSShadow,
                    this.mainResources.ShaderGSShadow,
                    this.mainResources.ShaderPSShadow,
                    this.mainResources.SrcAlphaBlendingBlendState,
                    this.mainResources.TwoSidedStencilDepthStencilState,
                    1,
                    this.mainResources.DisableCullingRasterizerState);
            }
            else
            {
                this.RenderSceneSolid(
                    this.mainResources.ShaderVSShadow,
                    this.mainResources.ShaderGSShadow,
                    this.mainResources.ShaderPSShadow,
                    this.mainResources.DisableFrameBufferBlendState,
                    this.mainResources.TwoSidedStencilDepthStencilState,
                    1,
                    this.mainResources.DisableCullingRasterizerState);
            }

            this.RenderSceneGround(
                this.groundResources.ShaderVSGround,
                null,
                this.groundResources.ShaderPSGroundShadow,
                this.mainResources.DefaultBlendState,
                this.mainResources.RenderInShadowsDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState);

            this.RenderSceneSolid(
                this.mainResources.ShaderVSMain,
                null,
                this.mainResources.ShaderPSAmbient,
                this.mainResources.DefaultBlendState,
                this.mainResources.RenderInShadowsDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState);

            this.RenderSceneGround(
                this.groundResources.ShaderVSGround,
                null,
                this.groundResources.ShaderPSGround,
                this.mainResources.DefaultBlendState,
                this.mainResources.RenderNonShadowsDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState);

            this.RenderSceneSolid(
                this.mainResources.ShaderVSMain,
                null,
                this.mainResources.ShaderPSMain,
                this.mainResources.DefaultBlendState,
                this.mainResources.RenderNonShadowsDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState);

            XMMatrix m = constantData.World.ToMatrix().Transpose() * constantData.View.ToMatrix().Transpose();
            SceneTriangles.Build(this.optResources.TransparentIndices, this.optResources.TransparentCenters, m);

            this.RenderSceneTriangles(
                this.mainResources.ShaderVSMain,
                null,
                this.mainResources.ShaderPSMain,
                this.mainResources.AlphaBlendingBlendState,
                this.mainResources.DisableDepthWriteDepthStencilState,
                0,
                this.mainResources.DisableCullingRasterizerState,
                this.optResources.TransparentIndices,
                this.optResources.TransparentVertexBuffer);
        }

        private void RenderSceneGround(
                D3D11VertexShader vs,
                D3D11GeometryShader gs,
                D3D11PixelShader ps,
                D3D11BlendState blendState,
                D3D11DepthStencilState depthStencilState,
                uint stencilReference,
                D3D11RasterizerState rasterizerState)
        {
            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(vs, null);
            context.GeometryShaderSetShader(gs, null);
            context.PixelShaderSetShader(ps, null);
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            context.InputAssemblerSetInputLayout(this.groundResources.InputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.InputAssemblerSetVertexBuffers(0, new[] { this.groundResources.VertexBuffer }, new[] { (uint)Marshal.SizeOf<XMFloat3>() }, new[] { 0U });
            context.InputAssemblerSetIndexBuffer(this.groundResources.IndexBuffer, DxgiFormat.R32UInt, 0);

            context.DrawIndexed(this.groundResources.IndicesCount, 0, 0);
        }

        private void RenderSceneSolid(
                D3D11VertexShader vs,
                D3D11GeometryShader gs,
                D3D11PixelShader ps,
                D3D11BlendState blendState,
                D3D11DepthStencilState depthStencilState,
                uint stencilReference,
                D3D11RasterizerState rasterizerState)
        {
            if (this.optResources.SolidVertexBufferLength == 0)
            {
                return;
            }

            var context = this.deviceResources.D3DContext;

            context.VertexShaderSetShader(vs, null);
            context.GeometryShaderSetShader(gs, null);
            context.PixelShaderSetShader(ps, null);
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            context.InputAssemblerSetInputLayout(this.mainResources.InputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.InputAssemblerSetVertexBuffers(0, new[] { this.optResources.SolidVertexBuffer }, new[] { (uint)Marshal.SizeOf<XMUInt4>() }, new[] { 0U });
            context.InputAssemblerSetIndexBuffer(null, DxgiFormat.Unknown, 0);

            context.Draw(this.optResources.SolidVertexBufferLength, 0);
        }

        private void RenderSceneTriangles(
                D3D11VertexShader vs,
                D3D11GeometryShader gs,
                D3D11PixelShader ps,
                D3D11BlendState blendState,
                D3D11DepthStencilState depthStencilState,
                uint stencilReference,
                D3D11RasterizerState rasterizerState,
                XMUInt4[] vertices,
                D3D11Buffer vertexBuffer)
        {
            if (vertices.Length == 0)
            {
                return;
            }

            var context = this.deviceResources.D3DContext;

            context.UpdateSubresource(vertexBuffer, 0, null, vertices, 0, 0);

            context.VertexShaderSetShader(vs, null);
            context.GeometryShaderSetShader(gs, null);
            context.PixelShaderSetShader(ps, null);
            context.OutputMergerSetBlendState(blendState, new float[] { 0.0f, 0.0f, 0.0f, 0.0f }, 0xFFFFFFFF);
            context.OutputMergerSetDepthStencilState(depthStencilState, stencilReference);
            context.RasterizerStageSetState(rasterizerState);

            context.InputAssemblerSetInputLayout(this.mainResources.InputLayout);
            context.InputAssemblerSetPrimitiveTopology(D3D11PrimitiveTopology.TriangleList);

            context.InputAssemblerSetVertexBuffers(0, new[] { vertexBuffer }, new[] { (uint)Marshal.SizeOf<XMUInt4>() }, new[] { 0U });
            context.InputAssemblerSetIndexBuffer(null, DxgiFormat.Unknown, 0);

            context.Draw((uint)vertices.Length, 0);
        }
    }
}
