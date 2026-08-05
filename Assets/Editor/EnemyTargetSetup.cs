using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyTargetSetup
{
    private const string ScenePath = "Assets/Game/Scenes/GameScene.unity";
    private const string CharacterFolder = "Assets/ThirdParty/KayKit/Adventurers/Characters/";
    private const string SharedAvatarPath = CharacterFolder + "Rogue_Hooded.fbx";
    private const string PropFolder = "Assets/ThirdParty/KayKit/Adventurers/Props/";
    private const string ConfigFolder = "Assets/Game/Configs/Enemies";
    private const string KnightConfigPath = "Assets/Game/Configs/EnemyConfig.asset";
    private const string PrefabFolder = "Assets/Game/Prefabs/Enemies";

    private static readonly EnemyDefinition[] Definitions =
    {
        new EnemyDefinition("Knight", "Knight.fbx", "sword_1handed.fbx", KnightConfigPath, 80f, 2.8f, 18f),
        new EnemyDefinition("Barbarian", "Barbarian.fbx", "axe_1handed.fbx", ConfigFolder + "/Enemy_Barbarian.asset", 130f, 2.4f, 26f),
        new EnemyDefinition("Mage", "Mage.fbx", "wand.fbx", ConfigFolder + "/Enemy_Mage.asset", 55f, 2.6f, 22f),
        new EnemyDefinition("Ranger", "Ranger.fbx", "dagger.fbx", ConfigFolder + "/Enemy_Ranger.asset", 70f, 3.2f, 16f),
        new EnemyDefinition("Rogue", "Rogue.fbx", "dagger.fbx", ConfigFolder + "/Enemy_Rogue.asset", 65f, 3.7f, 14f)
    };

    [InitializeOnLoadMethod]
    private static void SetupAfterCompile()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Hero vs Enemy/Setup Enemy Target")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Directory.CreateDirectory(ConfigFolder);
        Directory.CreateDirectory(PrefabFolder);
        AssetDatabase.Refresh();

        foreach (var definition in Definitions)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterFolder + definition.ModelFile);
            var weapon = AssetDatabase.LoadAssetAtPath<GameObject>(PropFolder + definition.WeaponFile);
            if (model == null || weapon == null) continue;

            var config = GetOrCreateConfig(definition, model);
            BuildPrefab(definition, config.ModelPrefab != null ? config.ModelPrefab : model, weapon, config);
        }

        AddEnemyToActiveScene();
        AssetDatabase.SaveAssets();
    }

    private static EnemyConfig GetOrCreateConfig(EnemyDefinition definition, GameObject model)
    {
        var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(definition.ConfigPath);
        var wasCreated = config == null;
        if (wasCreated)
        {
            config = ScriptableObject.CreateInstance<EnemyConfig>();
            AssetDatabase.CreateAsset(config, definition.ConfigPath);
        }

        var serializedConfig = new SerializedObject(config);
        var modelProperty = serializedConfig.FindProperty("modelPrefab");
        var selectedModelPath = AssetDatabase.GetAssetPath(modelProperty.objectReferenceValue);
        var pointsToEnemyPrefab = !string.IsNullOrEmpty(selectedModelPath) &&
                                  selectedModelPath.StartsWith(PrefabFolder + "/");
        if (modelProperty.objectReferenceValue == null || pointsToEnemyPrefab)
            modelProperty.objectReferenceValue = model;

        if (wasCreated)
        {
            serializedConfig.FindProperty("maxHealth").floatValue = definition.MaxHealth;
            serializedConfig.FindProperty("moveSpeed").floatValue = definition.MoveSpeed;
            serializedConfig.FindProperty("attackDamage").floatValue = definition.AttackDamage;
        }

        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        return config;
    }

    private static void BuildPrefab(
        EnemyDefinition definition,
        GameObject model,
        GameObject weaponModel,
        EnemyConfig config)
    {
        var root = new GameObject(definition.PrefabName);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);

        var rightHandSlot = FindDeepChild(visual.transform, "handslot.r");
        if (rightHandSlot != null)
        {
            var weapon = (GameObject)PrefabUtility.InstantiatePrefab(weaponModel);
            weapon.name = definition.WeaponName;
            weapon.transform.SetParent(rightHandSlot, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogError("Enemy weapon was not attached: handslot.r was not found.");
        }
        var animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            PlayerMovementPrefabSetup.GetOrCreateEnemyAnimatorController();
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(SharedAvatarPath)
            .OfType<Avatar>()
            .FirstOrDefault();
        if (animator.avatar == null)
            Debug.LogError("Enemy Animator Avatar was not assigned: Rogue_Hooded Avatar is missing.");
        animator.applyRootMotion = false;

        var controller = root.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.height = 1.8f;
        controller.radius = 0.4f;

        var motor = root.AddComponent<CharacterMotor>();
        var serializedMotor = new SerializedObject(motor);
        serializedMotor.FindProperty("config").objectReferenceValue = config;
        serializedMotor.ApplyModifiedPropertiesWithoutUndo();

        var characterAnimator = root.AddComponent<CharacterAnimator>();
        var serializedAnimator = new SerializedObject(characterAnimator);
        serializedAnimator.FindProperty("animator").objectReferenceValue = animator;
        serializedAnimator.FindProperty("movementDampTime").floatValue = config.AnimationDampTime;
        serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

        var movement = root.AddComponent<EnemyMovement>();
        var serializedMovement = new SerializedObject(movement);
        serializedMovement.FindProperty("config").objectReferenceValue = config;
        serializedMovement.ApplyModifiedPropertiesWithoutUndo();

        var health = root.AddComponent<EnemyHealth>();
        var serializedHealth = new SerializedObject(health);
        serializedHealth.FindProperty("config").objectReferenceValue = config;
        serializedHealth.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, definition.PrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var found = FindDeepChild(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    private static void AddEnemyToActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == "Enemy_Knight") return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Enemy_Knight.prefab");
        if (prefab == null) return;

        var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        enemy.name = "Enemy_Knight";
        enemy.transform.SetPositionAndRotation(new Vector3(0f, 0f, 6f), Quaternion.Euler(0f, 180f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Enemy target added to GameScene.");
    }

    private sealed class EnemyDefinition
    {
        public readonly string Name;
        public readonly string ModelFile;
        public readonly string WeaponFile;
        public readonly string ConfigPath;
        public readonly float MaxHealth;
        public readonly float MoveSpeed;
        public readonly float AttackDamage;

        public string PrefabName => "Enemy_" + Name;
        public string PrefabPath => PrefabFolder + "/" + PrefabName + ".prefab";
        public string WeaponName => Path.GetFileNameWithoutExtension(WeaponFile);

        public EnemyDefinition(
            string name,
            string modelFile,
            string weaponFile,
            string configPath,
            float maxHealth,
            float moveSpeed,
            float attackDamage)
        {
            Name = name;
            ModelFile = modelFile;
            WeaponFile = weaponFile;
            ConfigPath = configPath;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            AttackDamage = attackDamage;
        }
    }
}
