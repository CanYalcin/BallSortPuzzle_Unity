#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BallSort.Tests.Editor
{
    /// <summary>
    /// One-time setup utility. Run via BallSort > Setup > Create Test Assembly Definition
    /// to generate the .asmdef that registers Assets/Tests/EditMode as a proper Unity
    /// EditMode test assembly. Safe to run multiple times — overwrites the existing file.
    /// </summary>
    public static class TestAssemblySetup
    {
        private const string AsmdefPath    = "Assets/Tests/EditMode/BallSort.Tests.EditMode.asmdef";
        private const string AsmdefContent =
@"{
    ""name"": ""BallSort.Tests.EditMode"",
    ""rootNamespace"": ""BallSort.Tests.EditMode"",
    ""references"": [
        ""UnityEngine.TestRunner"",
        ""UnityEditor.TestRunner"",
        ""Assembly-CSharp""

    ],
    ""includePlatforms"": [ ""Editor"" ],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": true,
    ""precompiledReferences"": [ ""nunit.framework.dll"" ],
    ""autoReferenced"": false,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";

        [MenuItem("BallSort/Setup/Create Test Assembly Definition")]
        public static void CreateAsmdef()
        {
            string dir = Path.GetDirectoryName(AsmdefPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(AsmdefPath, AsmdefContent);
            AssetDatabase.ImportAsset(AsmdefPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            Debug.Log($"[TestSetup] Assembly definition written to {AsmdefPath}");
            EditorUtility.DisplayDialog("Test Setup", "Assembly definition created.\n" +
                "Unity will recompile. Open the Test Runner window to run the tests.", "OK");
        }
    }
}
#endif
