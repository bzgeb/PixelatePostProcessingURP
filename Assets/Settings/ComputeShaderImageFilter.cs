using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ComputeShaderImageFilter : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        public ComputeShader FilterComputeShader;
        public string KernelName;
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            //cmd.GetTemporaryRT("_Result", cameraTargetDescriptor);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            var mainKernel = FilterComputeShader.FindKernel(KernelName);
            FilterComputeShader.GetKernelThreadGroupSizes(mainKernel, out uint xGroupSize, out uint yGroupSize, out _);
            /*
                       Mathf.CeilToInt(_renderTexture.width / (float)BlockSize / xGroupSize),
                       Mathf.CeilToInt(_renderTexture.height / (float)BlockSize / yGroupSize),
                       1); 
                       */
            cmd.DispatchCompute(FilterComputeShader, mainKernel, 0, 0, 0);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    #region Renderer Feature
    CustomRenderPass _scriptablePass;
    public ComputeShader FilterComputeShader;
    public string KernelName;

    /// <inheritdoc/>
    public override void Create()
    {
        _scriptablePass = new CustomRenderPass
        {
            FilterComputeShader = FilterComputeShader, 
            KernelName = KernelName,
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_scriptablePass);
    }
    #endregion
}


