#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Arma TODO el juego "El Faro de Bruma" con un clic:
/// capas, sprites, tiles, prefabs, 5 escenas, UI, audio y Build Settings.
/// Menú de Unity:  Farol > 1. Construir todo el juego
/// </summary>
public static class FarolBuilder
{
    private const string ArtDir = "Assets/Art";
    private const string AudioDir = "Assets/Audio";
    private const string PrefabDir = "Assets/Prefabs";
    private const string TileDir = "Assets/Tiles";
    private const string SceneDir = "Assets/Scenes";

    private static Material litMaterial;
    private static PhysicsMaterial2D noFriction;
    private static int groundLayer;

    private struct Segment
    {
        public int x0, x1, top;
        public Segment(int x0, int x1, int top) { this.x0 = x0; this.x1 = x1; this.top = top; }
    }

    // Tramos de suelo: desde x0 hasta x1, con la fila superior (pasto) en "top"
    private static readonly Segment[] Segments =
    {
        new Segment(0, 14, 2),
        new Segment(18, 26, 2),
        new Segment(30, 34, 3),
        new Segment(44, 52, 3),
        new Segment(56, 62, 8),
        new Segment(63, 80, 3),
    };

    private const int LevelWidth = 82;

    // ------------------------------------------------------------------ MENÚS

    [MenuItem("Farol/1. Construir todo el juego")]
    public static void BuildAll()
    {
        if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Primero importa los recursos de TextMeshPro:\n\nWindow > TextMeshPro > Import TMP Essential Resources\n\nLuego vuelve a ejecutar Farol > 1. Construir todo el juego.",
                "Entendido");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (string dir in new[] { PrefabDir, TileDir, SceneDir })
            Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        EnsureLayer("Ground");
        groundLayer = LayerMask.NameToLayer("Ground");

        SetupSprites();
        CreateSharedAssets();
        Tile[] tiles = CreateTiles();
        CreatePrefabs();

        BuildMainMenu();
        BuildStoryScene("Intro", "Level1", IntroLines, false);
        BuildLevel(tiles);
        BuildGameOver();
        BuildStoryScene("Ending", "MainMenu", EndingLines, true);

        string[] order = { "MainMenu", "Intro", "Level1", "GameOver", "Ending" };
        EditorBuildSettings.scenes = order
            .Select(n => new EditorBuildSettingsScene($"{SceneDir}/{n}.unity", true)).ToArray();

