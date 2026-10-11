using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using System.Collections.Generic;

// Dựng toàn bộ game Survival Shooter từ asset bài tập: menu Tools > Survival Shooter > Build Game
public static class SurvivalShooterBuilder
{
    const string Root = "Assets/SurvivalShooter/";
    const string Gen = Root + "Generated/";
    const string ScenePath = Root + "Scenes/SurvivalShooter.unity";

    [MenuItem("Tools/Survival Shooter/Build Game")]
    public static void Build()
    {
        SetupLayers();
        EnsureFolder(Root + "Scenes");
        EnsureFolder(Gen);

        var playerCtrl = CreatePlayerController();
        var enemyCtrl = CreateEnemyController();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Môi trường + ánh sáng
        var env = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Prefabs/Environment.prefab"));
        var lights = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Prefabs/Lights.prefab"));
        foreach (var l in lights.GetComponentsInChildren<Light>()) { l.intensity = 1.1f; l.shadows = LightShadows.Soft; }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.3f, 0.42f);
        RenderSettings.skybox = null;

        // Mặt sàn vô hình để raycast chuột
        var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.name = "FloorRaycast";
        floor.layer = LayerMask.NameToLayer("Floor");
        floor.transform.rotation = Quaternion.Euler(90, 0, 0);
        floor.transform.localScale = new Vector3(100, 100, 1);
        Object.DestroyImmediate(floor.GetComponent<MeshRenderer>());

        // NavMesh (bake trước khi tạo nhân vật)
        // Sàn của Environment không có collider -> bake từ render mesh của chính Environment
        var surface = env.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.BuildNavMesh();
        var navPath = Gen + "NavMesh.asset";
        AssetDatabase.DeleteAsset(navPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navPath);

        // Người chơi
        var player = CreatePlayer(playerCtrl);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 150f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(1, 15, -22);
        camGo.transform.rotation = Quaternion.Euler(30, 0, 0);
        camGo.AddComponent<CameraFollow>().target = player.transform;

        // Nhạc nền
        var music = new GameObject("BackgroundMusic").AddComponent<AudioSource>();
        music.clip = Load<AudioClip>("Audio/Music/Background Music.mp3");
        music.loop = true;
        music.volume = 0.25f;
        music.playOnAwake = true;

        // Prefab quái
        var bunny = CreateEnemyPrefab("Zombunny", "Zombunny", "ZomBunny", 100, 10, 3.2f, enemyCtrl);
        var bear = CreateEnemyPrefab("ZomBear", "ZomBear", "ZomBear", 120, 15, 2.8f, enemyCtrl);
        var hell = CreateEnemyPrefab("Hellephant", "Hellephant", "Hellephant", 300, 50, 2.2f, enemyCtrl);

        // UI
        var ui = CreateUI(player.GetComponent<PlayerHealth>());

