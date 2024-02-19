using UnityEditor;
using System.IO;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build-AssetBundles")]
    private static void BuildAllAssetBundles()
    {
        var dir = "Assets/AssetBundles";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // BuildPipeline.BuildAssetBundle(dir, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }
}