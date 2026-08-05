using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoguePlayerPrefabBuilder
{
    private const string CharacterPath = "Assets/ThirdParty/KayKit/Adventurers/Characters/Rogue_Hooded.fbx";
    private const string CrossbowPath = "Assets/ThirdParty/KayKit/Adventurers/Props/crossbow_1handed.fbx";
    private const string PrefabFolder = "Assets/Game/Prefabs";
    private const string PrefabPath = PrefabFolder + "/Player_Rogue_Hooded.prefab";

    [InitializeOnLoadMethod]
    private static void BuildOnceAfterImport()
    {
        if (File.Exists(PrefabPath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isCompiling && !File.Exists(PrefabPath)) Build();
        };
    }

    [MenuItem("Tools/Hero vs Enemy/Build Rogue Player Prefab")]
    public static void Build()
    {
        var characterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        var crossbowAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CrossbowPath);

        if (characterAsset == null || crossbowAsset == null)
        {
            Debug.LogError("Rogue player prefab was not created: KayKit character or crossbow is missing.");
            return;
        }

        Directory.CreateDirectory(PrefabFolder);
        AssetDatabase.Refresh();

        var root = new GameObject("Player_Rogue_Hooded");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(characterAsset);
        visual.name = "Rogue_Hooded_Visual";
        visual.transform.SetParent(root.transform, false);

        var handSlot = FindChild(visual.transform, "handslot.r");
        if (handSlot == null)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("Rogue player prefab was not created: bone 'handslot.r' was not found.");
            return;
        }

        var crossbow = (GameObject)PrefabUtility.InstantiatePrefab(crossbowAsset);
        crossbow.name = "Crossbow_1H";
        crossbow.transform.SetParent(handSlot, false);
        crossbow.transform.localPosition = Vector3.zero;
        crossbow.transform.localRotation = Quaternion.identity;
        crossbow.transform.localScale = Vector3.one;

        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.height = 1.8f;
        collider.radius = 0.38f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Created player prefab: " + PrefabPath);
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root.name == childName) return root;

        foreach (Transform child in root)
        {
            var found = FindChild(child, childName);
            if (found != null) return found;
        }

        return null;
    }
}
