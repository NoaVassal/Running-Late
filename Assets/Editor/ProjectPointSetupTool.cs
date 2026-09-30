#if UNITY_EDITOR

using RunningLate;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectPointSetupTool
{
    private const string PrefabFolder =
        "Assets/Prefabs";

    private const string CollectiblesFolder =
        "Assets/Prefabs/Collectibles";

    private const string PrefabPath =
        "Assets/Prefabs/Collectibles/ProjectPoint.prefab";

    [MenuItem(
        "Tools/Running Late/Create Project Point From Selected Image"
    )]
    public static void CreateProjectPoint()
    {
        Texture2D selectedTexture =
            Selection.activeObject as Texture2D;

        if (selectedTexture == null)
        {
            Debug.LogError(
                "Select the Project Point image first."
            );

            return;
        }

        string texturePath =
            AssetDatabase.GetAssetPath(
                selectedTexture
            );

        ConfigureTextureAsSprite(
            texturePath
        );

        Sprite sprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                texturePath
            );

        if (sprite == null)
        {
            Debug.LogError(
                "Could not load image as Sprite."
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

        EnsureScoreSystem(config);

        CreateFoldersIfNeeded();

        GameObject prefab =
            CreateProjectPointPrefab(
                sprite
            );

        PlaceTestProjectPoint(
            prefab
        );

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene()
        );

        Debug.Log(
            "Running Late: Project Point created successfully."
        );
    }

    private static void ConfigureTextureAsSprite(
        string path
    )
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path)
            as TextureImporter;

        if (importer == null)
            return;

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
            return null;

        string path =
            AssetDatabase.GUIDToAssetPath(
                guids[0]
            );

        return AssetDatabase
            .LoadAssetAtPath<GameConfig>(
                path
            );
    }

    private static void EnsureScoreSystem(
        GameConfig config
    )
    {
        ScoreSystem scoreSystem =
            Object.FindFirstObjectByType<ScoreSystem>();

        if (scoreSystem == null)
        {
            GameObject obj =
                new GameObject(
                    "ScoreSystem"
                );

            scoreSystem =
                obj.AddComponent<ScoreSystem>();
        }

        SerializedObject serialized =
            new SerializedObject(
                scoreSystem
            );

        SerializedProperty property =
            serialized.FindProperty(
                "config"
            );

        property.objectReferenceValue =
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

    private static GameObject CreateProjectPointPrefab(
        Sprite sprite
    )
    {
        GameObject point =
            new GameObject(
                "ProjectPoint"
            );

        SpriteRenderer renderer =
            point.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;

        SphereCollider collider =
            point.AddComponent<SphereCollider>();

        collider.isTrigger = true;
        collider.radius = 4.5f;

        point.AddComponent<
            ProjectPointCollectible
        >();

        point.transform.localScale =
            Vector3.one * 0.035f;

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(
                point,
                PrefabPath
            );

        Object.DestroyImmediate(
            point
        );

        return prefab;
    }

    private static void PlaceTestProjectPoint(
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

        Transform old =
            segment.transform.Find(
                "ProjectPoint_Test"
            );

        if (old != null)
        {
            Object.DestroyImmediate(
                old.gameObject
            );
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                segment.transform
            ) as GameObject;

        if (instance == null)
            return;

        instance.name =
            "ProjectPoint_Test";

        instance.transform.localPosition =
            new Vector3(
                0f,
                1.1f,
                5f
            );

        instance.transform.localScale =
            Vector3.one * 0.035f;

        Selection.activeGameObject =
            instance;
    }
}

#endif