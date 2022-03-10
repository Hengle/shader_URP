using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering.Universal;
using BoolParameter = UnityEngine.Rendering.BoolParameter;
using ColorParameter = UnityEngine.Rendering.ColorParameter;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;
using Vector2Parameter = UnityEngine.Rendering.Vector2Parameter;


[VolumeComponentMenu(VolumeDefine.Glitch + "数字条纹故障 (Digital Stripe Glitch)")]
public class GlitchDigitalStripe : CustomVolumeComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
    public ClampedFloatParameter frequency = new ClampedFloatParameter(5f, 1f, 10f);
    public ClampedFloatParameter stripeLength = new ClampedFloatParameter(0.89f, 0.0f, 0.99f);
    public ClampedIntParameter noiseTextureWidth = new ClampedIntParameter(20, 8, 256);
    public ClampedIntParameter noiseTextureHeight = new ClampedIntParameter(20, 8, 256);
    public BoolParameter needStripColorAdjust = new BoolParameter(false);
    [ColorUsageAttribute(true, true, 0f, 20f, 0.125f, 3f)]
    public ColorParameter resolution = new ColorParameter(new Color(0.1f, 0.1f, 0.1f));
    public ClampedFloatParameter StripColorAdjustIndensity = new ClampedFloatParameter(2f, 0f, 10f);

    private float randomFrequency;

    Material material;
    const string shaderName = "Hidden/PostProcessing/Glitch/DigitalStripe";

    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        if (material == null)
        {
            //使用CoreUtils.CreateEngineMaterial来从Shader创建材质
            //CreateEngineMaterial：使用提供的着色器路径创建材质。hideFlags将被设置为 HideFlags.HideAndDontSave。
            material = CoreUtils.CreateEngineMaterial(shaderName);
        }
    }

    //需要注意的是，IsActive方法最好要在组件无效时返回false，避免组件未激活时仍然执行了渲染，
    //原因之前提到过，无论组件是否添加到Volume菜单中或是否勾选，VolumeManager总是会初始化所有的VolumeComponent。
    public override bool IsActive() => material != null && intensity.value > 0f;



    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RenderTargetIdentifier source, RenderTargetIdentifier destination)
    {
        if (material == null)
            return;

        UpdateFrequency(frequency);

        material.SetFloat("_Frequency", intervalType.value == IntervalType.Random ? randomFrequency : frequency.value);
        material.SetFloat("_RGBSplit", RGBSplit.value);
        material.SetFloat("_Speed", speed.value);
        material.SetFloat("_Amount", amount.value);
        material.SetVector("_Resolution", customResolution.value ? resolution.value : new Vector2(Screen.width, Screen.height));

        cmd.Blit(source, destination, material, jitterDirection.value == Direction.Horizontal ? 0 : 1);
    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(material); //在Dispose中销毁材质
    }
}