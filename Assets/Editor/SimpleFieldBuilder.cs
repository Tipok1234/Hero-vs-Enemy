using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SimpleFieldBuilder
{
    private const string PlayerPrefabPath = "Assets/Game/Prefabs/Player_Rogue_Hooded.prefab";
    private const string SceneFolder = "Assets/Game/Scenes";
    private const string ScenePath = SceneFolder + "/SimpleField.unity";
    private const string MaterialFolder = "Assets/Game/Materials";
    private const string FieldMaterialPath = MaterialFolder + "/SimpleField.mat";

    [InitializeOnLoadMethod]
    private static void BuildOnceAfterImport()
    {
        if (File.Exists(ScenePath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isCompiling && !File.Exists(ScenePath)) Build();
        };
    }

    [MenuItem("Tools/Hero vs Enemy/Build Simple Field")]
    public static void Build()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError("Simple field was not created: Player_Rogue_Hooded prefab is missing.");
            return;
        }

        Directory.CreateDirectory(SceneFolder);
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "SimpleField";

        var field = GameObject.CreatePrimitive(PrimitiveType.Cube);
        field.name = "Field";
        field.transform.position = new Vector3(0f, -0.5f, 0f);
        field.transform.localScale = new Vector3(20f, 1f, 20f);

        var fieldMaterial = AssetDatabase.LoadAssetAtPath<Material>(FieldMaterialPath);
        if (fieldMaterial == null)
        {
            fieldMaterial = new Material(Shader.Find("Standard"));
            fieldMaterial.name = "Simple Field Material";
            fieldMaterial.color = new Color(0.25f, 0.52f, 0.28f);
            fieldMaterial.SetFloat("_Glossiness", 0.08f);
            AssetDatabase.CreateAsset(fieldMaterial, FieldMaterialPath);
        }
        field.GetComponent<Renderer>().sharedMaterial = fieldMaterial;

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        player.name = "Player_Rogue_Hooded";
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 11f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(44f, 0f, 0f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 48f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.40f, 0.61f, 0.75f);
        cameraObject.AddComponent<AudioListener>();

        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.91f, 0.78f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.56f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Created simple field scene: " + ScenePath);
    }

    private static void AddSceneToBuildSettings()
    {
        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.path == ScenePath) return;
        }

        var oldScenes = EditorBuildSettings.scenes;
        var newScenes = new EditorBuildSettingsScene[oldScenes.Length + 1];
        for (var i = 0; i < oldScenes.Length; i++) newScenes[i] = oldScenes[i];
        newScenes[oldScenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = newScenes;
    }
}
