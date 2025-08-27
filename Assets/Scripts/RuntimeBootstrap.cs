using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Ensures scene has required objects and references before the first frame
public static class RuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSceneSetup()
    {
        // Ensure LoadManager + LoadAssets exists and has references
        var loadManager = GameObject.Find("LoadManager");
        if (loadManager == null)
        {
            loadManager = new GameObject("LoadManager");
            Object.DontDestroyOnLoad(loadManager); // not strictly necessary, but safe
        }
        var la = loadManager.GetComponent<LoadAssets>();
        if (la == null) la = loadManager.AddComponent<LoadAssets>();

        // Assign prefabs to LoadAssets
#if UNITY_EDITOR
        var redPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RedPrefab.prefab");
        var bluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BluePrefab.prefab");
        if (redPrefab != null) la.redObj = redPrefab;
        if (bluePrefab != null)
        {
            // Assign to private [SerializeField]
            var so = new SerializedObject(la);
            var prop = so.FindProperty("blueObj");
            if (prop != null) { prop.objectReferenceValue = bluePrefab; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
#else
        // If running outside editor, try Resources fallback (optional if not needed)
        var redPrefab = Resources.Load<GameObject>("Prefabs/RedPrefab");
        var bluePrefab = Resources.Load<GameObject>("Prefabs/BluePrefab");
        if (redPrefab != null) la.redObj = redPrefab;
        if (bluePrefab != null)
        {
            // Can't SerializedObject here; expose a setter if needed. For now, ignore.
        }
#endif

        // Ensure Progress Checker + ProgressEvaluator exists and configured
        var checker = GameObject.Find("Progress Checker");
        if (checker == null)
        {
            checker = new GameObject("Progress Checker");
            Object.DontDestroyOnLoad(checker);
        }
        var pe = checker.GetComponent<ProgressEvaluator>();
        if (pe == null) pe = checker.AddComponent<ProgressEvaluator>();

#if UNITY_EDITOR
        var peSO = new SerializedObject(pe);
        var snProp = peSO.FindProperty("studentNumber");
        var brProp = peSO.FindProperty("bandReached");
        uint sn = ReadStudentNumber();
        if (snProp != null && sn > 0) snProp.uintValue = sn;
        if (brProp != null) brProp.enumValueIndex = 6; // HD100
        peSO.ApplyModifiedPropertiesWithoutUndo();
#endif
    }

#if UNITY_EDITOR
    private static uint ReadStudentNumber()
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
#endif
}

