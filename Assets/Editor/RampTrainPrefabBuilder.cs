using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RunningLate.EditorTools
{
    public static class RampTrainPrefabBuilder
    {
        private const string OutputFolder =
            "Assets/Prefabs/Trains";

        private const string GrayMaterialPath =
            "Assets/Prefabs/Trains/Ramp_Gray.mat";

        private const string RedMaterialPath =
            "Assets/Prefabs/Trains/Ramp_Red.mat";

        private const string BlackMaterialPath =
            "Assets/Prefabs/Trains/Ramp_Black.mat";

        private const float TrainWidth =
            2.1f;

        private const float TrainHeight =
            2.25f;

        private const float TrainLength =
            21f;

        private const float RampLength =
            8f;

        private const float RampIntoTrainOverlap =
            1.2f;

        private const int RampSegments =
            18;

        private const float DeckThickness =
            0.10f;

        // ==================================================
        // MENU
        // ==================================================

        [MenuItem(
            "Tools/Running Late/Build Train Prefabs From Selected Model"
        )]
        public static void BuildTrainPrefabs()
        {
            GameObject selectedPrefab =
                Selection.activeObject as GameObject;

            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Running Late",
                    "Select the ORIGINAL 1995_stock_train prefab in the Project window first.",
                    "OK"
                );

                return;
            }

            string sourcePath =
                AssetDatabase.GetAssetPath(
                    selectedPrefab
                );

            if (string.IsNullOrEmpty(
                    sourcePath
                ) ||
                PrefabUtility.GetPrefabAssetType(
                    selectedPrefab
                ) ==
                PrefabAssetType.NotAPrefab)
            {
                EditorUtility.DisplayDialog(
                    "Running Late",
                    "The selected object must be the original train prefab asset.",
                    "OK"
                );

                return;
            }

            EnsureOutputFolder();

            Shader shader =
                FindBestTrainShader(
                    selectedPrefab
                );

            Material grayMaterial =
                CreateOrUpdateMaterial(
                    GrayMaterialPath,
                    "Ramp_Gray",
            new Color(
    0.72f,
    0.74f,
    0.78f,
    1f
),
                    shader
                );

            Material redMaterial =
                CreateOrUpdateMaterial(
                    RedMaterialPath,
                    "Ramp_Red",
                    new Color(
                        0.88f,
                        0.10f,
                        0.10f,
                        1f
                    ),
                    shader
                );

            Material blackMaterial =
                CreateOrUpdateMaterial(
                    BlackMaterialPath,
                    "Ramp_Black",
                    new Color(
                        0.06f,
                        0.06f,
                        0.07f,
                        1f
                    ),
                    shader
                );

            GameObject normalPrefab =
                BuildNormalTrainPrefab(
                    selectedPrefab
                );

            GameObject rampPrefab =
                BuildRampTrainPrefab(
                    selectedPrefab,
                    grayMaterial,
                    redMaterial,
                    blackMaterial
                );

            AssignPrefabsToOpenScene(
                normalPrefab,
                rampPrefab
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject =
                rampPrefab;

            EditorGUIUtility.PingObject(
                rampPrefab
            );

            EditorUtility.DisplayDialog(
                "Running Late",
                "Done.\n\n" +
                "RampTrain was rebuilt using ONLY persistent primitive objects.\n" +
                "The gray deck, red edges and black center stripe must now be visible in the saved prefab.",
                "Great"
            );
        }

        [MenuItem(
            "Tools/Running Late/Build Train Prefabs From Selected Model",
            true
        )]
        private static bool ValidateBuildTrainPrefabs()
        {
            return
                Selection.activeObject is GameObject;
        }

        // ==================================================
        // NORMAL TRAIN
        // ==================================================

        private static GameObject BuildNormalTrainPrefab(
            GameObject sourcePrefab
        )
        {
            GameObject root =
                new GameObject(
                    "NormalTrain"
                );

            GameObject trainVisual =
                PrefabUtility.InstantiatePrefab(
                    sourcePrefab
                ) as GameObject;

            trainVisual.name =
                "TrainVisual";

            trainVisual.transform.SetParent(
                root.transform,
                false
            );

            PrepareSourceVisual(
                trainVisual
            );

            FitSourceVisual(
                root.transform,
                trainVisual.transform,
                TrainWidth,
                TrainHeight,
                TrainLength,
                0f
            );

            string path =
                OutputFolder +
                "/NormalTrain.prefab";

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    path
                );

            Object.DestroyImmediate(
                root
            );

            return
                prefab;
        }

        // ==================================================
        // RAMP TRAIN
        // ==================================================

        private static GameObject BuildRampTrainPrefab(
            GameObject sourcePrefab,
            Material grayMaterial,
            Material redMaterial,
            Material blackMaterial
        )
        {
            GameObject root =
                new GameObject(
                    "RampTrain"
                );

            GameObject trainVisual =
                PrefabUtility.InstantiatePrefab(
                    sourcePrefab
                ) as GameObject;

            trainVisual.name =
                "TrainVisual";

            trainVisual.transform.SetParent(
                root.transform,
                false
            );

            PrepareSourceVisual(
                trainVisual
            );

            // Body starts before the ramp ends.
            // This overlap hides the seam and makes it read as one train.
            float bodyStartZ =
                -TrainLength / 2f +
                RampLength -
                RampIntoTrainOverlap;

            float bodyEndZ =
                TrainLength / 2f;

            float bodyLength =
                bodyEndZ -
                bodyStartZ;

            float bodyCenterZ =
                (
                    bodyStartZ +
                    bodyEndZ
                ) /
                2f;

            FitSourceVisual(
                root.transform,
                trainVisual.transform,
                TrainWidth,
                TrainHeight,
                bodyLength,
                bodyCenterZ
            );

            CreateSegmentedRamp(
                root.transform,
                grayMaterial,
                redMaterial,
                blackMaterial
            );

            string path =
                OutputFolder +
                "/RampTrain.prefab";

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    path
                );

            Object.DestroyImmediate(
                root
            );

            return
                prefab;
        }

        // ==================================================
        // SEGMENTED RAMP
        // ==================================================

        private static void CreateSegmentedRamp(
            Transform parent,
            Material grayMaterial,
            Material redMaterial,
            Material blackMaterial
        )
        {
            GameObject rampRoot =
                new GameObject(
                    "INTEGRATED_RAMP"
                );

            rampRoot.transform.SetParent(
                parent,
                false
            );

            float frontZ =
                -TrainLength /
                2f;

            float segmentZLength =
                RampLength /
                RampSegments;

            for (int i = 0;
                 i < RampSegments;
                 i++)
            {
                float t0 =
                    i /
                    (float)RampSegments;

                float t1 =
                    (
                        i +
                        1
                    ) /
                    (float)RampSegments;

                float z0 =
                    frontZ +
                    RampLength *
                    t0;

                float z1 =
                    frontZ +
                    RampLength *
                    t1;

                float y0 =
                    TrainHeight *
                    SmoothRamp(
                        t0
                    );

                float y1 =
                    TrainHeight *
                    SmoothRamp(
                        t1
                    );

                float width0 =
                    Mathf.Lerp(
                        TrainWidth *
                        0.78f,
                        TrainWidth,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(
                                t0 /
                                0.55f
                            )
                        )
                    );

                float width1 =
                    Mathf.Lerp(
                        TrainWidth *
                        0.78f,
                        TrainWidth,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(
                                t1 /
                                0.55f
                            )
                        )
                    );

                float width =
                    (
                        width0 +
                        width1
                    ) /
                    2f;

                Vector3 start =
                    new Vector3(
                        0f,
                        y0,
                        z0
                    );

                Vector3 end =
                    new Vector3(
                        0f,
                        y1,
                        z1
                    );

                Vector3 direction =
                    end -
                    start;

                float segmentLength =
                    direction.magnitude;

                Vector3 center =
                    (
                        start +
                        end
                    ) /
                    2f;

                Quaternion rotation =
                    Quaternion.FromToRotation(
                        Vector3.forward,
                        direction.normalized
                    );

                // ------------------------------------------
                // GRAY DECK
                // ------------------------------------------

                GameObject grayDeck =
                    CreateVisualCube(
                        "GrayDeck_" +
                        i,
                        rampRoot.transform,
                        grayMaterial
                    );

                grayDeck.transform.localPosition =
                    center +
                    rotation *
                    new Vector3(
                        0f,
                        DeckThickness *
                        0.55f,
                        0f
                    );

                grayDeck.transform.localRotation =
                    rotation;

                grayDeck.transform.localScale =
                    new Vector3(
                        width -
                        0.18f,
                        DeckThickness,
                        segmentLength +
                        0.025f
                    );

                // ------------------------------------------
                // RED LEFT / RIGHT EDGE
                // ------------------------------------------

                float edgeX =
                    width /
                    2f -
                    0.055f;

                GameObject blackLeft =
                    CreateVisualCube(
                        "BlackEdge_Left_" +
                        i,
                        rampRoot.transform,
                        blackMaterial
                    );

                blackLeft.transform.localPosition =
                    center +
                    rotation *
                    new Vector3(
                        -edgeX,
                        DeckThickness *
                        0.95f,
                        0f
                    );

                blackLeft.transform.localRotation =
                    rotation;

                blackLeft.transform.localScale =
                    new Vector3(
                        0.10f,
                        0.045f,
                        segmentLength +
                        0.03f
                    );

                GameObject blackRight =
                    CreateVisualCube(
                        "BlackEdge_Right_" +
                        i,
                        rampRoot.transform,
                        blackMaterial
                    );

                blackRight.transform.localPosition =
                    center +
                    rotation *
                    new Vector3(
                        edgeX,
                        DeckThickness *
                        0.95f,
                        0f
                    );

                blackRight.transform.localRotation =
                    rotation;

                blackRight.transform.localScale =
                    new Vector3(
                        0.10f,
                        0.045f,
                        segmentLength +
                        0.03f
                    );

                // ------------------------------------------
                // RED CENTER STRIPE
                // ------------------------------------------

                GameObject redStripe =
                    CreateVisualCube(
                        "RedCenter_" +
                        i,
                        rampRoot.transform,
                        redMaterial
                    );

                redStripe.transform.localPosition =
                    center +
                    rotation *
                    new Vector3(
                        0f,
                        DeckThickness *
                        1.02f,
                        0f
                    );

                redStripe.transform.localRotation =
                    rotation;

                redStripe.transform.localScale =
                    new Vector3(
                        0.12f,
                        0.025f,
                        segmentLength +
                        0.035f
                    );
            }

            CreateRoofBlend(
                rampRoot.transform,
                grayMaterial,
                redMaterial,
                blackMaterial
            );
        }

        private static float SmoothRamp(
            float t
        )
        {
            return
                t *
                t *
                (
                    3f -
                    2f *
                    t
                );
        }

        // ==================================================
        // ROOF BLEND
        // ==================================================

        private static void CreateRoofBlend(
            Transform parent,
            Material grayMaterial,
            Material redMaterial,
            Material blackMaterial
        )
        {
            float rampEndZ =
                -TrainLength /
                2f +
                RampLength;

            float blendLength =
                RampIntoTrainOverlap;

            GameObject grayBlend =
                CreateVisualCube(
                    "GrayRoofBlend",
                    parent,
                    grayMaterial
                );

            grayBlend.transform.localPosition =
                new Vector3(
                    0f,
                    TrainHeight +
                    0.05f,
                    rampEndZ -
                    blendLength /
                    2f
                );

            grayBlend.transform.localRotation =
                Quaternion.identity;

            grayBlend.transform.localScale =
                new Vector3(
                    TrainWidth -
                    0.18f,
                    DeckThickness,
                    blendLength
                );

            float edgeX =
                TrainWidth /
                2f -
                0.055f;

            GameObject blackLeft =
                CreateVisualCube(
                    "BlackRoofBlend_Left",
                    parent,
                    blackMaterial
                );

            blackLeft.transform.localPosition =
                new Vector3(
                    -edgeX,
                    TrainHeight +
                    0.095f,
                    rampEndZ -
                    blendLength /
                    2f
                );

            blackLeft.transform.localScale =
                new Vector3(
                    0.10f,
                    0.045f,
                    blendLength
                );

            GameObject blackRight =
                CreateVisualCube(
                    "BlackRoofBlend_Right",
                    parent,
                    blackMaterial
                );

            blackRight.transform.localPosition =
                new Vector3(
                    edgeX,
                    TrainHeight +
                    0.095f,
                    rampEndZ -
                    blendLength /
                    2f
                );

            blackRight.transform.localScale =
                new Vector3(
                    0.10f,
                    0.045f,
                    blendLength
                );

            GameObject redBlend =
                CreateVisualCube(
                    "RedRoofBlend",
                    parent,
                    redMaterial
                );

            redBlend.transform.localPosition =
                new Vector3(
                    0f,
                    TrainHeight +
                    0.102f,
                    rampEndZ -
                    blendLength /
                    2f
                );

            redBlend.transform.localScale =
                new Vector3(
                    0.12f,
                    0.025f,
                    blendLength
                );
        }

        // ==================================================
        // CUBE HELPER
        // ==================================================

        private static GameObject CreateVisualCube(
            string objectName,
            Transform parent,
            Material material
        )
        {
            GameObject cube =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            cube.name =
                objectName;

            cube.transform.SetParent(
                parent,
                false
            );

            Collider collider =
                cube.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(
                    collider
                );
            }

            Renderer renderer =
                cube.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    material;
            }

            return
                cube;
        }

        // ==================================================
        // MATERIALS
        // ==================================================

        private static Material CreateOrUpdateMaterial(
            string path,
            string materialName,
            Color color,
            Shader shader
        )
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path
                );

            if (material == null)
            {
                if (shader == null)
                {
                    shader =
                        Shader.Find(
                            "Universal Render Pipeline/Lit"
                        );
                }

                if (shader == null)
                {
                    shader =
                        Shader.Find(
                            "Standard"
                        );
                }

                material =
                    new Material(
                        shader
                    );

                material.name =
                    materialName;

                AssetDatabase.CreateAsset(
                    material,
                    path
                );
            }

            material.color =
                color;

            if (material.HasProperty(
                    "_BaseColor"
                ))
            {
                material.SetColor(
                    "_BaseColor",
                    color
                );
            }

            if (material.HasProperty(
                    "_Color"
                ))
            {
                material.SetColor(
                    "_Color",
                    color
                );
            }

            if (material.HasProperty(
                    "_EmissionColor"
                ))
            {
                material.SetColor(
                    "_EmissionColor",
                    Color.black
                );
            }

            if (material.HasProperty(
                    "_Metallic"
                ))
            {
                material.SetFloat(
                    "_Metallic",
                    0.05f
                );
            }

            if (material.HasProperty(
                    "_Smoothness"
                ))
            {
                material.SetFloat(
                    "_Smoothness",
                    0.22f
                );
            }

            EditorUtility.SetDirty(
                material
            );

            return
                material;
        }

        private static Shader FindBestTrainShader(
            GameObject sourcePrefab
        )
        {
            Renderer[] renderers =
                sourcePrefab.GetComponentsInChildren<Renderer>(
                    true
                );

            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                Material[] materials =
                    renderers[i].sharedMaterials;

                for (int j = 0;
                     j < materials.Length;
                     j++)
                {
                    Material material =
                        materials[j];

                    if (material != null &&
                        material.shader != null)
                    {
                        return
                            material.shader;
                    }
                }
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard"
                    );
            }

            return
                shader;
        }

        // ==================================================
        // SOURCE TRAIN
        // ==================================================

        private static void PrepareSourceVisual(
            GameObject source
        )
        {
            Collider[] colliders =
                source.GetComponentsInChildren<Collider>(
                    true
                );

            for (int i = 0;
                 i < colliders.Length;
                 i++)
            {
                Object.DestroyImmediate(
                    colliders[i]
                );
            }

            Rigidbody[] rigidbodies =
                source.GetComponentsInChildren<Rigidbody>(
                    true
                );

            for (int i = 0;
                 i < rigidbodies.Length;
                 i++)
            {
                Object.DestroyImmediate(
                    rigidbodies[i]
                );
            }
        }

        private static void FitSourceVisual(
            Transform root,
            Transform visual,
            float targetWidth,
            float targetHeight,
            float targetLength,
            float targetCenterZ
        )
        {
            visual.localPosition =
                Vector3.zero;

            visual.localRotation =
                Quaternion.identity;

            visual.localScale =
                Vector3.one;

            Bounds firstBounds;

            if (!TryGetRendererBoundsRelativeTo(
                    visual,
                    root,
                    out firstBounds
                ))
            {
                return;
            }

            if (firstBounds.size.x >
                firstBounds.size.z)
            {
                visual.localRotation =
                    Quaternion.Euler(
                        0f,
                        90f,
                        0f
                    );
            }

            Bounds rotatedBounds;

            if (!TryGetRendererBoundsRelativeTo(
                    visual,
                    root,
                    out rotatedBounds
                ))
            {
                return;
            }

            float scaleX =
                SafeScale(
                    targetWidth,
                    rotatedBounds.size.x
                );

            float scaleY =
                SafeScale(
                    targetHeight,
                    rotatedBounds.size.y
                );

            float scaleZ =
                SafeScale(
                    targetLength,
                    rotatedBounds.size.z
                );

            visual.localScale =
                new Vector3(
                    scaleX,
                    scaleY,
                    scaleZ
                );

            Bounds fittedBounds;

            if (!TryGetRendererBoundsRelativeTo(
                    visual,
                    root,
                    out fittedBounds
                ))
            {
                return;
            }

            visual.localPosition =
                new Vector3(
                    -fittedBounds.center.x,
                    -fittedBounds.min.y,
                    targetCenterZ -
                    fittedBounds.center.z
                );
        }

        // ==================================================
        // BOUNDS
        // ==================================================

        private static float SafeScale(
            float target,
            float current
        )
        {
            if (current <= 0.0001f)
            {
                return 1f;
            }

            return
                target /
                current;
        }

        private static bool TryGetRendererBoundsRelativeTo(
            Transform visualRoot,
            Transform reference,
            out Bounds combinedBounds
        )
        {
            Renderer[] renderers =
                visualRoot.GetComponentsInChildren<Renderer>(
                    true
                );

            bool hasBounds =
                false;

            combinedBounds =
                new Bounds();

            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                Renderer renderer =
                    renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                Bounds localBounds =
                    renderer.localBounds;

                Vector3 center =
                    localBounds.center;

                Vector3 extents =
                    localBounds.extents;

                for (int x = -1;
                     x <= 1;
                     x += 2)
                {
                    for (int y = -1;
                         y <= 1;
                         y += 2)
                    {
                        for (int z = -1;
                             z <= 1;
                             z += 2)
                        {
                            Vector3 localCorner =
                                center +
                                Vector3.Scale(
                                    extents,
                                    new Vector3(
                                        x,
                                        y,
                                        z
                                    )
                                );

                            Vector3 worldCorner =
                                renderer.transform.TransformPoint(
                                    localCorner
                                );

                            Vector3 refCorner =
                                reference.InverseTransformPoint(
                                    worldCorner
                                );

                            if (!hasBounds)
                            {
                                combinedBounds =
                                    new Bounds(
                                        refCorner,
                                        Vector3.zero
                                    );

                                hasBounds =
                                    true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(
                                    refCorner
                                );
                            }
                        }
                    }
                }
            }

            return
                hasBounds;
        }

        // ==================================================
        // FOLDERS / AUTO ASSIGN
        // ==================================================

        private static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(
                    "Assets/Prefabs"
                ))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    "Prefabs"
                );
            }

            if (!AssetDatabase.IsValidFolder(
                    OutputFolder
                ))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Prefabs",
                    "Trains"
                );
            }
        }

        private static void AssignPrefabsToOpenScene(
            GameObject normalPrefab,
            GameObject rampPrefab
        )
        {
            TrainRandomSpawner[] spawners =
                Object.FindObjectsByType<TrainRandomSpawner>(
                    FindObjectsSortMode.None
                );

            for (int i = 0;
                 i < spawners.Length;
                 i++)
            {
                SerializedObject serialized =
                    new SerializedObject(
                        spawners[i]
                    );

                SerializedProperty normalProperty =
                    serialized.FindProperty(
                        "normalTrainVisualPrefab"
                    );

                SerializedProperty rampProperty =
                    serialized.FindProperty(
                        "rampTrainVisualPrefab"
                    );

                if (normalProperty != null)
                {
                    normalProperty.objectReferenceValue =
                        normalPrefab;
                }

                if (rampProperty != null)
                {
                    rampProperty.objectReferenceValue =
                        rampPrefab;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(
                    spawners[i]
                );

                EditorSceneManager.MarkSceneDirty(
                    spawners[i].gameObject.scene
                );
            }
        }
    }
}
