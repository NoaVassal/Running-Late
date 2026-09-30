#if UNITY_EDITOR

using RunningLate;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BatteryPickupSetupTool
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string CollectiblesFolder =
        "Assets/Prefabs/Collectibles";

    private const string PrefabPath =
        "Assets/Prefabs/Collectibles/BatteryPickup.prefab";

    [MenuItem(
        "Tools/Running Late/Create Battery Pickup From Selected Image"
    )]
    public static void CreateBatteryPickup()
    {
        Texture2D selectedTexture =
            Selection.activeObject as Texture2D;

        if (selectedTexture == null)
        {
            Debug.LogError(
                "Select the Battery image in the Project window first."
            );

            return;
        }

        string texturePath =
            AssetDatabase.GetAssetPath(selectedTexture);

        ConfigureTextureAsSprite(texturePath);

        Sprite batterySprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                texturePath
            );

        if (batterySprite == null)
        {
            Debug.LogError(
                "Could not load the selected image as a Sprite."
            );

            return;
        }

        GameConfig config =
            FindGameConfig();

        if (config == null)
        {
            Debug.LogError(
                "Could not find GameConfig."
            );

            return;
        }

        GameObject player =
            GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogError(
                "Could not find Player in the scene."
            );

            return;
        }

        EnsurePlayerTag(player);

        EnsureBatterySystem(config);

        CreateFoldersIfNeeded();

        GameObject prefab =
            CreateBatteryPrefab(
                batterySprite
            );

        PlaceTestBattery(prefab);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene()
        );

        Debug.Log(
            "Running Late: Battery Pickup created successfully."
        );
    }

    private static void ConfigureTextureAsSprite(
        string texturePath
    )
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath)
            as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;

        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        importer.SaveAndReimport();
    }

    private static GameConfig FindGameConfig()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameConfig"
            );

        if (guids.Length == 0)
        {
            return null;
        }

        string path =
            AssetDatabase.GUIDToAssetPath(
                guids[0]
            );

        return AssetDatabase
            .LoadAssetAtPath<GameConfig>(
                path
            );
    }

    private static void EnsurePlayerTag(
        GameObject player
    )
    {
        try
        {
            player.tag = "Player";
        }
        catch
        {
            Debug.LogError(
                "The Player tag does not exist. " +
                "Create a tag named Player."
            );
        }
    }

    private static void EnsureBatterySystem(
        GameConfig config
    )
    {
        BatterySystem existing =
            Object.FindFirstObjectByType<BatterySystem>();

        if (existing != null)
        {
            AssignConfig(
                existing,
                config
            );

            return;
        }

        GameObject systemObject =
            new GameObject("BatterySystem");

        BatterySystem batterySystem =
            systemObject.AddComponent<BatterySystem>();

        AssignConfig(
            batterySystem,
            config
        );
    }

    private static void AssignConfig(
        BatterySystem batterySystem,
        GameConfig config
    )
    {
        SerializedObject serialized =
            new SerializedObject(
                batterySystem
            );

        SerializedProperty configProperty =
            serialized.FindProperty(
                "config"
            );

        configProperty.objectReferenceValue =
            config;

        serialized.ApplyModifiedProperties();
    }

    private static void CreateFoldersIfNeeded()
    {
        if (!AssetDatabase.IsValidFolder(
                PrefabFolder
            ))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "Prefabs"
            );
        }

        if (!AssetDatabase.IsValidFolder(
                CollectiblesFolder
            ))
        {
            AssetDatabase.CreateFolder(
                PrefabFolder,
                "Collectibles"
            );
        }
    }

    private static GameObject CreateBatteryPrefab(
        Sprite sprite
    )
    {
        GameObject battery =
            new GameObject("BatteryPickup");

        SpriteRenderer renderer =
            battery.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;

        SphereCollider collider =
            battery.AddComponent<SphereCollider>();

        collider.isTrigger = true;
        collider.radius = 0.65f;

        battery.AddComponent<BatteryPickup>();

        battery.transform.localScale =
            Vector3.one * 0.85f;

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(
                battery,
                PrefabPath
            );

        Object.DestroyImmediate(battery);

        return prefab;
    }

    private static void PlaceTestBattery(
        GameObject prefab
    )
    {
        GameObject segment =
            GameObject.Find(
                "TrackSegment_01"
            );

        if (segment == null)
        {
            Debug.LogError(
                "TrackSegment_01 was not found."
            );

            return;
        }

        Transform existing =
            segment.transform.Find(
                "BatteryPickup_Test"
            );

        if (existing != null)
        {
            Object.DestroyImmediate(
                existing.gameObject
            );
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                segment.transform
            ) as GameObject;

        if (instance == null)
        {
            return;
        }

        instance.name =
            "BatteryPickup_Test";

        instance.transform.localPosition =
            new Vector3(
                0f,
                1.1f,
                8f
            );

        instance.transform.localRotation =
            Quaternion.identity;

        instance.transform.localScale =
            Vector3.one * 0.85f;

        Selection.activeGameObject =
            instance;
    }
}

#endif