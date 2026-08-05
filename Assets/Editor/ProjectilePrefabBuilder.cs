using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectilePrefabBuilder
{
    private const string BoltModelPath = "Assets/ThirdParty/KayKit/Adventurers/Props/arrow_crossbow.fbx";
    private const string PrefabFolder = "Assets/Game/Prefabs/Projectiles";
    private const string PrefabPath = PrefabFolder + "/CrossbowBolt.prefab";

    [InitializeOnLoadMethod]
    private static void BuildOnceAfterImport()
    {
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existingPrefab != null && existingPrefab.GetComponent<ArrowBullet>() != null) return;
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isCompiling && !File.Exists(PrefabPath)) Build();
        };
    }

    [MenuItem("Tools/Hero vs Enemy/Build Crossbow Bolt Prefab")]
    public static void Build()
    {
        var boltModel = AssetDatabase.LoadAssetAtPath<GameObject>(BoltModelPath);
        if (boltModel == null)
        {
            Debug.LogError("Crossbow bolt prefab was not created: arrow_crossbow.fbx is missing.");
            return;
        }

        Directory.CreateDirectory(PrefabFolder);
        AssetDatabase.Refresh();

        var root = new GameObject("CrossbowBolt");

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(boltModel);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.mass = 0.05f;
        rigidbody.useGravity = false;
        rigidbody.isKinematic = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var collider = root.AddComponent<CapsuleCollider>();
        collider.direction = 2;
        collider.center = new Vector3(0f, 0f, 0.28f);
        collider.radius = 0.045f;
        collider.height = 0.65f;
        collider.isTrigger = true;

        root.AddComponent<ArrowBullet>();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Created crossbow bolt prefab: " + PrefabPath);
    }
}
