using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[VolumeComponentMenu("Custom Post-processing/DOF_BokehBlur")]
public class DOF_BokehBlur : CustomVolumeComponent
{
    //散景模糊
    public ClampedFloatParameter blurSize = new ClampedFloatParameter(0f, 0f, 0.01f); //模糊强度
    public ClampedFloatParameter iterations = new ClampedFloatParameter(5, 1f, 500f); //迭代次数
    public ClampedIntParameter RTDownSample = new ClampedIntParameter(1, 1, 10); //降采样次数    //散景模糊
    //景深
    public ClampedFloatParameter start = new ClampedFloatParameter(0f, 0f, 10f); 
    public ClampedFloatParameter end = new ClampedFloatParameter(4, 0f, 100f); 
    public ClampedIntParameter density = new ClampedIntParameter(1, 1, 10); 

    Material material;
    const string shaderName = "URP/Post/DOF_BokehBlur";

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
    public override bool IsActive() => material != null && blurSize.value > 0f;

    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RenderTargetIdentifier source, RenderTargetIdentifier destination)
    {
        if (material == null)
            return;

        //散景模糊
        material.SetFloat("_Iteration", iterations.value);
        material.SetFloat("_BlurSize", blurSize.value);
        material.SetFloat("_DownSample", RTDownSample.value);     
        //景深
        material.SetFloat("_Start", start.value);
        material.SetFloat("_End", end.value);
        material.SetFloat("_Density", density.value);

        //利用缩放对图像进行降采样
        int rtW = renderingData.cameraData.cameraTargetDescriptor.width / RTDownSample.value;
        int rtH = renderingData.cameraData.cameraTargetDescriptor.height / RTDownSample.value;

        renderingData.cameraData.cameraTargetDescriptor.width = RTDownSample.value;
        renderingData.cameraData.cameraTargetDescriptor.height = RTDownSample.value;

        //临时RT
        RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);
        //将该临时渲染纹理的滤波模式设置为双线性
        buffer0.filterMode = FilterMode.Bilinear;

        //源纹理到临时RT
        cmd.Blit(source, buffer0, material, 0);

        //临时RT到目标纹理
        cmd.Blit(buffer0, destination, material);
    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(material); //在Dispose中销毁材质
    }
}