        PlayerSettings.productName = "El Faro de Bruma";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"{SceneDir}/MainMenu.unity");
        EditorUtility.DisplayDialog("Listo",
            "Juego construido. Abre la escena MainMenu y presiona Play.\n\nCuando todo funcione: Farol > 2. Compilar build (Windows).",
            "OK");
    }

    [MenuItem("Farol/2. Compilar build (Windows)")]
    public static void BuildWindows()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildDir = Path.Combine(Directory.GetParent(projectRoot).FullName, "Build");
        Directory.CreateDirectory(buildDir);

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(buildDir, "FarolDeBruma.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        string msg = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded
            ? "Build creado en:\n" + buildDir
            : "El build falló. Revisa la consola. ¿Está instalado el módulo 'Windows Build Support' en Unity Hub?";
        EditorUtility.DisplayDialog("Build", msg, "OK");
    }

    // ---------------------------------------------------------- PREPARACIÓN

    private static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    private static void SetupSprites()
    {
        foreach (string file in Directory.GetFiles(ArtDir, "*.png"))
        {
            string path = file.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void CreateSharedAssets()
    {
        string matPath = ArtDir + "/SpriteLit.mat";
        litMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (litMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader != null)
            {
                litMaterial = new Material(shader);
                AssetDatabase.CreateAsset(litMaterial, matPath);
            }
        }

        string physPath = ArtDir + "/NoFriction.physicsMaterial2D";
        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physPath);
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, physPath);
        }
    }

    private static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");

    private static AudioClip Clip(string name) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");

    // Orden: 0 rock, 1 grass, 2 brick, 3 lamp, 4 roof, 5 water, 6 star
    private static Tile[] CreateTiles()
    {
        return new[]
        {
            MakeTile("rock", true), MakeTile("grass", true), MakeTile("brick", false),
            MakeTile("lamp", false), MakeTile("roof", false), MakeTile("water", false), MakeTile("star", false)
        };
    }

    private static Tile MakeTile(string name, bool solid)
    {
        string path = $"{TileDir}/{name}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = LoadSprite("tile_" + name);
        tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // ---------------------------------------------------- SERIALIZACIÓN AYUDA

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Object target, string field, Vector2 value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerMask(Object target, string field, int layer)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).intValue = 1 << layer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStringArray(Object target, string field, string[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            arr.GetArrayElementAtIndex(i).stringValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- PREFABS

    private static GameObject NewSprite(string objName, string spriteName, int order)
    {
        var go = new GameObject(objName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.sortingOrder = order;
        if (litMaterial != null) sr.sharedMaterial = litMaterial;
        return go;
    }

    private static Light2D AddPointLight(Transform parent, Color color, float intensity, float radius)
    {
        var go = new GameObject("Light 2D");
        go.transform.SetParent(parent, false);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = 0f;
        return light;
    }

    private static void SavePrefab(GameObject go, string prefabName)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{prefabName}.prefab");
        Object.DestroyImmediate(go);
    }

    private static void CreatePrefabs()
    {
        // --- Jugador ---
        var player = NewSprite("Player", "player", 10);
        player.tag = "Player";
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = player.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.7f, 0.9f);
        box.sharedMaterial = noFriction;
        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        var pc = player.AddComponent<PlayerController>();
        SetRef(pc, "groundCheck", groundCheck.transform);
        SetLayerMask(pc, "groundLayer", groundLayer);
        SetFloat(pc, "jumpForce", 15f);
        SetRef(pc, "jumpClip", Clip("sfx_jump"));
        SetRef(pc, "hurtClip", Clip("sfx_hurt"));
        AddPointLight(player.transform, new Color(1f, 0.85f, 0.55f), 1f, 5f);
        SavePrefab(player, "Player");

        // --- Fragmento de luz ---
        var frag = NewSprite("Fragmento", "fragment", 5);
        var circle = frag.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.5f;
        var col = frag.AddComponent<Collectible>();
        SetRef(col, "pickupClip", Clip("sfx_pickup"));
        AddPointLight(frag.transform, new Color(1f, 0.9f, 0.4f), 0.9f, 2.5f);
        SavePrefab(frag, "Fragmento");

        // --- Pinchos ---
        var spikes = NewSprite("Pinchos", "spikes", 5);
        var sbox = spikes.AddComponent<BoxCollider2D>();
        sbox.isTrigger = true;
        sbox.size = new Vector2(0.8f, 0.5f);
        sbox.offset = new Vector2(0f, -0.25f);
        spikes.AddComponent<Hazard>();
        SavePrefab(spikes, "Pinchos");

        // --- Checkpoint ---
        var cp = NewSprite("Checkpoint", "checkpoint", 5);
        var cbox = cp.AddComponent<BoxCollider2D>();
        cbox.isTrigger = true;
        cbox.size = new Vector2(1f, 3f);
        cbox.offset = new Vector2(0f, 1f);
        var cpc = cp.AddComponent<Checkpoint>();
        SetRef(cpc, "checkpointClip", Clip("sfx_checkpoint"));
        SavePrefab(cp, "Checkpoint");

        // --- Trampolín ---
        var pad = NewSprite("Trampolin", "pad", 5);
        pad.layer = groundLayer;
        var pbox = pad.AddComponent<BoxCollider2D>();
        pbox.size = new Vector2(1f, 0.5f);
        pbox.offset = new Vector2(0f, -0.25f);
        var bp = pad.AddComponent<BouncePad>();
        SetFloat(bp, "bounceForce", 22f);
        SetRef(bp, "bounceClip", Clip("sfx_bounce"));
        SavePrefab(pad, "Trampolin");

        // --- Plataforma móvil (3 de ancho) ---
        var plat = NewSprite("PlataformaMovil", "platform", 5);
        plat.layer = groundLayer;
        plat.transform.localScale = new Vector3(3f, 1f, 1f);
        var prb = plat.AddComponent<Rigidbody2D>();
        prb.bodyType = RigidbodyType2D.Kinematic;
        plat.AddComponent<BoxCollider2D>();
        var mp = plat.AddComponent<MovingPlatform>();
        SetVector2(mp, "travel", new Vector2(4f, 0f));
        SetFloat(mp, "speed", 2f);
        SavePrefab(plat, "PlataformaMovil");

        // --- Puerta del faro (1x2) ---
        var door = NewSprite("PuertaFaro", "door", 3);
        var dbox = door.AddComponent<BoxCollider2D>();
        dbox.isTrigger = true;
        dbox.size = new Vector2(1f, 2f);
        var gd = door.AddComponent<GoalDoor>();
        SetRef(gd, "openClip", Clip("sfx_door"));
        SetRef(gd, "lockedClip", Clip("sfx_locked"));
        SavePrefab(door, "PuertaFaro");
    }

    private static GameObject Spawn(string prefabName, Vector3 position, Transform parent = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{prefabName}.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = position;
        if (parent != null) go.transform.SetParent(parent, true);
        return go;
    }

    // ------------------------------------------------------------ ESCENAS BASE

    private static Scene NewScene() =>
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    private static void SaveScene(Scene scene, string sceneName) =>
        EditorSceneManager.SaveScene(scene, $"{SceneDir}/{sceneName}.unity");

    private static Camera AddCamera(Color background, float size)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
        return cam;
    }

    private static void AddAudioManager(string bgmName)
    {
        var go = new GameObject("AudioManager");
        var am = go.AddComponent<AudioManager>();
        SetRef(am, "backgroundMusic", Clip(bgmName));
    }

    private static void AddGlobalLight(float intensity, Color color)
    {
        var go = new GameObject("Global Light 2D");
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.intensity = intensity;
        l.color = color;
    }

    // ---------------------------------------------------------------------- UI

    private static Canvas MakeCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
        return canvas;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string objName, string text, float size,
        TextAlignmentOptions align, Vector2 anchor, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.enableWordWrapping = true;
        return t;
    }

    private static Button MakeButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject("Boton " + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(460f, 100f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.2f, 0.4f, 1f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        MakeText(go.transform, "Texto", label, 44, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 90f), Color.white);
        return button;
    }

    private static MenuButtons AddMenuButtonsObject()
    {
        var go = new GameObject("MenuButtons");
        var mb = go.AddComponent<MenuButtons>();
        SetRef(mb, "clickClip", Clip("sfx_click"));
        return mb;
    }

    private static readonly Color MenuBackground = new Color(0.03f, 0.05f, 0.12f);
    private static readonly Color Gold = new Color(1f, 0.88f, 0.45f);

    // ------------------------------------------------------------ MAIN MENU

    private static void BuildMainMenu()
    {
        Scene scene = NewScene();
        AddCamera(MenuBackground, 5f);
        AddAudioManager("bgm_calm");
        MenuButtons menu = AddMenuButtonsObject();
        Canvas canvas = MakeCanvas();

        MakeText(canvas.transform, "Titulo", "EL FARO DE BRUMA", 110, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1500f, 160f), Gold);
        MakeText(canvas.transform, "Subtitulo", "Una pequeña luz puede guiar a todo un puerto", 36,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f),
            new Vector2(1500f, 60f), new Color(0.75f, 0.85f, 1f));

        Button play = MakeButton(canvas.transform, "Jugar", new Vector2(0f, 0f));
        UnityEventTools.AddStringPersistentListener(play.onClick, menu.LoadScene, "Intro");
        Button quit = MakeButton(canvas.transform, "Salir", new Vector2(0f, -130f));
        UnityEventTools.AddPersistentListener(quit.onClick, menu.QuitGame);

        MakeText(canvas.transform, "Controles", "A / D o flechas: moverse     Espacio: saltar", 32,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 60f),
            new Vector2(1500f, 50f), new Color(0.7f, 0.75f, 0.9f));

        SaveScene(scene, "MainMenu");
    }

    // -------------------------------------------------------- INTRO / ENDING

    private static readonly string[] IntroLines =
    {
        "Puerto Bruma vivió siempre bajo la luz de su faro. Cada noche, su llama guiaba a los pescadores de vuelta a casa.",
        "Pero una noche de tormenta, la gran lámpara se rompió en cinco fragmentos que cayeron por los acantilados.",
        "Los barcos quedaron a oscuras en el mar. Nadie sabía cómo volver.",
        "Entonces despertó Nox, un pequeño espíritu nacido de la última chispa del faro.",
        "Recupera los cinco fragmentos de luz y devuélvelos a la lámpara. Bruma cuenta contigo."
    };

    private static readonly string[] EndingLines =
    {
        "Nox colocó el último fragmento y la gran lámpara despertó.",
        "Un haz de luz cruzó la tormenta y, en el horizonte, las luces de los barcos respondieron una a una.",
        "Puerto Bruma volvió a casa. Y en lo alto del faro, una pequeña chispa sonrió.",
        "FIN"
    };

    private static void BuildStoryScene(string sceneName, string nextScene, string[] lines, bool showScore)
    {
        Scene scene = NewScene();
        AddCamera(MenuBackground, 5f);
        AddAudioManager("bgm_calm");
        Canvas canvas = MakeCanvas();

        TextMeshProUGUI story = MakeText(canvas.transform, "TextoHistoria", "", 54,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(1500f, 500f), Color.white);

        if (showScore)
        {
            TextMeshProUGUI score = MakeText(canvas.transform, "Puntaje", "", 48,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -80f),
                new Vector2(1200f, 80f), Gold);
            score.gameObject.AddComponent<ScoreDisplay>();
        }

        MakeText(canvas.transform, "Ayuda", "Espacio / clic para continuar", 30, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1200f, 50f), new Color(0.6f, 0.7f, 0.9f));

        var seq = new GameObject("StorySequence").AddComponent<StorySequence>();
        SetRef(seq, "storyText", story);
        SetStringArray(seq, "lines", lines);
        SetString(seq, "nextScene", nextScene);
        SetRef(seq, "advanceClip", Clip("sfx_click"));

        SaveScene(scene, sceneName);
    }

    // -------------------------------------------------------------- GAME OVER

    private static void BuildGameOver()
    {
        Scene scene = NewScene();
        AddCamera(new Color(0.02f, 0.02f, 0.05f), 5f);
        AddAudioManager("bgm_calm");
        MenuButtons menu = AddMenuButtonsObject();
        Canvas canvas = MakeCanvas();

        MakeText(canvas.transform, "Titulo", "Las luces se apagaron...", 90, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1500f, 140f), new Color(1f, 0.5f, 0.45f));
        MakeText(canvas.transform, "Mensaje", "Nox se quedó sin energía, pero la luz aún puede volver.", 38,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f),
            new Vector2(1500f, 60f), new Color(0.8f, 0.85f, 1f));
        TextMeshProUGUI score = MakeText(canvas.transform, "Puntaje", "", 48, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1200f, 80f), Gold);
        score.gameObject.AddComponent<ScoreDisplay>();

        Button retry = MakeButton(canvas.transform, "Reintentar", new Vector2(0f, -40f));
        UnityEventTools.AddStringPersistentListener(retry.onClick, menu.LoadScene, "Level1");
        Button main = MakeButton(canvas.transform, "Menú principal", new Vector2(0f, -170f));
        UnityEventTools.AddStringPersistentListener(main.onClick, menu.LoadScene, "MainMenu");

        SaveScene(scene, "GameOver");
    }

    // ------------------------------------------------------------------ NIVEL

    private static Tilemap MakeTilemap(string objName, Transform parent, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        if (litMaterial != null) r.sharedMaterial = litMaterial;
        return tm;
    }

    private static void BuildLevel(Tile[] tiles)
    {
        Tile rock = tiles[0], grass = tiles[1], brick = tiles[2], lamp = tiles[3],
             roof = tiles[4], water = tiles[5], star = tiles[6];

        Scene scene = NewScene();
        Camera cam = AddCamera(new Color(0.04f, 0.07f, 0.17f), 6.5f);
        AddGlobalLight(0.75f, new Color(0.65f, 0.72f, 1f));
        AddAudioManager("bgm_level");

        // ---- Mapa con Tilemaps (relato gráfico) ----
        var grid = new GameObject("Grid", typeof(Grid));
        Tilemap background = MakeTilemap("Fondo_Estrellas", grid.transform, -10);
        Tilemap decoration = MakeTilemap("Decoracion_Faro_Agua", grid.transform, -5);
        Tilemap ground = MakeTilemap("Suelo", grid.transform, 0);

        ground.gameObject.layer = groundLayer;
        var gRb = ground.gameObject.AddComponent<Rigidbody2D>();
        gRb.bodyType = RigidbodyType2D.Static;
        var tc = ground.gameObject.AddComponent<TilemapCollider2D>();
        tc.usedByComposite = true;
        ground.gameObject.AddComponent<CompositeCollider2D>();

        // Suelo
        foreach (Segment s in Segments)
        {
            for (int x = s.x0; x <= s.x1; x++)
            {
                ground.SetTile(new Vector3Int(x, s.top, 0), grass);
                for (int y = s.top - 1; y >= 0; y--)
                    ground.SetTile(new Vector3Int(x, y, 0), rock);
            }
        }
        // Muros de borde para no salirse del mapa
        for (int y = 0; y <= 16; y++)
        {
            ground.SetTile(new Vector3Int(-1, y, 0), rock);
            ground.SetTile(new Vector3Int(LevelWidth - 1, y, 0), rock);
        }
        // Agua en los huecos (decoración)
        for (int i = 0; i < Segments.Length - 1; i++)
        {
            for (int x = Segments[i].x1 + 1; x < Segments[i + 1].x0; x++)
            {
                decoration.SetTile(new Vector3Int(x, 0, 0), water);
                decoration.SetTile(new Vector3Int(x, 1, 0), water);
            }
        }
        // Faro (torre de ladrillo, lámpara y techo)
        for (int y = 4; y <= 9; y++)
            for (int x = 75; x <= 79; x++)
                decoration.SetTile(new Vector3Int(x, y, 0), brick);
        for (int x = 76; x <= 78; x++) decoration.SetTile(new Vector3Int(x, 10, 0), lamp);
        for (int x = 75; x <= 79; x++) decoration.SetTile(new Vector3Int(x, 11, 0), roof);
        // Estrellas
        var rng = new System.Random(42);
        for (int i = 0; i < 150; i++)
            background.SetTile(new Vector3Int(rng.Next(0, LevelWidth), rng.Next(6, 17), 0), star);

        // ---- Jugador ----
        GameObject player = Spawn("Player", new Vector3(2.5f, 3.6f, 0f));

        // ---- Fragmentos de luz (5) ----
        Vector2[] fragments =
        {
            new Vector2(24.5f, 5.5f), new Vector2(32.5f, 6.5f), new Vector2(48.5f, 7.5f),
            new Vector2(59.5f, 9.5f), new Vector2(70.5f, 5.5f)
        };
        foreach (Vector2 p in fragments) Spawn("Fragmento", p);

        // ---- Peligros ----
        foreach (Vector2 p in new[] { new Vector2(24.5f, 3.5f), new Vector2(47.5f, 4.5f),
                                       new Vector2(48.5f, 4.5f), new Vector2(67.5f, 4.5f) })
            Spawn("Pinchos", p);

        // Zona de caída bajo el mapa
        var kill = new GameObject("ZonaDeCaida");
        kill.transform.position = new Vector3(40f, -5f, 0f);
        var kbox = kill.AddComponent<BoxCollider2D>();
        kbox.isTrigger = true;
        kbox.size = new Vector2(200f, 2f);
        kill.AddComponent<Hazard>();

        // ---- Checkpoints, trampolín, plataforma móvil, puerta ----
        Spawn("Checkpoint", new Vector3(20.5f, 3.5f, 0f));
        Spawn("Checkpoint", new Vector3(45.5f, 4.5f, 0f));
        Spawn("Checkpoint", new Vector3(57.5f, 9.5f, 0f));
        Spawn("Trampolin", new Vector3(52.5f, 4.5f, 0f));
        Spawn("PlataformaMovil", new Vector3(37.5f, 3.5f, 0f));
        GameObject door = Spawn("PuertaFaro", new Vector3(77.5f, 5f, 0f));

        // Luz del faro
        var towerLight = new GameObject("LuzDelFaro");
        towerLight.transform.position = new Vector3(77.5f, 10.5f, 0f);
        AddPointLight(towerLight.transform, new Color(1f, 0.9f, 0.5f), 1.4f, 9f);
        AddPointLight(door.transform, new Color(1f, 0.85f, 0.5f), 0.8f, 4f);

        // ---- Historia en el nivel ----
        AddStory(new Vector2(3.5f, 5f),
            "Nox: Los cinco fragmentos cayeron por estos acantilados. Debo encontrarlos todos.");
        AddStory(new Vector2(19.5f, 5f),
            "Cuidado con los pinchos: la tormenta dejó el camino lleno de rocas afiladas.");
        AddStory(new Vector2(36.5f, 6f),
            "Una vieja plataforma de pescadores aún flota sobre el abismo. Espera el momento justo.");
        AddStory(new Vector2(57.5f, 11f),
            "Desde aquí se ve el faro... Ya casi. Aún falta un fragmento.");
        AddStory(new Vector2(70.5f, 6f),
            "El faro me espera. Con los cinco fragmentos, la lámpara volverá a brillar.");

        // ---- Sistemas ----
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        Canvas canvas = MakeCanvas();
        TextMeshProUGUI score = MakeText(canvas.transform, "Puntaje", "Puntaje: 0", 44,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(40f, -30f),
            new Vector2(600f, 60f), Gold);
        TextMeshProUGUI lives = MakeText(canvas.transform, "Vidas", "Vidas: 3", 44,
            TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -30f),
            new Vector2(500f, 60f), new Color(1f, 0.55f, 0.55f));
        TextMeshProUGUI frag = MakeText(canvas.transform, "Fragmentos", "Fragmentos: 0/5", 44,
            TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(-40f, -30f),
            new Vector2(700f, 60f), new Color(1f, 0.95f, 0.6f));
        TextMeshProUGUI msg = MakeText(canvas.transform, "Mensaje", "", 40,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 70f),
            new Vector2(1500f, 120f), Color.white);

        var hud = canvas.gameObject.AddComponent<HUDController>();
        SetRef(hud, "scoreText", score);
        SetRef(hud, "livesText", lives);
        SetRef(hud, "fragmentsText", frag);
        SetRef(hud, "messageText", msg);

        // Cámara que sigue al jugador (límites según el ancho del mapa)
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        SetRef(follow, "target", player.transform);
        float halfWidth = cam.orthographicSize * 16f / 9f;
        SetVector2(follow, "minBounds", new Vector2(halfWidth, 6.5f));
        SetVector2(follow, "maxBounds", new Vector2(LevelWidth - halfWidth, 9f));
        cam.transform.position = new Vector3(halfWidth, 6.5f, -10f);

        SaveScene(scene, "Level1");
    }

    private static void AddStory(Vector2 position, string message)
    {
        var go = new GameObject("Historia");
        go.transform.position = position;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 12f);
        var st = go.AddComponent<StoryTrigger>();
        SetString(st, "message", message);
        SetFloat(st, "duration", 5f);
    }
}
#endif
