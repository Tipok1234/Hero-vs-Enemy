using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerMovementPrefabSetup
{
    private const string PrefabPath = "Assets/Game/Prefabs/Player_Rogue_Hooded.prefab";
    private const string ConfigFolder = "Assets/Game/Configs";
    private const string ConfigPath = ConfigFolder + "/PlayerConfig.asset";
    private const string AnimatorFolder = "Assets/Game/Animations";
    private const string AnimatorPath = AnimatorFolder + "/Player_Rogue_Hooded.controller";
    private const string IdleClipPath = AnimatorFolder + "/Player_Idle.anim";
    private const string RunClipPath = AnimatorFolder + "/Player_Run.anim";
    private const string AttackClipPath = AnimatorFolder + "/Player_Attack_Crossbow.anim";
    private const string HitClipPath = AnimatorFolder + "/Player_Hit.anim";
    private const string DeathClipPath = AnimatorFolder + "/Player_Death.anim";
    private const string EnemyAnimatorPath = AnimatorFolder + "/Enemy_Knight.controller";
    private const string EnemyAttackDiagonalPath = AnimatorFolder + "/Enemy_Attack_Slice_Diagonal.anim";
    private const string EnemyAttackHorizontalPath = AnimatorFolder + "/Enemy_Attack_Slice_Horizontal.anim";
    private const string EnemyAttackChopPath = AnimatorFolder + "/Enemy_Attack_Chop.anim";
    private const string CharacterPath = "Assets/ThirdParty/KayKit/Adventurers/Characters/Rogue_Hooded.fbx";
    private const string GeneralAnimationsPath = "Assets/ThirdParty/KayKit/Adventurers/Animations/Rig_Medium_General.fbx";
    private const string MovementAnimationsPath = "Assets/ThirdParty/KayKit/Adventurers/Animations/Rig_Medium_MovementBasic.fbx";
    private const string RangedAnimationsPath = "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium_CombatRanged.fbx";
    private const string MeleeAnimationsPath = "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium_CombatMelee.fbx";

    [InitializeOnLoadMethod]
    private static void SetupAfterCompile()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Hero vs Enemy/Setup Player Movement")]
    public static void Setup()
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null) return;

        var avatar = ConfigureKayKitRig();
        if (avatar == null) return;

        var config = GetOrCreateConfig();
        var animatorController = GetOrCreateAnimatorController();
        GetOrCreateEnemyAnimatorController();
        if (config == null || animatorController == null) return;

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);

        var capsuleCollider = root.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null) Object.DestroyImmediate(capsuleCollider);

        var controller = root.GetComponent<CharacterController>();
        if (controller == null) controller = root.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.height = 1.8f;
        controller.radius = 0.38f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.04f;

        var movement = root.GetComponent<PlayerMovement>();
        if (movement == null) movement = root.AddComponent<PlayerMovement>();
        var motor = root.GetComponent<CharacterMotor>();
        if (motor == null) motor = root.AddComponent<CharacterMotor>();
        var motorObject = new SerializedObject(motor);
        motorObject.FindProperty("config").objectReferenceValue = config;
        motorObject.ApplyModifiedPropertiesWithoutUndo();

        var health = root.GetComponent<PlayerHealth>();
        if (health == null) health = root.AddComponent<PlayerHealth>();
        var healthObject = new SerializedObject(health);
        healthObject.FindProperty("config").objectReferenceValue = config;
        healthObject.ApplyModifiedPropertiesWithoutUndo();

        var visual = root.transform.Find("Rogue_Hooded_Visual");
        if (visual == null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            Debug.LogError("Player setup failed: Rogue_Hooded visual root was not found.");
            return;
        }

        var animators = visual.GetComponents<Animator>();
        var characterAnimator = animators.FirstOrDefault(component =>
            PrefabUtility.GetCorrespondingObjectFromSource(component) != null);
        if (characterAnimator == null && animators.Length > 0) characterAnimator = animators[0];
        if (characterAnimator == null) characterAnimator = visual.gameObject.AddComponent<Animator>();

        foreach (var extraAnimator in animators)
        {
            if (extraAnimator != characterAnimator) Object.DestroyImmediate(extraAnimator);
        }

        characterAnimator.runtimeAnimatorController = animatorController;
        characterAnimator.avatar = avatar;
        characterAnimator.applyRootMotion = false;

        var animationController = root.GetComponent<CharacterAnimator>();
        if (animationController == null) animationController = root.AddComponent<CharacterAnimator>();
        var animationObject = new SerializedObject(animationController);
        animationObject.FindProperty("animator").objectReferenceValue = characterAnimator;
        animationObject.FindProperty("movementDampTime").floatValue = config.AnimationDampTime;
        animationObject.ApplyModifiedPropertiesWithoutUndo();

        var movementObject = new SerializedObject(movement);
        movementObject.FindProperty("config").objectReferenceValue = config;
        movementObject.ApplyModifiedPropertiesWithoutUndo();

        var attack = root.GetComponent<PlayerAttack>();
        if (attack == null) attack = root.AddComponent<PlayerAttack>();
        var weapon = FindDeepChild(root.transform, "Crossbow_1H");
        var projectileSpawnPoint = GetOrCreateProjectileSpawnPoint(weapon);
        var attackObject = new SerializedObject(attack);
        attackObject.FindProperty("config").objectReferenceValue = config;
        attackObject.FindProperty("characterAnimator").objectReferenceValue = animationController;
        attackObject.FindProperty("weapon").objectReferenceValue = weapon;
        attackObject.FindProperty("projectileSpawnPoint").objectReferenceValue = projectileSpawnPoint;
        attackObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Player config, movement and Animator Controller assigned to Player_Rogue_Hooded prefab.");
    }

    private static Transform GetOrCreateProjectileSpawnPoint(Transform weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("ProjectileSpawnPoint was not created: Crossbow_1H was not found.");
            return null;
        }

        var spawnPoint = weapon.Find("ProjectileSpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("ProjectileSpawnPoint").transform;
            spawnPoint.SetParent(weapon, false);

            // Crossbow rests at X = -90. This places the point toward its muzzle
            // and keeps the point's blue Z axis aimed forward.
            spawnPoint.localPosition = new Vector3(0f, -0.7f, 0f);
            spawnPoint.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spawnPoint.localScale = Vector3.one;
        }
        return spawnPoint;
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

    private static Avatar ConfigureKayKitRig()
    {
        var characterImporter = AssetImporter.GetAtPath(CharacterPath) as ModelImporter;
        if (characterImporter == null)
        {
            Debug.LogError("KayKit rig setup failed: Rogue_Hooded ModelImporter was not found.");
            return null;
        }

        if (characterImporter.animationType != ModelImporterAnimationType.Generic ||
            characterImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            characterImporter.animationType = ModelImporterAnimationType.Generic;
            characterImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            characterImporter.SaveAndReimport();
        }

        var avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath)
            .OfType<Avatar>()
            .FirstOrDefault();
        if (avatar == null)
        {
            Debug.LogError("KayKit rig setup failed: Rogue_Hooded Avatar was not generated.");
            return null;
        }

        ConfigureAnimationImporter(GeneralAnimationsPath, avatar);
        ConfigureAnimationImporter(MovementAnimationsPath, avatar);
        ConfigureAnimationImporter(RangedAnimationsPath, avatar);
        ConfigureAnimationImporter(MeleeAnimationsPath, avatar);
        ConfigureLooping(GeneralAnimationsPath);
        ConfigureLooping(MovementAnimationsPath);
        return avatar;
    }

    private static void ConfigureAnimationImporter(string assetPath, Avatar avatar)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return;
        if (importer.animationType == ModelImporterAnimationType.Generic &&
            importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther &&
            importer.sourceAvatar == avatar) return;

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.SaveAndReimport();
    }

    private static void ConfigureLooping(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return;

        var clips = importer.clipAnimations.Length > 0
            ? importer.clipAnimations
            : importer.defaultClipAnimations;
        var changed = false;

        foreach (var clip in clips)
        {
            var shouldLoop = clip.name.StartsWith("Idle_") ||
                             clip.name.StartsWith("Walking_") ||
                             clip.name.StartsWith("Running_");
            if (!shouldLoop || clip.loopTime) continue;

            clip.loopTime = true;
            clip.loopPose = true;
            changed = true;
        }

        if (!changed) return;
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static PlayerConfig GetOrCreateConfig()
    {
        Directory.CreateDirectory(ConfigFolder);
        AssetDatabase.Refresh();
        var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<PlayerConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);
        return config;
    }

    private static AnimatorController GetOrCreateAnimatorController()
    {
        Directory.CreateDirectory(AnimatorFolder);
        AssetDatabase.Refresh();

        var idleSource = FindClip(GeneralAnimationsPath, "Idle_A");
        var runSource = FindClip(MovementAnimationsPath, "Running_A");
        var attackSource = FindClip(RangedAnimationsPath, "Ranged_2H_Shoot");
        var hitSource = FindClip(GeneralAnimationsPath, "Hit_A");
        var deathSource = FindClip(GeneralAnimationsPath, "Death_A");
        var idle = GetOrCreateLoopingClip(idleSource, IdleClipPath, "Player_Idle");
        var run = GetOrCreateLoopingClip(runSource, RunClipPath, "Player_Run");
        var attack = GetOrCreateAttackClip(attackSource);
        var hit = GetOrCreateOneShotClip(hitSource, HitClipPath, "Player_Hit");
        var death = GetOrCreateOneShotClip(deathSource, DeathClipPath, "Player_Death");
        if (idle == null || run == null || attack == null || hit == null || death == null)
        {
            Debug.LogError("Player Animator Controller was not created: Idle_A or Running_A clip is missing.");
            return null;
        }

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
        if (existing != null)
        {
            var existingTree = AssetDatabase.LoadAllAssetsAtPath(AnimatorPath)
                .OfType<BlendTree>()
                .FirstOrDefault(tree => tree.name == "Locomotion Blend Tree");
            if (existingTree != null)
            {
                existingTree.children = new[]
                {
                    new ChildMotion { motion = idle, threshold = 0f, timeScale = 1f },
                    new ChildMotion { motion = run, threshold = 1f, timeScale = 1f }
                };
                EditorUtility.SetDirty(existingTree);
            }
            EnsureParameter(existing, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(existing, "Die", AnimatorControllerParameterType.Trigger);
            EnsureParameter(existing, "IsDead", AnimatorControllerParameterType.Bool);
            EnsureAttackState(existing, attack);
            EnsureHitAndDeathStates(existing, hit, death);
            AssetDatabase.SaveAssets();
            return existing;
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        var blendTree = new BlendTree
        {
            name = "Locomotion Blend Tree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(run, 1f);
        AssetDatabase.AddObjectToAsset(blendTree, controller);

        var stateMachine = controller.layers[0].stateMachine;
        var locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = blendTree;
        stateMachine.defaultState = locomotion;
        EnsureAttackState(controller, attack);
        EnsureHitAndDeathStates(controller, hit, death);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    public static AnimatorController GetOrCreateEnemyAnimatorController()
    {
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunClipPath);
        var hit = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipPath);
        var death = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
        var diagonal = GetOrCreateOneShotClip(
            FindClip(MeleeAnimationsPath, "Melee_1H_Attack_Slice_Diagonal"),
            EnemyAttackDiagonalPath,
            "Enemy_Attack_Slice_Diagonal");
        var horizontal = GetOrCreateOneShotClip(
            FindClip(MeleeAnimationsPath, "Melee_1H_Attack_Slice_Horizontal"),
            EnemyAttackHorizontalPath,
            "Enemy_Attack_Slice_Horizontal");
        var chop = GetOrCreateOneShotClip(
            FindClip(MeleeAnimationsPath, "Melee_1H_Attack_Chop"),
            EnemyAttackChopPath,
            "Enemy_Attack_Chop");

        if (idle == null || run == null || hit == null || death == null ||
            diagonal == null || horizontal == null || chop == null)
        {
            Debug.LogError("Enemy Animator Controller was not created: a melee clip is missing.");
            return null;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyAnimatorPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(EnemyAnimatorPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var blendTree = new BlendTree
            {
                name = "Enemy Locomotion Blend Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(run, 1f);
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            var locomotion = controller.layers[0].stateMachine.AddState("Locomotion");
            locomotion.motion = blendTree;
            controller.layers[0].stateMachine.defaultState = locomotion;
        }

        EnsureParameter(controller, "AttackVariant", AnimatorControllerParameterType.Int);
        EnsureEnemyAttackStates(controller, new[] { diagonal, horizontal, chop });
        EnsureHitAndDeathStates(controller, hit, death);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void EnsureEnemyAttackStates(
        AnimatorController controller,
        AnimationClip[] attackClips)
    {
        var stateMachine = controller.layers[0].stateMachine;
        var locomotion = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(state => state.name == "Locomotion");
        if (locomotion == null) return;

        for (var index = 0; index < attackClips.Length; index++)
        {
            var stateName = "Attack_" + (index + 1);
            var attack = stateMachine.states
                .Select(item => item.state)
                .FirstOrDefault(state => state.name == stateName);
            if (attack == null)
            {
                attack = stateMachine.AddState(stateName);
                var enterAttack = stateMachine.AddAnyStateTransition(attack);
                enterAttack.hasExitTime = false;
                enterAttack.duration = 0.05f;
                enterAttack.canTransitionToSelf = false;
                enterAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
                enterAttack.AddCondition(AnimatorConditionMode.Equals, index, "AttackVariant");

                var returnToLocomotion = attack.AddTransition(locomotion);
                returnToLocomotion.hasExitTime = true;
                returnToLocomotion.exitTime = 0.9f;
                returnToLocomotion.duration = 0.08f;
            }

            attack.motion = attackClips[index];
            EditorUtility.SetDirty(attack);
        }
    }

    private static AnimationClip GetOrCreateLoopingClip(
        AnimationClip source,
        string outputPath,
        string outputName)
    {
        if (source == null) return null;

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            EditorUtility.CopySerialized(source, clip);
            clip.name = outputName;
            AssetDatabase.CreateAsset(clip, outputPath);
        }

        clip.wrapMode = WrapMode.Loop;
        var serializedClip = new SerializedObject(clip);
        var settings = serializedClip.FindProperty("m_AnimationClipSettings");
        settings.FindPropertyRelative("m_LoopTime").boolValue = true;
        settings.FindPropertyRelative("m_LoopBlend").boolValue = true;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static AnimationClip GetOrCreateAttackClip(AnimationClip source)
    {
        if (source == null) return null;

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, AttackClipPath);
        }

        // Always refresh the local clip so changing the selected KayKit take
        // also updates an already-created Player_Attack_Crossbow asset.
        EditorUtility.CopySerialized(source, clip);
        clip.name = "Player_Attack_Crossbow";

        clip.wrapMode = WrapMode.Once;
        var serializedClip = new SerializedObject(clip);
        var settings = serializedClip.FindProperty("m_AnimationClipSettings");
        settings.FindPropertyRelative("m_LoopTime").boolValue = false;
        settings.FindPropertyRelative("m_LoopBlend").boolValue = false;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static AnimationClip GetOrCreateOneShotClip(
        AnimationClip source,
        string outputPath,
        string outputName)
    {
        if (source == null) return null;

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, outputPath);
        }

        EditorUtility.CopySerialized(source, clip);
        clip.name = outputName;
        clip.wrapMode = WrapMode.Once;

        var serializedClip = new SerializedObject(clip);
        var settings = serializedClip.FindProperty("m_AnimationClipSettings");
        settings.FindPropertyRelative("m_LoopTime").boolValue = false;
        settings.FindPropertyRelative("m_LoopBlend").boolValue = false;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void EnsureHitAndDeathStates(
        AnimatorController controller,
        AnimationClip hitClip,
        AnimationClip deathClip)
    {
        var stateMachine = controller.layers[0].stateMachine;
        var locomotion = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(state => state.name == "Locomotion");
        if (locomotion == null) return;

        var hit = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(state => state.name == "Hit");
        if (hit == null)
        {
            hit = stateMachine.AddState("Hit");
            var enterHit = stateMachine.AddAnyStateTransition(hit);
            enterHit.hasExitTime = false;
            enterHit.duration = 0.05f;
            enterHit.canTransitionToSelf = false;
            enterHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

            var returnToLocomotion = hit.AddTransition(locomotion);
            returnToLocomotion.hasExitTime = true;
            returnToLocomotion.exitTime = 0.9f;
            returnToLocomotion.duration = 0.08f;
        }
        hit.motion = hitClip;

        var death = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(state => state.name == "Death");
        if (death == null)
        {
            death = stateMachine.AddState("Death");
            var enterDeath = stateMachine.AddAnyStateTransition(death);
            enterDeath.hasExitTime = false;
            enterDeath.duration = 0.05f;
            enterDeath.canTransitionToSelf = false;
            enterDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");
        }
        death.motion = deathClip;

        EditorUtility.SetDirty(hit);
        EditorUtility.SetDirty(death);
    }

    private static void EnsureParameter(
        AnimatorController controller,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (controller.parameters.Any(parameter => parameter.name == parameterName)) return;
        controller.AddParameter(parameterName, parameterType);
    }

    private static void EnsureAttackState(AnimatorController controller, AnimationClip attackClip)
    {
        var stateMachine = controller.layers[0].stateMachine;
        var locomotion = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(state => state.name == "Locomotion");
        if (locomotion == null) return;

        var attack = stateMachine.states
            .Select(item => item.state)
            .FirstOrDefault(state => state.name == "Attack");
        if (attack == null)
        {
            attack = stateMachine.AddState("Attack");
            var enterAttack = stateMachine.AddAnyStateTransition(attack);
            enterAttack.hasExitTime = false;
            enterAttack.duration = 0.05f;
            enterAttack.canTransitionToSelf = false;
            enterAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            var returnToLocomotion = attack.AddTransition(locomotion);
            returnToLocomotion.hasExitTime = true;
            returnToLocomotion.exitTime = 0.9f;
            returnToLocomotion.duration = 0.08f;
        }

        attack.motion = attackClip;
        EditorUtility.SetDirty(attack);
    }

    private static AnimationClip FindClip(string assetPath, string clipName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == clipName);
    }
}
