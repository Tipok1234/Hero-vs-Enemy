using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ArrowsPoolSceneSetup
{
    private const string ScenePath = "Assets/Game/Scenes/GameScene.unity";
    private const string ProjectilePrefabPath = "Assets/Game/Prefabs/Projectiles/CrossbowBolt.prefab";

    [InitializeOnLoadMethod]
    private static void SetupAfterCompile()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Hero vs Enemy/Setup Arrows Pool")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;

        var pool = Object.FindObjectOfType<ArrowsPool>();
        if (pool == null)
            pool = new GameObject("ArrowsPool").AddComponent<ArrowsPool>();
        else
            pool.gameObject.name = "ArrowsPool";

        var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>("Assets/Game/Configs/PlayerConfig.asset");
        if (arrowPrefab == null || config == null) return;

        var serializedPool = new SerializedObject(pool);
        serializedPool.FindProperty("arrowPrefab").objectReferenceValue =
            arrowPrefab.GetComponent<ArrowBullet>();
        serializedPool.FindProperty("initialSize").intValue = config.ProjectilePoolSize;
        serializedPool.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ArrowsPool added to GameScene.");
    }
}
