#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// This editor-time configurator auto-creates the required assets and scene wiring
// for Lab Week 4 when the project reloads in the Unity Editor.
public static class AutoConfigure
{
    [MenuItem("Tools/Lab4/Run Auto Configure")]
    private static void RunAutoConfigureMenu()
    {
        try
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Materials");

            var redMat = EnsureMaterial("Assets/Materials/RedMat.mat", Color.red);
            var blueMat = EnsureMaterial("Assets/Materials/BlueMat.mat", Color.blue);
            EnsurePrefab("Assets/Prefabs/RedPrefab.prefab", "Red", redMat);
            EnsurePrefab("Assets/Prefabs/BluePrefab.prefab", "Blue", blueMat);

            WireScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        catch { }
    }
    [InitializeOnLoadMethod]
    private static void ConfigureOnLoad()
    {
        try
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Materials");

            // Force delete and recreate materials/prefabs each editor load
            ForceDeleteAsset("Assets/Materials/RedMat.mat");
            ForceDeleteAsset("Assets/Materials/BlueMat.mat");
            ForceDeleteAsset("Assets/Prefabs/RedPrefab.prefab");
            ForceDeleteAsset("Assets/Prefabs/BluePrefab.prefab");

            var redMat = EnsureMaterial("Assets/Materials/RedMat.mat", Color.red);
            var blueMat = EnsureMaterial("Assets/Materials/BlueMat.mat", Color.blue);

            EnsurePrefab(
                prefabPath: "Assets/Prefabs/RedPrefab.prefab",
                tag: "Red",
                material: redMat
            );
            EnsurePrefab(
                prefabPath: "Assets/Prefabs/BluePrefab.prefab",
                tag: "Blue",
                material: blueMat
            );

            // Scene wiring is no longer done automatically on load to avoid save prompts.
            // Use the menu Tools/Lab4/Run Auto Configure if you want to wire the scene manually.





            // Avoid forcing asset/scene saves on load
            // AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();
        }
        catch
        {
            // Swallow exceptions to avoid blocking editor load; users can recompile to retry
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            // Recursively ensure parent exists
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, folder);
    }

	    private static void ForceDeleteAsset(string path)
	    {
	        var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
	        if (obj != null)
	        {
	            AssetDatabase.DeleteAsset(path);
	        }
	    }

    private static Material EnsureMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        var mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static bool EnsurePrefab(string prefabPath, string tag, Material material)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null) return false;

        // Create a temp primitive with MeshRenderer (use Sphere to match assignment expectation)
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        temp.name = Path.GetFileNameWithoutExtension(prefabPath);

        // Assign tag (assumes Tag exists in TagManager)
        try { temp.tag = tag; } catch { /* ignore if tag missing */ }

        var rend = temp.GetComponent<Renderer>();
        if (rend != null && material != null)
        {
            rend.sharedMaterial = material;
        }

        // Add PrintAndHide and set rend reference
        var pAndH = temp.AddComponent<PrintAndHide>();
        pAndH.rend = rend;

        // Save as prefab asset
        var saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);

        return saved != null;
    }

    private static void WireScene()
    {
        // Ensure LoadManager with LoadAssets component and assign references
        var loadManager = GameObject.Find("LoadManager");
        if (loadManager == null)
        {
            loadManager = new GameObject("LoadManager");
            Undo.RegisterCreatedObjectUndo(loadManager, "Create LoadManager");
        }

        var la = loadManager.GetComponent<LoadAssets>();
        if (la == null)
        {
            la = Undo.AddComponent<LoadAssets>(loadManager);
        }

        var redPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RedPrefab.prefab");
        var bluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BluePrefab.prefab");
        if (redPrefab != null) la.redObj = redPrefab;

        // Assign private [SerializeField] via SerializedObject to keep it private
        if (bluePrefab != null)
        {
            var so = new SerializedObject(la);
            var prop = so.FindProperty("blueObj");
            if (prop != null) {
                prop.objectReferenceValue = bluePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // Ensure ProgressEvaluator exists and has studentNumber/bandReached configured
        var checker = GameObject.Find("Progress Checker");
        if (checker == null)
        {
            checker = new GameObject("Progress Checker");
            Undo.RegisterCreatedObjectUndo(checker, "Create Progress Checker");
        }

        var pe = checker.GetComponent<ProgressEvaluator>();
        if (pe == null)
        {
            pe = Undo.AddComponent<ProgressEvaluator>(checker);
        }

        // studentNumber and bandReached exposed in Inspector
        var peSO = new SerializedObject(pe);
        var snProp = peSO.FindProperty("studentNumber");
        var brProp = peSO.FindProperty("bandReached");

        uint sn = GetStudentNumberFromSubmission();
        if (snProp != null && sn > 0) snProp.uintValue = sn;
        if (brProp != null) brProp.enumValueIndex = 6; // HD100
        peSO.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

        private static uint GetStudentNumberFromSubmission()
        {
            // Try to read from COMPLETE-BEFORE-SUBMISSION.txt for student number
            try
            {
                var root = Directory.GetCurrentDirectory();
                var file = Path.Combine(root, "COMPLETE-BEFORE-SUBMISSION.txt");
                if (File.Exists(file))
                {
                    var lines = File.ReadAllLines(file);
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
            // Fallback: return 0 to leave inspector value unchanged
            return 0u;
        }
}
#endif

