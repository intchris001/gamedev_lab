#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WireOnPlay
{
    [InitializeOnLoadMethod]
    private static void Setup()
    {
        EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                try { WireSceneForPlayMode(); } catch {} // run wiring once when returning to edit mode to avoid playmode API differences
            }
        };
    }

    private static uint GetStudentNumberFromSubmission()
    {
        try
        {
            var root = System.IO.Directory.GetCurrentDirectory();
            var file = System.IO.Path.Combine(root, "COMPLETE-BEFORE-SUBMISSION.txt");
            if (System.IO.File.Exists(file))
            {
                var lines = System.IO.File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("Student number:"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1 && uint.TryParse(parts[1].Trim(), out uint val))
                            return val;
                    }
                }
            }
        }
        catch { }
        return 0u;
    }

    private static void WireSceneForPlayMode()
    {
        var loadManager = GameObject.Find("LoadManager") ?? new GameObject("LoadManager");
        var la = loadManager.GetComponent<LoadAssets>() ?? loadManager.AddComponent<LoadAssets>();

        var redPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RedPrefab.prefab");
        var bluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BluePrefab.prefab");
        if (redPrefab != null) la.redObj = redPrefab;

        if (bluePrefab != null)
        {
            var so = new SerializedObject(la);
            var prop = so.FindProperty("blueObj");
            if (prop != null) { prop.objectReferenceValue = bluePrefab; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        var checker = GameObject.Find("Progress Checker") ?? new GameObject("Progress Checker");
        var pe = checker.GetComponent<ProgressEvaluator>() ?? checker.AddComponent<ProgressEvaluator>();
        var peSO = new SerializedObject(pe);
        var snProp = peSO.FindProperty("studentNumber");
        var brProp = peSO.FindProperty("bandReached");
        uint sn = GetStudentNumberFromSubmission();
        if (snProp != null && sn > 0) snProp.uintValue = sn;
        if (brProp != null) brProp.enumValueIndex = 6; // HD100
        peSO.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif

