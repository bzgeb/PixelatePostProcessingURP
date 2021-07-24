using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ComputeShaderPixelateImageFilter : ScriptableRendererFeature
{
    #region Renderer Pass
    class PixelateRenderPass : ScriptableRenderPass
    {
        ComputeShader _filterComputeShader;
        string _kernelName;
        int _renderTargetId;

        RenderTargetIdentifier _renderTargetIdentifier;
        int _renderTextureWidth;
        int _renderTextureHeight;

        int _blockSize;

        public PixelateRenderPass(ComputeShader filterComputeShader, string kernelName, int blockSize, int renderTargetId)
        {
            _filterComputeShader = filterComputeShader;
            _kernelName = kernelName;
            _blockSize = blockSize;
            _renderTargetId = renderTargetId;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(_renderTargetId, cameraTargetDescriptor);
            _renderTargetIdentifier = new RenderTargetIdentifier(_renderTargetId);

            _renderTextureWidth = cameraTargetDescriptor.width;
            _renderTextureHeight = cameraTargetDescriptor.height;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            var mainKernel = _filterComputeShader.FindKernel(_kernelName);
            _filterComputeShader.GetKernelThreadGroupSizes(mainKernel, out uint xGroupSize, out uint yGroupSize, out _);
            cmd.Blit(renderingData.cameraData.targetTexture, _renderTargetIdentifier);
            cmd.SetComputeTextureParam(_filterComputeShader, mainKernel, _renderTargetId, _renderTargetIdentifier);
            cmd.SetComputeIntParam(_filterComputeShader, "_BlockSize", _blockSize);
            cmd.SetComputeIntParam(_filterComputeShader, "_ResultWidth", _renderTextureWidth);
            cmd.SetComputeIntParam(_filterComputeShader, "_ResultHeight", _renderTextureHeight);
            cmd.DispatchCompute(_filterComputeShader, mainKernel,
                Mathf.CeilToInt(_renderTextureWidth / (float) _blockSize / xGroupSize),
                Mathf.CeilToInt(_renderTextureHeight / (float) _blockSize / yGroupSize),
                1);
            cmd.Blit(_renderTargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_renderTargetId);
        }
    }

    #endregion

    #region Renderer Feature

    PixelateRenderPass _scriptablePass;
    bool _initialized;
    
    public ComputeShader FilterComputeShader;
    public string KernelName = "Pixelate";
    [Range(2, 40)] public int BlockSize = 3;

    /// <inheritdoc/>
    public override void Create()
    {
        if (FilterComputeShader == null)
        {
            _initialized = false;
            return;
        }
        
        int renderTargetId = Shader.PropertyToID("_ImageFilterResult");
        _scriptablePass = new PixelateRenderPass(FilterComputeShader, KernelName, BlockSize, renderTargetId);
        _scriptablePass.renderPassEvent = RenderPassEvent.AfterRendering;
        _initialized = true;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_initialized)
        {
            renderer.EnqueuePass(_scriptablePass);
        }
    }

    #endregion
}