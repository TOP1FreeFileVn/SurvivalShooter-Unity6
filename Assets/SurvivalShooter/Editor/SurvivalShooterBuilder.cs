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
        var ui = CreateUI(player.GetComponent<PlayerHealth>(), player.GetComponent<PlayerStats>());

        // Wave manager + điểm spawn quanh bản đồ
        var wm = new GameObject("WaveManager").AddComponent<WaveManager>();
        var points = new List<Transform>();
        foreach (var pos in new[] {
            new Vector3(-20.5f, 0, 12.5f), new Vector3(22.5f, 0, 15f), new Vector3(0, 0, 32f),
            new Vector3(-22f, 0, -12f), new Vector3(20f, 0, -18f), new Vector3(0f, 0, -24f) })
            points.Add(AddSpawnPoint(wm.transform, pos));
        wm.spawnPoints = points.ToArray();
        wm.enemies = new[] {
            new WaveManager.EnemyType { prefab = bunny, unlockWave = 1, weight = 5f },
            new WaveManager.EnemyType { prefab = bear, unlockWave = 3, weight = 3f },
            new WaveManager.EnemyType { prefab = hell, unlockWave = 6, weight = 1f },
        };
        wm.playerHealth = player.GetComponent<PlayerHealth>();
        wm.upgradeManager = ui.GetComponent<UpgradeManager>();
        wm.waveText = ui.transform.Find("WaveText").GetComponent<Text>();
        wm.timerText = ui.transform.Find("TimerText").GetComponent<Text>();
        wm.bannerText = ui.transform.Find("BannerText").GetComponent<Text>();
        wm.xpGemPrefab = CreateXpGemPrefab();
        wm.popupContainer = (RectTransform)ui.transform.Find("Popups");
        wm.popupFont = Load<Font>("Fonts/LuckiestGuy/LuckiestGuy.ttf");

        var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

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

        player.AddComponent<PlayerStats>();
        player.AddComponent<PlayerMovement>();
        var health = player.AddComponent<PlayerHealth>();

        // Hào quang lửa (bật khi chọn buff)
        var aura = new GameObject("DamageAura");
        aura.transform.SetParent(player.transform, false);
        var ring = aura.AddComponent<LineRenderer>();
        var ringMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        ringMat.SetColor("_BaseColor", new Color(1f, 0.45f, 0.1f, 0.8f));
        ringMat.SetFloat("_Surface", 1); ringMat.SetFloat("_Blend", 2);
        ringMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ringMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        ringMat.SetFloat("_ZWrite", 0); ringMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ringMat.renderQueue = 3000;
        SaveAsset(ringMat, Gen + "AuraMaterial.mat");
        ring.sharedMaterial = ringMat;
        ring.loop = true;
        ring.useWorldSpace = true;
        ring.widthMultiplier = 0.15f;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.enabled = false;
        aura.AddComponent<DamageAura>();
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
        health.xpValue = model == "Hellephant" ? 8 : model == "ZomBear" ? 2 : 1;
        health.deathClip = Load<AudioClip>("Audio/Effects/" + audioPrefix + " Death.wav");
        var attack = go.AddComponent<EnemyAttack>();
        attack.attackDamage = model == "Hellephant" ? 20 : 10;
        go.AddComponent<EnemyMovement>();

        var path = Gen + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Transform AddSpawnPoint(Transform parent, Vector3 pos)
    {
        var point = new GameObject("SpawnPoint").transform;
        point.SetParent(parent);
        point.position = NavMesh.SamplePosition(pos, out var hit, 6f, NavMesh.AllAreas) ? hit.position : pos;
        point.rotation = Quaternion.LookRotation(-new Vector3(pos.x, 0, pos.z).normalized);
        return point;
    }

    static GameObject CreateXpGemPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "XpGem";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * 0.28f;
        go.transform.rotation = Quaternion.Euler(45, 0, 45);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.2f, 1f, 0.5f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.1f, 1.2f, 0.45f));
        SaveAsset(mat, Gen + "XpGemMaterial.mat");
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<XpGem>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Gen + "XpGem.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void SaveAsset(Object asset, string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    static GameObject CreateUI(PlayerHealth health, PlayerStats stats)
    {
        var font = Load<Font>("Fonts/LuckiestGuy/LuckiestGuy.ttf");
        var canvasGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        // Số sát thương bay lên
        var popups = new GameObject("Popups", typeof(RectTransform)).GetComponent<RectTransform>();
        popups.SetParent(canvasGo.transform, false);
        Stretch(popups);

        // ---- Góc trên trái: máu + EXP ----
        var hpUi = new GameObject("HealthUI", typeof(RectTransform)).GetComponent<RectTransform>();
        hpUi.SetParent(canvasGo.transform, false);
        Anchor(hpUi, new Vector2(0, 1), new Vector2(24, -20), new Vector2(380, 90), new Vector2(0, 1));

        var heart = NewUI<Image>("Heart", hpUi);
        heart.sprite = LoadSprite("Textures/Heart.png");
        heart.preserveAspect = true;
        Anchor(heart.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(44, 44), new Vector2(0, 1));

        var hpSlider = MakeBar("HealthSlider", hpUi, new Color(0.9f, 0.15f, 0.15f), new Vector2(56, -8), new Vector2(300, 28));
        health.healthSlider = hpSlider;
        var hpText = NewText("HealthText", hpSlider.transform, font, 20, "100 / 100");
        Stretch(hpText.rectTransform);
        hpText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);
        health.healthText = hpText;

        var lvText = NewText("LevelText", hpUi, font, 26, "LV 1");
        Anchor(lvText.rectTransform, new Vector2(0, 1), new Vector2(0, -52), new Vector2(56, 30), new Vector2(0, 1));
        lvText.color = new Color(0.4f, 1f, 0.6f);
        var xpSlider = MakeBar("XpSlider", hpUi, new Color(0.25f, 0.95f, 0.5f), new Vector2(56, -56), new Vector2(300, 16));

        // Bảng chỉ số
        var statsText = NewText("StatsText", canvasGo.transform, font, 17, "");
        Anchor(statsText.rectTransform, new Vector2(0, 1), new Vector2(28, -120), new Vector2(260, 220), new Vector2(0, 1));
        statsText.alignment = TextAnchor.UpperLeft;
        statsText.color = new Color(1, 1, 1, 0.75f);
        statsText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);

        // ---- Giữa trên: wave + đồng hồ ----
        var wave = NewText("WaveText", canvasGo.transform, font, 30, "WAVE 1 / 20");
        Anchor(wave.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(500, 40), new Vector2(0.5f, 1));
        wave.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        var timer = NewText("TimerText", canvasGo.transform, font, 56, "25");
        Anchor(timer.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(300, 70), new Vector2(0.5f, 1));
        timer.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);

        // ---- Góc trên phải: điểm ----
        var score = NewText("ScoreText", canvasGo.transform, font, 34, "Score: 0");
        Anchor(score.rectTransform, new Vector2(1, 1), new Vector2(-28, -20), new Vector2(400, 50), new Vector2(1, 1));
        score.alignment = TextAnchor.MiddleRight;
        score.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        score.gameObject.AddComponent<ScoreManager>();

        var help = NewText("HelpText", canvasGo.transform, font, 20, "WASD: di chuyen   -   Chuot: ngam   -   Giu chuot trai: ban");
        Anchor(help.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(900, 34), new Vector2(0.5f, 0));
        help.color = new Color(1, 1, 1, 0.5f);

        // Chữ to giữa màn hình (WAVE X / WAVE CLEAR)
        var banner = NewText("BannerText", canvasGo.transform, font, 100, "WAVE 1");
        Anchor(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1000, 140), new Vector2(0.5f, 0.5f));
        banner.color = new Color(1f, 0.85f, 0.3f);
        banner.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -5);
        banner.gameObject.SetActive(false);

        // ---- Bảng chọn nâng cấp ----
        var um = canvasGo.AddComponent<UpgradeManager>();
        um.stats = stats;
        um.health = health;
        um.levelText = lvText;
        um.xpSlider = xpSlider;
        um.statsText = statsText;

        var panel = NewUI<Image>("UpgradePanel", canvasGo.transform);
        Stretch(panel.rectTransform);
        panel.color = new Color(0.05f, 0.02f, 0.1f, 0.85f);
        um.panel = panel.gameObject;
        var upTitle = NewText("Title", panel.transform, font, 54, "LEN CAP!");
        Anchor(upTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1100, 80), new Vector2(0.5f, 0.5f));
        upTitle.color = new Color(0.4f, 1f, 0.6f);
        upTitle.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);
        um.titleText = upTitle;

        var hint = NewText("Hint", panel.transform, font, 22, "Click vao the hoac bam phim 1 / 2 / 3");
        Anchor(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(900, 40), new Vector2(0.5f, 0.5f));
        hint.color = new Color(1, 1, 1, 0.6f);

        um.cardButtons = new Button[3];
        um.cardNames = new Text[3];
        um.cardDescriptions = new Text[3];
        um.cardRarities = new Text[3];
        um.cardBackgrounds = new Image[3];
        var panelSprite = LoadSprite("Textures/UIPanel.png");
        for (int i = 0; i < 3; i++)
        {
            var card = NewUI<Image>("Card" + (i + 1), panel.transform);
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 330, -10), new Vector2(300, 380), new Vector2(0.5f, 0.5f));
            card.gameObject.AddComponent<Outline>().effectColor = new Color(1, 1, 1, 0.25f);
            var btn = card.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;

            var rarity = NewText("Rarity", card.transform, font, 22, "[1] THUONG");
            Anchor(rarity.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(280, 34), new Vector2(0.5f, 1));
            var name = NewText("Name", card.transform, font, 36, "Ten");
            Anchor(name.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(280, 90), new Vector2(0.5f, 1));
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            var desc = NewText("Description", card.transform, font, 22, "Mo ta");
            Anchor(desc.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(260, 190), new Vector2(0.5f, 1));
            desc.alignment = TextAnchor.UpperCenter;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.color = new Color(0.92f, 0.92f, 0.95f);

            um.cardButtons[i] = btn;
            um.cardNames[i] = name;
            um.cardDescriptions[i] = desc;
            um.cardRarities[i] = rarity;
            um.cardBackgrounds[i] = card;
        }

        // ---- Game Over / Chiến thắng ----
        var go = new GameObject("GameOver", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)go.transform);
        var group = go.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
        var overlay = NewUI<Image>("Overlay", go.transform);
        Stretch(overlay.rectTransform);
        overlay.color = new Color(0.08f, 0.02f, 0.12f, 0.8f);
        var title = NewText("GameOverText", go.transform, font, 110, "GAME OVER!");
        Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1100, 150), new Vector2(0.5f, 0.5f));
        title.color = new Color(1f, 0.85f, 0.3f);
        title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -5);
        var summary = NewText("SummaryText", go.transform, font, 32, "");
        Anchor(summary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(900, 140), new Vector2(0.5f, 0.5f));
        summary.horizontalOverflow = HorizontalWrapMode.Wrap;

        var restartBtnImg = NewUI<Image>("RestartButton", go.transform);
        restartBtnImg.sprite = LoadSprite("Textures/UIButtonDefault.png");
        restartBtnImg.type = Image.Type.Sliced;
        Anchor(restartBtnImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(320, 70), new Vector2(0.5f, 0.5f));
        var restartBtn = restartBtnImg.gameObject.AddComponent<Button>();
        var rLabel = NewText("Label", restartBtnImg.transform, font, 30, "CHOI LAI (R)");
        Stretch(rLabel.rectTransform);
        rLabel.color = new Color(0.2f, 0.12f, 0.25f);

        var gom = canvasGo.AddComponent<GameOverManager>();
        gom.playerHealth = health;
        gom.gameOverGroup = group;
        gom.titleText = title;
        gom.summaryText = summary;
        gom.restartButton = restartBtn;
        return canvasGo;
    }

    static Slider MakeBar(string name, Transform parent, Color fillColor, Vector2 pos, Vector2 size)
    {
        var slider = DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();
        slider.name = name;
        slider.transform.SetParent(parent, false);
        Anchor((RectTransform)slider.transform, new Vector2(0, 1), pos, size, new Vector2(0, 1));
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0;
        Object.DestroyImmediate(slider.handleRect.parent.gameObject);
        slider.handleRect = null;
        var bg = slider.transform.Find("Background").GetComponent<Image>();
        bg.sprite = LoadSprite("Textures/UISliderBG.png");
        bg.color = new Color(0, 0, 0, 0.55f);
        var fill = slider.fillRect.GetComponent<Image>();
        fill.sprite = LoadSprite("Textures/UISliderFill.png");
        fill.color = fillColor;
        var fillArea = (RectTransform)slider.fillRect.parent;
        fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;
        slider.fillRect.offsetMin = Vector2.zero; slider.fillRect.offsetMax = Vector2.zero;
        return slider;
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
