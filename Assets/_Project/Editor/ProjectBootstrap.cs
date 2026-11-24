using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ProjectBootstrap
{
    private const string Root = "Assets/_Project";
    private const string PrefabDir = Root + "/Prefabs";
    private const string DataDir = Root + "/ScriptableObjects";
    private const string SceneDir = Root + "/Scenes";
    private const string DoneKey = "Raveneer.Bootstrapped";

    private static readonly string[] SceneNames = { "MainMenu", "LoadingScene", "GameScene", "GameOverScene" };

    private static readonly Color Ink = new Color(0.17f, 0.15f, 0.24f);
    private static readonly Color Graphite = new Color(0.32f, 0.3f, 0.42f);
    private static readonly Color Bone = new Color(0.99f, 0.96f, 0.88f);
    private static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
    private static readonly Color Gem = new Color(0.35f, 0.7f, 0.95f);
    private static readonly Color Berry = new Color(0.93f, 0.33f, 0.4f);
    private static readonly Color Leaf = new Color(0.36f, 0.68f, 0.42f);
    private static readonly Color Wood = new Color(0.64f, 0.45f, 0.3f);
    private static readonly Color Straw = new Color(0.94f, 0.8f, 0.5f);
    private static readonly Color Grass = new Color(0.4f, 0.68f, 0.45f);
    private static readonly Color Stone = new Color(0.7f, 0.7f, 0.78f);
    private static readonly Color Fur = new Color(0.88f, 0.62f, 0.42f);
    private static readonly Color Plumage = new Color(0.62f, 0.5f, 0.66f);
    private static readonly Color Scale = new Color(0.48f, 0.74f, 0.4f);

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        if (SessionState.GetBool(DoneKey, false)) return;
        EditorApplication.update += WaitAndRun;
    }

    private static void WaitAndRun()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        EditorApplication.update -= WaitAndRun;
        SessionState.SetBool(DoneKey, true);

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneDir + "/GameScene.unity") != null) return;

        Build();
    }

    [MenuItem("Raveneer/Exportar Android")]
    public static void ExportAndroid()
    {
        var scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);

        string output = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).Parent.FullName, "RaveneerAndroid");
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        Debug.Log("Raveneer: exportación " + report.summary.result + " en " + output);
    }

    [MenuItem("Raveneer/Build Project")]
    public static void Build()
    {
        try
        {
            EnsureFolder(PrefabDir);
            EnsureFolder(DataDir);
            EnsureFolder(SceneDir);

            ConfigureProject();

            var prefabs = BuildPrefabs();
            var data = BuildData(prefabs);
            BuildScenes(prefabs, data);
            RegisterScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SceneDir + "/MainMenu.unity");
            Debug.Log("Raveneer: proyecto generado.");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Texture2D IconTexture(string name)
    {
        string path = Root + "/Art/Icon/" + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Default || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void FillIcons(PlatformIcon[] icons, params Texture2D[] layers)
    {
        foreach (var icon in icons)
            icon.SetTextures(layers);
    }

    private static void ConfigureIcons()
    {
        var main = IconTexture("AppIcon");
        var foreground = IconTexture("AppIconForeground");
        var background = IconTexture("AppIconBackground");
        if (main == null || foreground == null || background == null) return;

        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { main }, IconKind.Any);

        var target = NamedBuildTarget.Android;
        var adaptive = PlayerSettings.GetPlatformIcons(target, AndroidPlatformIconKind.Adaptive);
        FillIcons(adaptive, background, foreground);
        PlayerSettings.SetPlatformIcons(target, AndroidPlatformIconKind.Adaptive, adaptive);
    }

    private static void ConfigureProject()
    {
        foreach (string tag in new[] { "Collectible", "Consumable" })
            if (System.Array.IndexOf(InternalEditorUtility.tags, tag) < 0)
                InternalEditorUtility.AddTag(tag);

        ConfigureIcons();

        PlayerSettings.companyName = "Raveneer";
        PlayerSettings.productName = "Raveneer";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.raveneer.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
    }

    private static void RegisterScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        foreach (string name in SceneNames)
            scenes.Add(new EditorBuildSettingsScene(SceneDir + "/" + name + ".unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }


    private const string SpriteDir = Root + "/Prefabs/Sprites";
    private static readonly Color Shadow = new Color(0.17f, 0.15f, 0.24f, 0.18f);

    private static Sprite circleSprite;
    private static Sprite squareSprite;
    private static Sprite triangleSprite;
    private static Sprite groundSprite;

    private static Sprite MakeSprite(string name, int size, float pixelsPerUnit, bool tiled, System.Func<int, int, int, Color> pixel)
    {
        string path = SpriteDir + "/" + name + ".png";
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, pixel(x, y, size));

        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void BuildSprites()
    {
        EnsureFolder(SpriteDir);

        circleSprite = MakeSprite("Circle", 128, 128f, false, (x, y, size) =>
        {
            float dx = (x + 0.5f) / size * 2f - 1f;
            float dy = (y + 0.5f) / size * 2f - 1f;
            float alpha = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * size * 0.5f);
            return new Color(1f, 1f, 1f, alpha);
        });

        squareSprite = MakeSprite("Square", 8, 8f, false, (x, y, size) => Color.white);

        triangleSprite = MakeSprite("Triangle", 64, 64f, false, (x, y, size) =>
        {
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;
            float alpha = Mathf.Clamp01(((1f - v) * 0.5f - Mathf.Abs(u - 0.5f)) * size);
            return new Color(1f, 1f, 1f, alpha);
        });

        groundSprite = MakeSprite("GroundTile", 128, 32f, true, (x, y, size) =>
        {
            float tau = Mathf.PI * 2f;
            float u = x / (float)size;
            float v = y / (float)size;
            float n = 0.5f + 0.25f * Mathf.Sin(tau * u * 2f) * Mathf.Sin(tau * v * 3f) + 0.15f * Mathf.Sin(tau * (u * 5f + v * 4f)) + 0.1f * Mathf.Sin(tau * (u * 7f - v * 6f));
            float shade = 0.9f + 0.18f * n;
            return new Color(Grass.r * shade, Grass.g * shade, Grass.b * shade, 1f);
        });
    }

    private static GameObject Shape(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 scale, Color color, int order, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        go.transform.localEulerAngles = new Vector3(0f, 0f, rotation);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static Transform Pivot(string name, Transform parent, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        return go.transform;
    }

    private static void Sticker(GameObject root, float thickness)
    {
        var shapes = new List<SpriteRenderer>(root.GetComponentsInChildren<SpriteRenderer>());
        if (shapes.Count == 0) return;

        int lowest = int.MaxValue;
        foreach (var shape in shapes)
        {
            if (shape.name == "Shadow" || shape.name == "Glow") continue;
            lowest = Mathf.Min(lowest, shape.sortingOrder);
        }

        foreach (var shape in shapes)
        {
            if (shape.name == "Shadow") { shape.sortingOrder = lowest - 2; continue; }
            if (shape.name == "Glow") { shape.sortingOrder = lowest - 2; continue; }

            var outline = new GameObject("Outline");
            outline.transform.SetParent(shape.transform, false);
            Vector3 size = shape.sprite.bounds.size;
            Vector3 world = shape.transform.lossyScale;
            float sx = 1f + 2f * thickness / Mathf.Max(0.001f, size.x * Mathf.Abs(world.x));
            float sy = 1f + 2f * thickness / Mathf.Max(0.001f, size.y * Mathf.Abs(world.y));
            outline.transform.localScale = new Vector3(sx, sy, 1f);

            var renderer = outline.AddComponent<SpriteRenderer>();
            renderer.sprite = shape.sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = lowest - 1;
        }
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefs(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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

    private class PrefabSet
    {
        public GameObject crowModel, crow, nest, coin, gem, key, berry, note, owl, snake, coyote;
    }

    private class DataSet
    {
        public ItemData coin, gem, berry;
        public ItemData[] keys = new ItemData[5];
        public EnemyStats owl, snake, coyote;
    }

    private static PrefabSet BuildPrefabs()
    {
        BuildSprites();

        var set = new PrefabSet();
        set.crowModel = BuildCrowModel();
        set.crow = BuildCrow(set.crowModel);
        set.nest = BuildNest();
        set.coin = BuildPickup("TreasureCoin", "Collectible", BuildCoinVisual);
        set.gem = BuildPickup("TreasureGem", "Collectible", BuildGemVisual);
        set.key = BuildPickup("Key", "Collectible", BuildKeyVisual);
        set.berry = BuildPickup("Berry", "Consumable", BuildBerryVisual);
        set.note = BuildNote();
        set.owl = BuildOwl();
        set.snake = BuildSnake();
        set.coyote = BuildCoyote();
        return set;
    }

    private static GameObject BuildCrowModel()
    {
        var root = new GameObject("CrowModel");
        var beak = new Color(0.45f, 0.45f, 0.5f);

        Shape("Shadow", root.transform, circleSprite, new Vector2(0.3f, -0.35f), new Vector2(2.3f, 1.1f), Shadow, 59);

        var left = Pivot("WingLeft", root.transform, new Vector2(-0.2f, 0.1f));
        Shape("Feathers", left, circleSprite, new Vector2(-0.55f, 0f), new Vector2(1.1f, 0.5f), Graphite, 60, 8f);
        var right = Pivot("WingRight", root.transform, new Vector2(0.2f, 0.1f));
        Shape("Feathers", right, circleSprite, new Vector2(0.55f, 0f), new Vector2(1.1f, 0.5f), Graphite, 60, -8f);

        Shape("Tail", root.transform, triangleSprite, new Vector2(0f, -0.15f), new Vector2(0.5f, 0.65f), Graphite, 61, 180f);
        Shape("Body", root.transform, circleSprite, Vector2.zero, new Vector2(0.62f, 1.05f), Ink, 62);
        Shape("Head", root.transform, circleSprite, new Vector2(0f, 0.52f), new Vector2(0.42f, 0.42f), Ink, 63);
        Shape("Beak", root.transform, triangleSprite, new Vector2(0f, 0.7f), new Vector2(0.17f, 0.34f), beak, 64);
        Shape("EyeLeft", root.transform, circleSprite, new Vector2(-0.11f, 0.58f), Vector2.one * 0.1f, Bone, 65);
        Shape("EyeRight", root.transform, circleSprite, new Vector2(0.11f, 0.58f), Vector2.one * 0.1f, Bone, 65);

        var flap = root.AddComponent<WingFlap>();
        SetRef(flap, "leftWing", left);
        SetRef(flap, "rightWing", right);

        Sticker(root, 0.07f);
        return SavePrefab(root, "CrowModel");
    }

    private static GameObject BuildCrow(GameObject model)
    {
        var root = new GameObject("Crow");
        root.tag = "Player";

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        visual.transform.localPosition = Vector3.zero;

        var collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        var source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;

        var controller = root.AddComponent<PlayerController>();
        SetRef(controller, "crowAudioSource", source);

        return SavePrefab(root, "Crow");
    }

    private static GameObject BuildNest()
    {
        var root = new GameObject("Nest");

        Shape("Shadow", root.transform, circleSprite, new Vector2(0.15f, -0.2f), Vector2.one * 3.4f, Shadow, 5);
        Shape("Base", root.transform, circleSprite, Vector2.zero, Vector2.one * 3.2f, Wood, 6);
        Shape("Bedding", root.transform, circleSprite, Vector2.zero, Vector2.one * 2.5f, Straw, 7);
        Shape("Hollow", root.transform, circleSprite, Vector2.zero, Vector2.one * 1.6f, new Color(Straw.r * 0.78f, Straw.g * 0.78f, Straw.b * 0.78f), 8);

        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f * Mathf.Deg2Rad;
            var position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.45f;
            Shape("Twig" + i, root.transform, squareSprite, position, new Vector2(0.95f, 0.14f), Wood, 9, i * 22.5f + 90f + (i % 2) * 14f);
        }

        var trigger = root.AddComponent<CircleCollider2D>();
        trigger.radius = 1.7f;
        trigger.isTrigger = true;

        root.AddComponent<Nest>();
        Sticker(root, 0.1f);
        return SavePrefab(root, "Nest");
    }

    private static void BuildCoinVisual(Transform parent)
    {
        Shape("Shadow", parent, circleSprite, new Vector2(0.06f, -0.08f), Vector2.one * 0.58f, Shadow, 9);
        Shape("Coin", parent, circleSprite, Vector2.zero, Vector2.one * 0.58f, Gold, 10);
        Shape("Face", parent, circleSprite, Vector2.zero, Vector2.one * 0.38f, new Color(Gold.r * 0.82f, Gold.g * 0.82f, Gold.b * 0.5f), 11);
    }

    private static void BuildGemVisual(Transform parent)
    {
        Shape("Shadow", parent, squareSprite, new Vector2(0.06f, -0.08f), Vector2.one * 0.46f, Shadow, 9, 45f);
        Shape("Gem", parent, squareSprite, Vector2.zero, Vector2.one * 0.46f, Gem, 10, 45f);
        Shape("Facet", parent, squareSprite, new Vector2(-0.04f, 0.04f), Vector2.one * 0.2f, new Color(0.7f, 0.9f, 1f), 11, 45f);
    }

    private static void BuildKeyVisual(Transform parent)
    {
        Shape("Glow", parent, circleSprite, Vector2.zero, Vector2.one * 1.1f, new Color(Gold.r, Gold.g, Gold.b, 0.28f), 9);
        Shape("Bow", parent, circleSprite, new Vector2(0f, 0.3f), Vector2.one * 0.36f, Gold, 10);
        Shape("BowHole", parent, circleSprite, new Vector2(0f, 0.3f), Vector2.one * 0.15f, Graphite, 11);
        Shape("Shaft", parent, squareSprite, new Vector2(0f, -0.06f), new Vector2(0.1f, 0.62f), Gold, 10);
        Shape("ToothTop", parent, squareSprite, new Vector2(0.1f, -0.22f), new Vector2(0.2f, 0.09f), Gold, 10);
        Shape("ToothBottom", parent, squareSprite, new Vector2(0.08f, -0.35f), new Vector2(0.16f, 0.09f), Gold, 10);
    }

    private static void BuildBerryVisual(Transform parent)
    {
        Shape("Shadow", parent, circleSprite, new Vector2(0.06f, -0.08f), Vector2.one * 0.42f, Shadow, 9);
        Shape("Fruit", parent, circleSprite, Vector2.zero, Vector2.one * 0.42f, Berry, 10);
        Shape("Shine", parent, circleSprite, new Vector2(-0.08f, 0.08f), Vector2.one * 0.1f, new Color(1f, 1f, 1f, 0.6f), 11);
        Shape("Leaf", parent, triangleSprite, new Vector2(0.06f, 0.26f), new Vector2(0.18f, 0.24f), Leaf, 11, -30f);
    }

    private static GameObject BuildPickup(string name, string tag, System.Action<Transform> visual)
    {
        var root = new GameObject(name);
        root.tag = tag;
        visual(root.transform);

        var collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;
        collider.isTrigger = true;

        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        Sticker(root, 0.06f);
        root.AddComponent<ItemPickup>();
        return SavePrefab(root, name);
    }

    private static GameObject BuildNote()
    {
        var root = new GameObject("Note");
        Shape("Shadow", root.transform, squareSprite, new Vector2(0.06f, -0.08f), new Vector2(0.62f, 0.8f), Shadow, 9, 6f);
        Shape("Paper", root.transform, squareSprite, Vector2.zero, new Vector2(0.62f, 0.8f), Bone, 10, 6f);
        for (int i = 0; i < 4; i++)
            Shape("Line" + i, root.transform, squareSprite, new Vector2(0f, 0.24f - i * 0.15f), new Vector2(0.4f, 0.03f), Graphite, 11, 6f);

        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 1.1f);
        collider.isTrigger = true;

        root.AddComponent<NoteReadable>();
        Sticker(root, 0.06f);
        return SavePrefab(root, "Note");
    }

    private static GameObject BuildOwl()
    {
        var root = new GameObject("Owl");

        Shape("Shadow", root.transform, circleSprite, new Vector2(0.35f, -0.4f), new Vector2(2.2f, 1.0f), Shadow, 51);
        Shape("WingLeft", root.transform, circleSprite, new Vector2(-0.8f, 0f), new Vector2(1.2f, 0.62f), Plumage, 53, 10f);
        Shape("WingRight", root.transform, circleSprite, new Vector2(0.8f, 0f), new Vector2(1.2f, 0.62f), Plumage, 53, -10f);
        Shape("Body", root.transform, circleSprite, Vector2.zero, new Vector2(0.95f, 1.1f), Plumage, 54);
        Shape("Face", root.transform, circleSprite, new Vector2(0f, 0.3f), new Vector2(0.7f, 0.58f), Bone, 55);
        Shape("EyeLeft", root.transform, circleSprite, new Vector2(-0.16f, 0.34f), Vector2.one * 0.26f, Gold, 56);
        Shape("EyeRight", root.transform, circleSprite, new Vector2(0.16f, 0.34f), Vector2.one * 0.26f, Gold, 56);
        Shape("PupilLeft", root.transform, circleSprite, new Vector2(-0.16f, 0.36f), Vector2.one * 0.1f, Ink, 57);
        Shape("PupilRight", root.transform, circleSprite, new Vector2(0.16f, 0.36f), Vector2.one * 0.1f, Ink, 57);
        Shape("Beak", root.transform, triangleSprite, new Vector2(0f, 0.5f), new Vector2(0.14f, 0.22f), Graphite, 57);

        root.AddComponent<EnemyBehaviour>();
        Sticker(root, 0.08f);
        return SavePrefab(root, "Owl");
    }

    private static GameObject BuildSnake()
    {
        var root = new GameObject("Snake");
        var dark = Scale;
        var light = new Color(Scale.r * 1.35f, Scale.g * 1.3f, Scale.b * 1.2f);

        Shape("Shadow", root.transform, circleSprite, new Vector2(0.12f, -1.0f), new Vector2(0.5f, 2.2f), Shadow, 19);

        for (int i = 7; i >= 1; i--)
        {
            float size = 0.44f - i * 0.035f;
            var position = new Vector2(Mathf.Sin(i * 0.9f) * 0.2f, -i * 0.3f);
            Shape("Segment" + i, root.transform, circleSprite, position, Vector2.one * size, i % 2 == 0 ? dark : light, 20 + (7 - i));
        }

        Shape("Head", root.transform, circleSprite, new Vector2(0f, 0.05f), new Vector2(0.5f, 0.56f), dark, 30);
        Shape("EyeLeft", root.transform, circleSprite, new Vector2(-0.12f, 0.15f), Vector2.one * 0.1f, Bone, 31);
        Shape("EyeRight", root.transform, circleSprite, new Vector2(0.12f, 0.15f), Vector2.one * 0.1f, Bone, 31);
        Shape("Tongue", root.transform, triangleSprite, new Vector2(0f, 0.4f), new Vector2(0.1f, 0.22f), Berry, 31);

        root.AddComponent<EnemyBehaviour>();
        Sticker(root, 0.07f);
        return SavePrefab(root, "Snake");
    }

    private static GameObject BuildCoyote()
    {
        var root = new GameObject("Coyote");
        var dark = new Color(Fur.r * 0.7f, Fur.g * 0.7f, Fur.b * 0.7f);

        Shape("Shadow", root.transform, circleSprite, new Vector2(0.15f, -0.15f), new Vector2(0.9f, 2.0f), Shadow, 19);

        foreach (float x in new[] { -0.36f, 0.36f })
            foreach (float y in new[] { -0.45f, 0.4f })
                Shape("Leg", root.transform, circleSprite, new Vector2(x, y), new Vector2(0.2f, 0.36f), dark, 21);

        Shape("Tail", root.transform, triangleSprite, new Vector2(0f, -0.65f), new Vector2(0.24f, 0.7f), dark, 22, 180f);
        Shape("Body", root.transform, circleSprite, Vector2.zero, new Vector2(0.7f, 1.3f), Fur, 23);
        Shape("Head", root.transform, circleSprite, new Vector2(0f, 0.8f), new Vector2(0.52f, 0.52f), Fur, 24);
        Shape("Snout", root.transform, triangleSprite, new Vector2(0f, 0.98f), new Vector2(0.24f, 0.4f), dark, 25);
        Shape("EarLeft", root.transform, triangleSprite, new Vector2(-0.2f, 0.98f), new Vector2(0.16f, 0.26f), dark, 25);
        Shape("EarRight", root.transform, triangleSprite, new Vector2(0.2f, 0.98f), new Vector2(0.16f, 0.26f), dark, 25);
        Shape("EyeLeft", root.transform, circleSprite, new Vector2(-0.11f, 0.84f), Vector2.one * 0.07f, Ink, 26);
        Shape("EyeRight", root.transform, circleSprite, new Vector2(0.11f, 0.84f), Vector2.one * 0.07f, Ink, 26);

        root.AddComponent<EnemyBehaviour>();
        Sticker(root, 0.07f);
        return SavePrefab(root, "Coyote");
    }

    private static T LoadOrCreate<T>(string name) where T : ScriptableObject
    {
        string path = DataDir + "/" + name + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        return asset;
    }

    private static ItemData MakeItem(string asset, string id, string label, ItemType type, float heal = 0f)
    {
        var item = LoadOrCreate<ItemData>(asset);
        item.id = id;
        item.displayName = label;
        item.type = type;
        item.healAmount = heal;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static EnemyStats MakeEnemy(string asset, string label, GameObject prefab, EnemyTimeSlot slot, bool flying, float damage, float speed, float detection, DamageType type = DamageType.Instant, float effect = 0f, EnemyMode mode = EnemyMode.World, float approach = 6f)
    {
        var enemy = LoadOrCreate<EnemyStats>(asset);
        enemy.enemyName = label;
        enemy.prefab = prefab;
        enemy.activeDuring = slot;
        enemy.flying = flying;
        enemy.damage = damage;
        enemy.speed = speed;
        enemy.detectionRadius = detection;
        enemy.damageType = type;
        enemy.effectSeconds = effect;
        enemy.mode = mode;
        enemy.arApproachSeconds = approach;
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static DataSet BuildData(PrefabSet prefabs)
    {
        var set = new DataSet();
        set.coin = MakeItem("Item_Coin", "coin", "Moneda", ItemType.Treasure);
        set.gem = MakeItem("Item_Gem", "gem", "Gema", ItemType.Treasure);
        set.berry = MakeItem("Item_Berry", "berry", "Baya", ItemType.Food, 25f);
        for (int i = 0; i < 5; i++)
            set.keys[i] = MakeItem("Item_Key" + (i + 1), "key_" + (i + 1), "Llave " + (i + 1), ItemType.Key);

        set.owl = MakeEnemy("Enemy_Owl", "Búho", prefabs.owl, EnemyTimeSlot.Night, true, 18f, 3.2f, 10f, DamageType.Stun, 1.5f);
        set.snake = MakeEnemy("Enemy_Snake", "Serpiente", prefabs.snake, EnemyTimeSlot.Day, false, 24f, 1.8f, 7f, DamageType.Poison, 6f);
        set.coyote = MakeEnemy("Enemy_Coyote", "Coyote", prefabs.coyote, EnemyTimeSlot.Day, false, 20f, 3f, 9f, DamageType.Instant);

        AssetDatabase.SaveAssets();

        SetPrefabItem(prefabs.coin, set.coin);
        SetPrefabItem(prefabs.gem, set.gem);
        SetPrefabItem(prefabs.berry, set.berry);
        SetPrefabItem(prefabs.key, set.keys[0]);
        return set;
    }

    private static void SetPrefabItem(GameObject prefab, ItemData data)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        var contents = PrefabUtility.LoadPrefabContents(path);
        SetRef(contents.GetComponent<ItemPickup>(), "data", data);
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static Image Panel(Transform parent, string name, Color color)
    {
        var rect = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text Label(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 position, Vector2 box, TextAnchor align)
    {
        var rect = Rect(name, parent, anchor, position, box);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.fontStyle = FontStyle.Bold;
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.07f, 0.18f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        return text;
    }

    private static Text TitleLabel(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 position, Vector2 box)
    {
        var text = Label(parent, name, content, size, anchor, position, box, TextAnchor.MiddleCenter);
        text.color = new Color(1f, 0.82f, 0.25f);
        var outline = text.GetComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.27f, 0.1f);
        outline.effectDistance = new Vector2(6f, -6f);
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.08f, 0.07f, 0.18f, 0.6f);
        shadow.effectDistance = new Vector2(0f, -10f);
        return text;
    }

    private static Sprite UiSprite(string name, Vector4 border)
    {
        string path = Root + "/Art/UI/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Image SpritePanel(Transform parent, string name, string sprite, Vector4 border, Color tint)
    {
        var image = Panel(parent, name, tint);
        image.sprite = UiSprite(sprite, border);
        image.type = Image.Type.Sliced;
        return image;
    }

    private const float WideAspect = 3000f / 1152f;

    private static RectTransform ArtScreen(Transform parent, string sprite, float aspect, float backdropDim)
    {
        var backdrop = Panel(parent, "Backdrop", new Color(0.09f, 0.1f, 0.22f, 1f));
        backdrop.raycastTarget = false;

        var art = Rect("Art", parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        art.pivot = new Vector2(0.5f, 0.5f);
        Stretch(art);
        var fit = art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = aspect;
        var fitter = art.gameObject.AddComponent<ArtScreenFitter>();
        SetFloat(fitter, "artAspect", aspect);

        var wide = Rect("Picture", art, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        wide.pivot = new Vector2(0.5f, 0.5f);
        wide.anchorMin = new Vector2(0.5f, 0f);
        wide.anchorMax = new Vector2(0.5f, 1f);
        wide.offsetMin = Vector2.zero;
        wide.offsetMax = Vector2.zero;
        var image = wide.gameObject.AddComponent<Image>();
        image.sprite = UiSprite(sprite + "Wide", Vector4.zero);
        image.raycastTarget = false;
        var wideFit = wide.gameObject.AddComponent<AspectRatioFitter>();
        wideFit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        wideFit.aspectRatio = WideAspect;
        return art;
    }

    private static RectTransform Zone(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var rect = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Button ArtButton(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var rect = Zone(parent, name, x0, y0, x1, y1);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.22f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        button.colors = colors;
        return button;
    }

    private static Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = Rect(label + "Button", parent, anchor, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = UiSprite("WoodButton", new Vector4(28f, 28f, 28f, 28f));
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
        colors.pressedColor = new Color(0.72f, 0.62f, 0.55f);
        button.colors = colors;

        var text = Label(rect, "Label", label, 38, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        text.color = Bone;
        Stretch(text.rectTransform);
        return button;
    }

    private static Slider PauseRow(RectTransform parent, string label, float y)
    {
        var text = Label(parent, label + "Label", label, 24, new Vector2(0f, y), Vector2.zero, new Vector2(170f, 40f), TextAnchor.MiddleLeft);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(24f, 0f);

        var slider = MakeSlider(parent, label, new Vector2(1f, y), Vector2.zero, new Vector2(0f, 30f), true, Gold);
        var rect = slider.GetComponent<RectTransform>();
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchorMin = new Vector2(0f, y);
        rect.anchorMax = new Vector2(1f, y);
        rect.offsetMin = new Vector2(170f, -9f);
        rect.offsetMax = new Vector2(-24f, 9f);
        return slider;
    }

    private static Slider MakeSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, bool interactive, Color fillColor)
    {
        var rect = Rect(name, parent, anchor, position, size);
        var slider = rect.gameObject.AddComponent<Slider>();

        var background = Panel(rect, "Background", new Color(0.2f, 0.12f, 0.1f, 0.9f));

        var fillArea = Rect("Fill Area", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(fillArea, 4f);
        var fill = Rect("Fill", fillArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = fillColor;

        slider.fillRect = fill;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = interactive;
        slider.targetGraphic = fillImage;

        if (interactive)
        {
            var area = Rect("Handle Slide Area", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(area);
            area.offsetMin = new Vector2(16f, 0f);
            area.offsetMax = new Vector2(-16f, 0f);
            var handle = Rect("Handle", area, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 0f));
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
        }

        background.raycastTarget = interactive;
        return slider;
    }

    private static void AddEventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
        var moduleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        if (moduleType == null)
        {
            go.AddComponent<StandaloneInputModule>();
            return;
        }

        var module = go.AddComponent(moduleType);
        var actions = EnsureUiActions();
        if (actions == null) return;

        var property = moduleType.GetProperty("actionsAsset");
        if (property != null) property.SetValue(module, actions);
    }

    private static Object EnsureUiActions()
    {
        const string path = DataDir + "/UIActions.inputactions";
        var assetType = System.Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
        var defaultsType = System.Type.GetType("UnityEngine.InputSystem.DefaultInputActions, Unity.InputSystem");
        if (assetType == null || defaultsType == null) return null;

        var existing = AssetDatabase.LoadAssetAtPath(path, assetType);
        if (existing != null) return existing;

        var defaults = System.Activator.CreateInstance(defaultsType);
        var source = defaultsType.GetProperty("asset").GetValue(defaults);
        var json = (string)assetType.GetMethod("ToJson", System.Type.EmptyTypes).Invoke(source, null);
        (defaults as System.IDisposable)?.Dispose();

        System.IO.File.WriteAllText(path, json);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath(path, assetType);
    }


    private static void StyleCamera(Camera camera, Color background)
    {
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
    }

    private static Scene NewScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    }

    private static void Save(Scene scene, string name)
    {
        EditorSceneManager.SaveScene(scene, SceneDir + "/" + name + ".unity");
    }

    private static void BuildScenes(PrefabSet prefabs, DataSet data)
    {
        BuildMainMenu(prefabs);
        BuildLoading();
        BuildGame(prefabs, data);
        BuildGameOver();
    }


    private static void BuildMainMenu(PrefabSet prefabs)
    {
        var scene = NewScene();
        var camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = 3f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
        StyleCamera(camera, new Color(0.09f, 0.1f, 0.22f));

        DestroyLights();
        new GameObject("Persistence").AddComponent<DataPersistence>();

        var canvas = MakeCanvas("Canvas", 0);
        var art = ArtScreen(canvas.transform, "MenuNest", 572f / 1024f, 0.2f);
        TitleLabel(art, "Title", "RAVENEER", 112, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(700f, 160f));
        var record = Label(art, "Record", "Récord 00:00", 36, new Vector2(0.5f, 1f), new Vector2(0f, -228f), new Vector2(800f, 60f), TextAnchor.MiddleCenter);

        var play = MakeButton(art, "Jugar", new Vector2(0.5f, 0f), new Vector2(0f, 115f), new Vector2(400f, 86f));
        var quit = MakeButton(art, "Salir", new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(400f, 86f));

        var collectionHost = new GameObject("Collection");
        var collection = collectionHost.AddComponent<CollectionScreen>();
        SetRef(collection, "panelSprite", UiSprite("NightPanel", new Vector4(36f, 36f, 36f, 36f)));
        SetRef(collection, "buttonSprite", UiSprite("WoodButton", new Vector4(28f, 28f, 28f, 28f)));
        var albumButton = MakeButton(canvas.transform, "Álbum", new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(300f, 86f));
        albumButton.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
        var achievementsButton = MakeButton(canvas.transform, "Logros", new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 86f));
        achievementsButton.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
        UnityEventTools.AddPersistentListener(albumButton.onClick, collection.Open);
        UnityEventTools.AddPersistentListener(achievementsButton.onClick, collection.OpenAchievements);

        var controller = new GameObject("MenuController").AddComponent<MainMenuController>();
        SetRef(controller, "recordText", record);
        UnityEventTools.AddPersistentListener(play.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);

        AddEventSystem();
        Save(scene, "MainMenu");
    }


    private static void BuildLoading()
    {
        var scene = NewScene();
        var camera = Camera.main;
        StyleCamera(camera, new Color(0.09f, 0.1f, 0.22f));
        Object.DestroyImmediate(GameObject.Find("Directional Light"));

        var canvas = MakeCanvas("Canvas", 0);
        ArtScreen(canvas.transform, "MenuNest", 572f / 1024f, 0.2f);
        Panel(canvas.transform, "Dim", new Color(0.05f, 0.05f, 0.15f, 0.45f)).raycastTarget = false;
        TitleLabel(canvas.transform, "Title", "Cargando", 90, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(800f, 120f));
        var bar = MakeSlider(canvas.transform, "Progress", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(720f, 28f), false, Gold);
        bar.value = 0f;

        var controller = new GameObject("LoadingController").AddComponent<LoadingScreenController>();
        SetRef(controller, "progressBar", bar);

        AddEventSystem();
        Save(scene, "LoadingScene");
    }

    private static void BuildGameOver()
    {
        var scene = NewScene();
        var camera = Camera.main;
        StyleCamera(camera, new Color(0.09f, 0.1f, 0.22f));
        Object.DestroyImmediate(GameObject.Find("Directional Light"));

        new GameObject("Persistence").AddComponent<DataPersistence>();

        var canvas = MakeCanvas("Canvas", 0);
        ArtScreen(canvas.transform, "MenuNest", 572f / 1024f, 0.2f);
        Panel(canvas.transform, "Dim", new Color(0.05f, 0.05f, 0.15f, 0.6f)).raycastTarget = false;
        TitleLabel(canvas.transform, "Title", "Fin del vuelo", 110, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1400f, 150f));
        var reason = Label(canvas.transform, "Reason", "", 48, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 70f), TextAnchor.MiddleCenter);
        var time = Label(canvas.transform, "Time", "", 48, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1200f, 70f), TextAnchor.MiddleCenter);
        var record = Label(canvas.transform, "Record", "", 48, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1200f, 70f), TextAnchor.MiddleCenter);

        var retry = MakeButton(canvas.transform, "Reintentar", new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(480f, 100f));
        var menu = MakeButton(canvas.transform, "Menú", new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(480f, 100f));

        var controller = new GameObject("GameOverController").AddComponent<GameOverController>();
        SetRef(controller, "reasonText", reason);
        SetRef(controller, "timeText", time);
        SetRef(controller, "recordText", record);
        UnityEventTools.AddPersistentListener(retry.onClick, controller.Retry);
        UnityEventTools.AddPersistentListener(menu.onClick, controller.MainMenu);

        AddEventSystem();
        Save(scene, "GameOverScene");
    }


    private static void DestroyLights()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            Object.DestroyImmediate(light.gameObject);
    }

    private static GameObject Spawn(GameObject prefab, Transform parent, Vector2 position, float angle = 0f)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = new Vector3(position.x, position.y, 0f);
        instance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return instance;
    }

    private static Vector2 Ring(System.Random random, float min, float max)
    {
        double angle = random.NextDouble() * System.Math.PI * 2.0;
        float radius = min + (float)random.NextDouble() * (max - min);
        return new Vector2(Mathf.Cos((float)angle) * radius, Mathf.Sin((float)angle) * radius);
    }

    private static T Reload<T>(string name) where T : ScriptableObject
    {
        return AssetDatabase.LoadAssetAtPath<T>(DataDir + "/" + name + ".asset");
    }

    private static void ReloadData(DataSet data)
    {
        data.coin = Reload<ItemData>("Item_Coin");
        data.gem = Reload<ItemData>("Item_Gem");
        data.berry = Reload<ItemData>("Item_Berry");
        for (int i = 0; i < data.keys.Length; i++)
            data.keys[i] = Reload<ItemData>("Item_Key" + (i + 1));
        data.owl = Reload<EnemyStats>("Enemy_Owl");
        data.snake = Reload<EnemyStats>("Enemy_Snake");
        data.coyote = Reload<EnemyStats>("Enemy_Coyote");
    }

    private static void BuildGame(PrefabSet prefabs, DataSet data)
    {
        var scene = NewScene();
        ReloadData(data);
        DestroyLights();
        var random = new System.Random(7);

        var camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.transform.position = new Vector3(0f, -4f, -10f);
        camera.transform.rotation = Quaternion.identity;
        StyleCamera(camera, Grass);

        var world = new GameObject("World");
        var ground = new GameObject("Ground");
        ground.transform.SetParent(world.transform, false);
        var groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = groundSprite;
        groundRenderer.drawMode = SpriteDrawMode.Tiled;
        groundRenderer.size = new Vector2(140f, 140f);
        groundRenderer.sortingOrder = -100;

        for (int i = 0; i < 16; i++)
        {
            float size = 0.8f + (float)random.NextDouble() * 1.4f;
            var position = Ring(random, 8f, 58f);
            var rock = new GameObject("Rock" + i);
            rock.transform.SetParent(world.transform, false);
            rock.transform.position = new Vector3(position.x, position.y, 0f);
            Shape("Shadow", rock.transform, circleSprite, new Vector2(0.12f, -0.12f), new Vector2(size * 1.1f, size * 0.8f), Shadow, 3);
            Shape("Stone", rock.transform, circleSprite, Vector2.zero, new Vector2(size, size * 0.78f), Stone, 4);
            Shape("Light", rock.transform, circleSprite, new Vector2(-size * 0.15f, size * 0.12f), new Vector2(size * 0.45f, size * 0.32f), new Color(Stone.r * 1.1f, Stone.g * 1.1f, Stone.b * 1.1f), 5);
            Sticker(rock, 0.08f);
        }

        for (int i = 0; i < 40; i++)
        {
            var position = Ring(random, 14f, 62f);
            float crown = 2.6f + (float)random.NextDouble() * 1.4f;
            var tree = new GameObject("Tree" + i);
            tree.transform.SetParent(world.transform, false);
            tree.transform.position = new Vector3(position.x, position.y, 0f);
            Shape("Shadow", tree.transform, circleSprite, new Vector2(0.5f, -0.5f), Vector2.one * crown, Shadow, 38);
            Shape("Trunk", tree.transform, circleSprite, Vector2.zero, Vector2.one * 0.7f, Wood, 40);
            Shape("Crown", tree.transform, circleSprite, Vector2.zero, Vector2.one * crown, Leaf, 41);
            Shape("Highlight", tree.transform, circleSprite, new Vector2(-crown * 0.15f, crown * 0.15f), Vector2.one * crown * 0.55f, new Color(Leaf.r * 1.12f, Leaf.g * 1.12f, Leaf.b * 1.1f), 42);
            Sticker(tree, 0.1f);
        }

        var collectibles = new GameObject("Collectibles").transform;
        for (int i = 0; i < 14; i++)
            Spawn(i % 2 == 0 ? prefabs.coin : prefabs.gem, collectibles, Ring(random, 7f, 56f), (float)random.NextDouble() * 360f);

        for (int i = 0; i < 5; i++)
        {
            var key = Spawn(prefabs.key, collectibles, Ring(random, 30f, 58f), (float)random.NextDouble() * 360f);
            key.name = "Key_" + (i + 1);
            SetRef(key.GetComponent<ItemPickup>(), "data", data.keys[i]);
        }

        for (int i = 0; i < 10; i++)
            Spawn(prefabs.berry, collectibles, Ring(random, 6f, 52f));

        string[] notes =
        {
            "Las llaves están lejos del nido. Explora hacia los bordes.",
            "Búhos de noche. Serpientes y coyotes de día.",
            "Una baya a tiempo vale más que un tesoro."
        };
        for (int i = 0; i < notes.Length; i++)
        {
            var note = Spawn(prefabs.note, collectibles, Ring(random, 9f, 28f), (float)random.NextDouble() * 360f);
            SetString(note.GetComponent<NoteReadable>(), "noteText", notes[i]);
        }

        var nest = Spawn(prefabs.nest, null, Vector2.zero);
        var crow = Spawn(prefabs.crow, null, new Vector2(0f, -4f));
        var player = crow.GetComponent<PlayerController>();

        var enemies = new GameObject("Enemies");

        var follow = camera.gameObject.AddComponent<FollowCamera>();
        SetRef(follow, "target", crow.transform);

        var systems = new GameObject("Systems");
        new GameObject("Persistence").AddComponent<DataPersistence>();
        var manager = systems.AddComponent<GameManager>();
        var gestures = systems.AddComponent<GestureInputManager>();
        var spawner = systems.AddComponent<TimeOfDaySpawner>();
        var pause = systems.AddComponent<PauseMenu>();
        var toggle = systems.AddComponent<ARCameraToggle>();
        systems.AddComponent<DayNightCycle>();
        systems.AddComponent<DangerAlert>();
        systems.AddComponent<AmbientAudio>();
        var tracker = systems.AddComponent<AchievementTracker>();
        var collection = systems.AddComponent<CollectionScreen>();
        var namePrompt = systems.AddComponent<NamePrompt>();
        var nightPanelSprite = UiSprite("NightPanel", new Vector4(36f, 36f, 36f, 36f));
        var woodSprite = UiSprite("WoodButton", new Vector4(28f, 28f, 28f, 28f));
        SetRef(tracker, "panelSprite", nightPanelSprite);
        SetRef(collection, "panelSprite", nightPanelSprite);
        SetRef(collection, "buttonSprite", woodSprite);
        SetRef(namePrompt, "panelSprite", nightPanelSprite);
        SetRef(namePrompt, "buttonSprite", woodSprite);

        SetRef(toggle, "player", player);
        SetRef(toggle, "namePrompt", namePrompt);
        SetRef(toggle, "followCamera", follow);
        SetRefs(toggle, "hideDuringAR", new Object[] { world, collectibles.gameObject, enemies, crow.transform.GetChild(0).gameObject, nest });
        SetRefs(toggle, "pauseDuringAR", new Object[] { spawner });
        SetRef(toggle, "triangleSprite", triangleSprite);
        SetRef(toggle, "circleSprite", circleSprite);
        SetRef(toggle, "squareSprite", squareSprite);

        SetRef(gestures, "playerController", player);
        SetRef(gestures, "arToggle", toggle);
        SetRef(gestures, "inputCamera", camera);

        SetRef(spawner, "player", player);
        SetRef(spawner, "container", enemies.transform);
        SetRefs(spawner, "enemies", new Object[] { data.owl, data.snake, data.coyote });

        SetRef(manager, "player", player);

        var hud = MakeCanvas("HUD", 0);
        var health = MakeSlider(hud.transform, "Health", new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(520f, 36f), false, Berry);
        var hunger = MakeSlider(hud.transform, "Hunger", new Vector2(0f, 1f), new Vector2(40f, -90f), new Vector2(520f, 24f), false, Gold);
        var held = Label(hud.transform, "Held", "Pico vacío", 36, new Vector2(0f, 1f), new Vector2(40f, -140f), new Vector2(520f, 50f), TextAnchor.MiddleLeft);
        var clock = Label(hud.transform, "Clock", "00:00", 48, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(300f, 70f), TextAnchor.MiddleCenter);
        var nestCount = Label(hud.transform, "NestCount", "", 34, new Vector2(1f, 1f), new Vector2(-40f, -150f), new Vector2(560f, 50f), TextAnchor.MiddleRight);
        nestCount.rectTransform.pivot = new Vector2(1f, 1f);

        var pauseButton = MakeButton(hud.transform, "Pausa", new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(240f, 80f));
        pauseButton.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
        var arButton = MakeButton(hud.transform, "Cámara RA", new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 90f));
        arButton.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);

        var albumButton = MakeButton(hud.transform, "Colección", new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(300f, 90f));
        albumButton.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);

        var dropButton = MakeButton(hud.transform, "Soltar", new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(280f, 90f));
        dropButton.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
        var pickButton = MakeButton(hud.transform, "Recoger", new Vector2(0f, 0f), new Vector2(40f, 150f), new Vector2(280f, 90f));
        pickButton.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);
        var helpButton = MakeButton(hud.transform, "Controles", new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(260f, 70f));

        var controlsBar = Label(hud.transform, "ControlsBar", "Deslizar: volar   |   Doble toque: recoger   |   Mantener: soltar   |   Tocar nota: leer", 30, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1200f, 50f), TextAnchor.MiddleCenter);

        var intro = SpritePanel(hud.transform, "ControlsIntro", "NightPanel", new Vector4(36f, 36f, 36f, 36f), Color.white);
        intro.raycastTarget = false;
        var introRect = intro.rectTransform;
        introRect.anchorMin = introRect.anchorMax = new Vector2(0.5f, 0.5f);
        introRect.sizeDelta = new Vector2(1200f, 560f);
        introRect.anchoredPosition = new Vector2(0f, 40f);
        TitleLabel(intro.transform, "Title", "Controles", 72, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 100f));
        Label(intro.transform, "Body", "Desliza el dedo: vuela en esa dirección\nDoble toque o botón Recoger: toma un objeto cercano\nMantén presionado o botón Soltar: lanza el objeto\nToca una nota para leerla\nLleva comida al nido antes de que termine el día", 40, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1100f, 300f), TextAnchor.MiddleCenter);
        var introClose = MakeButton(intro.transform, "Entendido", new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 80f));

        var hint = hud.gameObject.AddComponent<ControlsHint>();
        SetRef(hint, "panel", intro.gameObject);

        var arIntro = SpritePanel(hud.transform, "ARControlsIntro", "NightPanel", new Vector4(36f, 36f, 36f, 36f), Color.white);
        var arIntroRect = arIntro.rectTransform;
        arIntroRect.anchorMin = arIntroRect.anchorMax = new Vector2(0.5f, 0.5f);
        arIntroRect.sizeDelta = new Vector2(1200f, 600f);
        arIntroRect.anchoredPosition = new Vector2(0f, 40f);
        TitleLabel(arIntro.transform, "Title", "Modo cámara", 72, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 100f));
        Label(arIntro.transform, "Body", "Mueve el celular para buscar tesoros a tu alrededor\nToca un tesoro para tomarle una foto\nPonle nombre y se guardará en tu álbum\nAquí no hay enemigos: el mundo queda en pausa\nApaga la cámara para volver a volar", 38, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1100f, 320f), TextAnchor.MiddleCenter);
        var arIntroClose = MakeButton(arIntro.transform, "Entendido", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 80f));
        var arHint = hud.gameObject.AddComponent<ControlsHint>();
        SetRef(arHint, "panel", arIntro.gameObject);
        arIntro.gameObject.SetActive(false);

        var notePanel = SpritePanel(hud.transform, "NotePanel", "NightPanel", new Vector4(36f, 36f, 36f, 36f), Color.white);
        var noteText = Label(notePanel.transform, "NoteText", "", 46, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1200f, 400f), TextAnchor.MiddleCenter);
        var noteClose = MakeButton(notePanel.transform, "Cerrar", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(360f, 90f));
        notePanel.gameObject.SetActive(false);

        var ui = hud.gameObject.AddComponent<UIManager>();
        SetRef(ui, "player", player);
        SetRef(ui, "healthBar", health);
        SetRef(ui, "hungerBar", hunger);
        SetRef(ui, "survivalTimeText", clock);
        SetRef(ui, "heldItemText", held);
        SetRef(ui, "nestCountText", nestCount);
        SetRef(ui, "notePanel", notePanel.gameObject);
        SetRef(ui, "noteText", noteText);

        var pauseCanvas = MakeCanvas("PauseMenu", 10);
        var pauseArt = ArtScreen(pauseCanvas.transform, "PauseScreen", 572f / 1024f, 0.35f);

        var settings = SpritePanel(pauseArt, "Settings", "NightPanel", new Vector4(36f, 36f, 36f, 36f), new Color(1f, 1f, 1f, 0.82f));
        var settingsRect = settings.rectTransform;
        settingsRect.anchorMin = new Vector2(0.06f, 0.015f);
        settingsRect.anchorMax = new Vector2(0.94f, 0.105f);
        settingsRect.offsetMin = Vector2.zero;
        settingsRect.offsetMax = Vector2.zero;

        var music = PauseRow(settingsRect, "Música", 0.78f);
        var sfx = PauseRow(settingsRect, "Efectos", 0.5f);
        var brightness = PauseRow(settingsRect, "Brillo", 0.22f);

        var resume = ArtButton(pauseArt, "Continuar", 0.27f, 0.209f, 0.731f, 0.29f);
        var restart = ArtButton(pauseArt, "Reiniciar", 0.16f, 0.119f, 0.525f, 0.191f);
        var mainMenu = ArtButton(pauseArt, "MenuPrincipal", 0.568f, 0.119f, 0.843f, 0.191f);

        SetRef(pause, "musicSlider", music);
        SetRef(pause, "sfxSlider", sfx);
        SetRef(pause, "brightnessSlider", brightness);
        SetRef(manager, "pauseMenuCanvas", pauseCanvas.gameObject);

        UnityEventTools.AddPersistentListener(music.onValueChanged, pause.SetMusic);
        UnityEventTools.AddPersistentListener(sfx.onValueChanged, pause.SetSfx);
        UnityEventTools.AddPersistentListener(brightness.onValueChanged, pause.SetBrightness);
        UnityEventTools.AddPersistentListener(resume.onClick, manager.TogglePause);
        UnityEventTools.AddPersistentListener(restart.onClick, manager.RestartLevel);
        UnityEventTools.AddPersistentListener(mainMenu.onClick, manager.ReturnToMainMenu);
        UnityEventTools.AddPersistentListener(pauseButton.onClick, manager.TogglePause);
        UnityEventTools.AddPersistentListener(arButton.onClick, toggle.Toggle);
        UnityEventTools.AddPersistentListener(albumButton.onClick, collection.Open);
        UnityEventTools.AddPersistentListener(pickButton.onClick, player.TryPickUpItem);
        UnityEventTools.AddPersistentListener(dropButton.onClick, player.TryThrowItem);
        UnityEventTools.AddPersistentListener(introClose.onClick, hint.Hide);
        UnityEventTools.AddPersistentListener(helpButton.onClick, toggle.ShowHelp);
        UnityEventTools.AddPersistentListener(arIntroClose.onClick, arHint.Hide);
        SetRef(toggle, "arHint", arHint);
        SetRef(toggle, "worldHint", hint);
        SetRef(toggle, "controlsBar", controlsBar);
        SetString(toggle, "worldBarText", controlsBar.text);
        SetString(toggle, "arBarText", "Mueve el celular: mirar   |   Toca un tesoro: tomar foto   |   Colección: álbum y logros");
        UnityEventTools.AddPersistentListener(noteClose.onClick, ui.HideNote);

        pauseCanvas.gameObject.SetActive(false);

        AddEventSystem();
        Save(scene, "GameScene");
    }
}