        // Spawner
        var mgr = new GameObject("EnemyManagers");
        AddSpawner(mgr, "ZombunnySpawner", bunny, 3f, player, new Vector3(-20.5f, 0, 12.5f), 130);
        AddSpawner(mgr, "ZomBearSpawner", bear, 4f, player, new Vector3(22.5f, 0, 15f), 240);
        AddSpawner(mgr, "HellephantSpawner", hell, 12f, player, new Vector3(0, 0, 32f), 180);

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Survival Shooter built: " + ScenePath);
    }

    static GameObject CreatePlayer(AnimatorController ctrl)
    {
        var player = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel("Player"));
        PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        player.name = "Player";
        player.tag = "Player";
        player.GetComponent<Animator>().runtimeAnimatorController = ctrl;
        player.GetComponent<Animator>().applyRootMotion = false;

        var rb = player.AddComponent<Rigidbody>();
        rb.linearDamping = Mathf.Infinity;
        rb.angularDamping = Mathf.Infinity;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var col = player.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0.2f, 0.6f, 0f);
        col.radius = 0.5f;
        col.height = 1.2f;

        var audio = player.AddComponent<AudioSource>();
        audio.clip = Load<AudioClip>("Audio/Effects/Player Hurt.wav");
        audio.playOnAwake = false;

        player.AddComponent<PlayerMovement>();
        var health = player.AddComponent<PlayerHealth>();
        health.deathClip = Load<AudioClip>("Audio/Effects/Player Death.wav");

        // Nòng súng
        var barrel = player.transform.Find("GunBarrelEnd");
        var gunFx = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Prefabs/GunParticles.prefab"), barrel);
        gunFx.transform.localPosition = Vector3.zero;
        gunFx.transform.localRotation = Quaternion.identity;

        var line = barrel.gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = Load<Material>("Materials/LineRenderMaterial.mat");
        line.sharedMaterial.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f));
        line.widthMultiplier = 0.05f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.enabled = false;

        var light = barrel.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.3f);
        light.range = 8f;
        light.intensity = 4f;
        light.enabled = false;

        var gunAudio = barrel.gameObject.AddComponent<AudioSource>();
        gunAudio.clip = Load<AudioClip>("Audio/Effects/Player GunShot.wav");
        gunAudio.playOnAwake = false;
        gunAudio.volume = 0.5f;

        barrel.gameObject.AddComponent<PlayerShooting>();
        return player;
    }

    static GameObject CreateEnemyPrefab(string name, string model, string audioPrefix, int hp, int score, float speed, AnimatorController ctrl)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(model));
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = name;
        SetLayerRecursive(go, LayerMask.NameToLayer("Shootable"));

        var anim = go.GetComponent<Animator>();
        anim.applyRootMotion = false;
        // ZomBear không có clip riêng -> dùng controller theo clip của Zombunny (cùng rig)
        anim.runtimeAnimatorController = model == "Hellephant" ? CreateEnemyController("Hellephant") : ctrl;

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var size = model == "Hellephant" ? 1.4f : 1f;
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.8f * size, 0);
        col.radius = 0.5f * size;
        col.height = 1.5f * size;

        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, 0.8f * size, 0);
        trigger.radius = 0.8f * size;

        var nav = go.AddComponent<NavMeshAgent>();
        nav.radius = 0.4f * size;
        nav.height = 1.1f * size;
        nav.speed = speed;
        nav.acceleration = 12f;
        nav.angularSpeed = 360f;
        nav.stoppingDistance = 1.0f * size;

        var audio = go.AddComponent<AudioSource>();
        audio.clip = Load<AudioClip>("Audio/Effects/" + audioPrefix + " Hurt.wav");
        audio.playOnAwake = false;

        var hit = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Prefabs/HitParticles.prefab"), go.transform);
        hit.transform.localPosition = new Vector3(0, 0.8f, 0);

        var health = go.AddComponent<EnemyHealth>();
        health.startingHealth = hp;
        health.scoreValue = score;
        health.deathClip = Load<AudioClip>("Audio/Effects/" + audioPrefix + " Death.wav");
        var attack = go.AddComponent<EnemyAttack>();
        attack.attackDamage = model == "Hellephant" ? 20 : 10;
        go.AddComponent<EnemyMovement>();

        var path = Gen + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void AddSpawner(GameObject parent, string name, GameObject enemy, float spawnTime, GameObject player, Vector3 pos, float yRot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        var point = new GameObject(name + "Point").transform;
        point.SetParent(go.transform);
        point.position = NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas) ? hit.position : pos;
        point.rotation = Quaternion.Euler(0, yRot, 0);
        var em = go.AddComponent<EnemyManager>();
        em.enemy = enemy;
        em.spawnTime = spawnTime;
        em.minSpawnTime = spawnTime * 0.4f;
        em.firstSpawnDelay = spawnTime > 10 ? 2f : spawnTime - 1f;
        em.playerHealth = player.GetComponent<PlayerHealth>();
        em.spawnPoints = new[] { point };
    }

    static GameObject CreateUI(PlayerHealth health)
    {
        var font = Load<Font>("Fonts/LuckiestGuy/LuckiestGuy.ttf");
        var canvasGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        // Nháy đỏ khi bị đánh
        var dmg = NewUI<Image>("DamageImage", canvasGo.transform);
        Stretch(dmg.rectTransform);
        dmg.color = Color.clear;
        dmg.raycastTarget = false;
        health.damageImage = dmg;

        // Thanh máu
        var hpUi = new GameObject("HealthUI", typeof(RectTransform)).GetComponent<RectTransform>();
        hpUi.SetParent(canvasGo.transform, false);
        Anchor(hpUi, new Vector2(0, 0), new Vector2(30, 30), new Vector2(320, 60), new Vector2(0, 0));

        var heart = NewUI<Image>("Heart", hpUi);
        heart.sprite = LoadSprite("Textures/Heart.png");
        heart.preserveAspect = true;
        Anchor(heart.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(50, 50), new Vector2(0, 0.5f));

        var slider = DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();
        slider.name = "HealthSlider";
        slider.transform.SetParent(hpUi, false);
        Anchor((RectTransform)slider.transform, new Vector2(0, 0.5f), new Vector2(65, 0), new Vector2(250, 22), new Vector2(0, 0.5f));
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0;
        slider.maxValue = health.startingHealth;
        slider.value = health.startingHealth;
        Object.DestroyImmediate(slider.handleRect.parent.gameObject);
        slider.handleRect = null;
        var bg = slider.transform.Find("Background").GetComponent<Image>();
        bg.sprite = LoadSprite("Textures/UISliderBG.png");
        bg.color = new Color(1, 1, 1, 0.6f);
        var fill = slider.fillRect.GetComponent<Image>();
        fill.sprite = LoadSprite("Textures/UISliderFill.png");
        fill.color = new Color(0.9f, 0.15f, 0.15f);
        var fillArea = (RectTransform)slider.fillRect.parent;
        fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;
        slider.fillRect.offsetMin = Vector2.zero; slider.fillRect.offsetMax = Vector2.zero;
        health.healthSlider = slider;

        // Điểm
        var score = NewText("ScoreText", canvasGo.transform, font, 50, "Score: 0");
        Anchor(score.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(600, 70), new Vector2(0.5f, 1));
        score.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        score.gameObject.AddComponent<ScoreManager>();

        var help = NewText("HelpText", canvasGo.transform, font, 22, "WASD: di chuyen   -   Chuot: ngam   -   Click trai: ban");
        Anchor(help.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(900, 40), new Vector2(0.5f, 0));
        help.color = new Color(1, 1, 1, 0.6f);

        // Game Over
        var go = new GameObject("GameOver", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)go.transform);
        var group = go.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        var overlay = NewUI<Image>("Overlay", go.transform);
        Stretch(overlay.rectTransform);
        overlay.color = new Color(0.08f, 0.02f, 0.12f, 0.75f);
        var title = NewText("GameOverText", go.transform, font, 110, "Game Over!");
        Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1000, 150), new Vector2(0.5f, 0.5f));
        title.color = new Color(1f, 0.85f, 0.3f);
        title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -5);
        var best = NewText("HighScoreText", go.transform, font, 44, "Best: 0");
        Anchor(best.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(800, 70), new Vector2(0.5f, 0.5f));
        var restart = NewText("RestartText", go.transform, font, 26, "Choi lai sau vai giay...");
        Anchor(restart.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(800, 50), new Vector2(0.5f, 0.5f));
        restart.color = new Color(1, 1, 1, 0.7f);

        var gom = canvasGo.AddComponent<GameOverManager>();
        gom.playerHealth = health;
        gom.gameOverGroup = group;
        gom.highScoreText = best;
        return canvasGo;
    }

    // ---------- Animator ----------

    static AnimatorController CreatePlayerController()
    {
        var path = Gen + "PlayerAC.controller";
        AssetDatabase.DeleteAsset(path);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;
        var idle = sm.AddState("Idle"); idle.motion = Clip("Player", "Idle");
        var move = sm.AddState("Move"); move.motion = Clip("Player", "Move");
        var death = sm.AddState("Death"); death.motion = Clip("Player", "Death");
        sm.defaultState = idle;
        var t = idle.AddTransition(move); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
        t = move.AddTransition(idle); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        var a = sm.AddAnyStateTransition(death); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0, "Die");
        return ac;
    }

    static AnimatorController CreateEnemyController(string model = "Zombunny")
    {
        var path = Gen + model + "AC.controller";
        AssetDatabase.DeleteAsset(path);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("PlayerDead", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;
        var move = sm.AddState("Move"); move.motion = Clip(model, "Move");
        var idle = sm.AddState("Idle"); idle.motion = Clip(model, "Idle");
        var death = sm.AddState("Death"); death.motion = Clip(model, "Death");
        sm.defaultState = move;
        var t = move.AddTransition(idle); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.If, 0, "PlayerDead");
        var a = sm.AddAnyStateTransition(death); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        return ac;
    }

    static AnimationClip Clip(string model, string name)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Root + "Models/Characters/" + model + ".fbx"))
            if (o is AnimationClip c && c.name == name) return c;
        Debug.LogWarning("Missing clip " + model + "/" + name);
        return null;
    }

    // ---------- Helpers ----------

    static void SetupLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        layers.GetArrayElementAtIndex(8).stringValue = "Floor";
        layers.GetArrayElementAtIndex(9).stringValue = "Shootable";
        tm.ApplyModifiedProperties();
    }

    static T Load<T>(string rel) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(Root + rel);
        if (a == null) Debug.LogError("Missing asset " + Root + rel);
        return a;
    }

    static GameObject LoadModel(string name)
    {
        var a = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/Characters/" + name + ".fbx");
        if (a == null) Debug.LogError("Missing model " + name);
        return a;
    }

    static Sprite LoadSprite(string rel)
    {
        var path = Root + rel;
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureFolder(string path)
    {
        path = path.TrimEnd('/');
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    static T NewUI<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(T));
        go.transform.SetParent(parent, false);
        return go.GetComponent<T>();
    }

    static Text NewText(string name, Transform parent, Font font, int size, string content)
    {
        var t = NewUI<Text>(name, parent);
        t.font = font;
        t.fontSize = size;
        t.text = content;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
