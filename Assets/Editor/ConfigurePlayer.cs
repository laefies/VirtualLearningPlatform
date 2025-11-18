using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConfigurePlayer
{
    [MenuItem("Tools/MRDevice/MagicLeap2")]
    public static void SetMagicLeap2()
    {
        SetGraphicsAPI(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.DXT;
        Debug.Log("PlayerSettings.companyName set to DeviceACompany");
    }

    [MenuItem("Tools/MRDevice/MetaQuest3")]
    public static void SetMetaQuest3()
    {
        SetGraphicsAPI(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        Debug.Log("PlayerSettings.companyName set to DeviceBCompany");
    }

    private static void SetGraphicsAPI(BuildTarget target, GraphicsDeviceType[] apis)
    {
        if (PlayerSettings.GetGraphicsAPIs(target) != apis)
        {
            PlayerSettings.SetGraphicsAPIs(target, apis);
            Debug.Log($"Graphics API set for {target}: {string.Join(", ", apis)}");
        }
    }

}