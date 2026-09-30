#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RunningLate.EditorTools
{
    public static class RunningLateSceneBuilder
    {
        private const float SegmentLength = 30f;

        [MenuItem("Tools/Running Late/Build Prototype Track")]
        public static void BuildPrototypeTrack()
        {
            GameConfig config = FindGameConfig();

            if (config == null)
            {
                Debug.LogError(
                    "Running Late: Could not find GameConfig.asset."
                );

                return;
            }

            GameObject player = FindRootObject("Player");

            if (player == null)
            {
                Debug.LogError(
                    "Running Late: Could not find the Player in the scene."
                );

                return;
            }

            int groundLayer = LayerMask.NameToLayer("Ground");

            if (groundLayer == -1)
            {
                Debug.LogError(
                    "Running Late: Layer named 'Ground' does not exist."
                );

                return;
            }

            // The old temporary ground is no longer needed.
            GameObject oldGround = FindRootObject("Ground");

            if (oldGround != null && oldGround.activeSelf)
            {
                Undo.RecordObject(oldGround, "Disable old Ground");
                oldGround.SetActive(false);
            }

            // Important for collectibles later.
            if (player.tag != "Player")
            {
                Undo.RecordObject(player, "Set Player Tag");

                try
                {
                    player.tag = "Player";
                }
                catch
                {
                    Debug.LogWarning(
                        "Running Late: Could not assign the Player tag."
                    );
                }
            }

            TrackSegment[] segments = new TrackSegment[3];

            for (int i = 0; i < 3; i++)
            {
                string segmentName =
                    "TrackSegment_0" + (i + 1);

                Vector3 segmentPosition =
                    new Vector3(
                        0f,
                        0f,
                        i * SegmentLength
                    );

                GameObject segment =
                    CreateOrUpdateSegment(
                        segmentName,
                        segmentPosition,
                        groundLayer,
                        config
                    );

                segments[i] =
                    segment.GetComponent<TrackSegment>();
            }

            TrackManager trackManager =
                FindOrCreateTrackManager();

            ConfigureTrackManager(
                trackManager,
                config,
                player.transform,
                segments
            );

            Selection.activeGameObject =
                segments[0].gameObject;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene()
            );

            Debug.Log(
                "Running Late: Prototype track created successfully. " +
                "Layout: Left Train | Boulevard | Right Train."
            );
        }

        private static GameObject CreateOrUpdateSegment(
            string segmentName,
            Vector3 position,
            int groundLayer,
            GameConfig config
        )
        {
            GameObject segment =
                FindRootObject(segmentName);

            if (segment == null)
            {
                segment = new GameObject(segmentName);

                Undo.RegisterCreatedObjectUndo(
                    segment,
                    "Create " + segmentName
                );
            }

            Undo.RecordObject(
                segment.transform,
                "Configure " + segmentName
            );

            segment.transform.position = position;
            segment.transform.rotation = Quaternion.identity;
            segment.transform.localScale = Vector3.one;

            CreateOrUpdateCube(
                segment.transform,
                "Boulevard",
                new Vector3(
                    0f,
                    -0.1f,
                    0f
                ),
                new Vector3(
                    2.3f,
                    0.2f,
                    SegmentLength
                ),
                groundLayer
            );

            CreateOrUpdateCube(
                segment.transform,
                "LeftTrain",
                new Vector3(
                    -2.5f,
                    0.6f,
                    0f
                ),
                new Vector3(
                    2.2f,
                    1.2f,
                    SegmentLength
                ),
                groundLayer
            );

            CreateOrUpdateCube(
                segment.transform,
                "RightTrain",
                new Vector3(
                    2.5f,
                    0.6f,
                    0f
                ),
                new Vector3(
                    2.2f,
                    1.2f,
                    SegmentLength
                ),
                groundLayer
            );

            TrackSegment trackSegment =
                segment.GetComponent<TrackSegment>();

            if (trackSegment == null)
            {
                trackSegment =
                    Undo.AddComponent<TrackSegment>(
                        segment
                    );
            }

            SerializedObject serializedSegment =
                new SerializedObject(trackSegment);

            SerializedProperty configProperty =
                serializedSegment.FindProperty("config");

            if (configProperty != null)
            {
                configProperty.objectReferenceValue =
                    config;
            }

            serializedSegment.ApplyModifiedProperties();

            return segment;
        }

        private static void CreateOrUpdateCube(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            int groundLayer
        )
        {
            Transform child =
                FindDirectChild(
                    parent,
                    objectName
                );

            GameObject cube;

            if (child == null)
            {
                cube =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube
                    );

                cube.name = objectName;

                Undo.RegisterCreatedObjectUndo(
                    cube,
                    "Create " + objectName
                );

                Undo.SetTransformParent(
                    cube.transform,
                    parent,
                    "Parent " + objectName
                );
            }
            else
            {
                cube = child.gameObject;

                Undo.RecordObject(
                    cube.transform,
                    "Update " + objectName
                );
            }

            cube.transform.localPosition =
                localPosition;

            cube.transform.localRotation =
                Quaternion.identity;

            cube.transform.localScale =
                localScale;

            cube.layer = groundLayer;

            // A Cube created by Unity already has a BoxCollider.
            // Keep it so the player can run and land on it.

            BoxCollider collider =
                cube.GetComponent<BoxCollider>();

            if (collider == null)
            {
                Undo.AddComponent<BoxCollider>(
                    cube
                );
            }
        }

        private static TrackManager FindOrCreateTrackManager()
        {
            TrackManager existing =
                Object.FindFirstObjectByType<TrackManager>();

            if (existing != null)
            {
                return existing;
            }

            GameObject managerObject =
                new GameObject("TrackManager");

            Undo.RegisterCreatedObjectUndo(
                managerObject,
                "Create TrackManager"
            );

            return Undo.AddComponent<TrackManager>(
                managerObject
            );
        }

        private static void ConfigureTrackManager(
            TrackManager manager,
            GameConfig config,
            Transform player,
            TrackSegment[] segments
        )
        {
            SerializedObject serializedManager =
                new SerializedObject(manager);

            SerializedProperty configProperty =
                serializedManager.FindProperty("config");

            SerializedProperty playerProperty =
                serializedManager.FindProperty("player");

            SerializedProperty segmentsProperty =
                serializedManager.FindProperty(
                    "trackSegments"
                );

            configProperty.objectReferenceValue =
                config;

            playerProperty.objectReferenceValue =
                player;

            segmentsProperty.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                segmentsProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue =
                    segments[i];
            }

            serializedManager.ApplyModifiedProperties();

            manager.enabled = true;
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

            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]
                );

            return AssetDatabase.LoadAssetAtPath<GameConfig>(
                assetPath
            );
        }

        private static GameObject FindRootObject(
            string objectName
        )
        {
            Scene scene =
                SceneManager.GetActiveScene();

            GameObject[] roots =
                scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName
        )
        {
            for (int i = 0;
                 i < parent.childCount;
                 i++)
            {
                Transform child =
                    parent.GetChild(i);

                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}

#endif