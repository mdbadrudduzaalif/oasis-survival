using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class FullscreenPassDataBase : ContextItem
{
    public Material material;
    public int passIndex;
    public TextureHandle source;

    public override void Reset()
    {
        material = null;
        passIndex = 0;
        source = TextureHandle.nullHandle;
    }
}

public abstract class FullscreenPassBase<TData> : ScriptableRenderPass where TData : FullscreenPassDataBase, new()
{
    public Material material { get; set; }
    public int passIndex { get; set; }
    public bool requiresDepth { get; set; }
    public bool requiresNormals { get; set; }

    public FullscreenPassBase()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (material == null) return;
        UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
        if (resourcesData.isActiveTargetBackBuffer) return;

        TextureHandle source = resourcesData.activeColorTexture;

        RenderGraphUtils.BlitMaterialParameters blitParams = new RenderGraphUtils.BlitMaterialParameters(source, source, material, passIndex);
        renderGraph.AddBlitPass(blitParams, "OasisFullscreenPass");
    }

    public virtual void ExecuteRenderGraph(TData passData, RasterGraphContext rgContext)
    {
        if (passData.material == null) return;
        Blitter.BlitTexture(rgContext.cmd, passData.source, new Vector4(1, 1, 0, 0), passData.material, passData.passIndex);
    }
}

public abstract class FullscreenEffectBase<TPass> : ScriptableRendererFeature where TPass : FullscreenPassBase<FullscreenPassDataBase>, new()
{
    [SerializeField]
    public Material material;
    [SerializeField]
    public int passIndex = 0;
    [SerializeField]
    public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    protected TPass m_Pass;

    public override void Create()
    {
        m_Pass = new TPass();
        m_Pass.renderPassEvent = passEvent;
    }

    public virtual void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null) return;
        m_Pass.material = material;
        m_Pass.passIndex = passIndex;
        m_Pass.renderPassEvent = passEvent;
        renderer.EnqueuePass(m_Pass);
    }
}
