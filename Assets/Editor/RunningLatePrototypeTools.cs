#if UNITY_EDITOR
using RunningLate;
using UnityEditor;
using UnityEngine;

public static class RunningLatePrototypeTools
{
    private const string SegmentName = "TrackSegment_01";

    [MenuItem("Tools/Running Late/Create Prototype Track Segment")]
    public static void CreatePrototypeTrackSegment()
    {
        GameObject segment = GameObject.Find(SegmentName);

        if (segment == null)
        {
            segment = new GameObject(SegmentName);
            Undo.RegisterCreatedObjectUndo(segment, "Create Prototype Track Segment");
        }

        Undo.RecordObject(segment.transform, "Reset Prototype Track Segment");
        segment.transform.position = Vector3.zero;
        segment.transform.rotation = Quaternion.identity;
        segment.transform.localScale = Vector3.one;

        int groundLayer = LayerMask.NameToLayer("Ground");

        if (groundLayer == -1)
        {
            Debug.LogWarning(
                "Running Late: Ground layer was not found. " +
                "Create a layer named 'Ground' before testing jumping."
            );
        }

        CreateOrUpdateCube(
            segment.transform,
            "Boulevard",
            new Vector3(0f, -0.1f, 0f),
            new Vector3(2.3f, 0.2f, 30f),
            groundLayer
        );

        CreateOrUpdateCube(
            segment.transform,
            "LeftTrain",
            new Vector3(-2.5f, 0.6f, 0f),
            new Vector3(2.2f, 1.2f, 30f),
            groundLayer
        );

        CreateOrUpdateCube(
            segment.transform,
            "RightTrain",
            new Vector3(2.5f, 0.6f, 0f),
            new Vector3(2.2f, 1.2f, 30f),
            groundLayer
        );

        Selection.activeGameObject = segment;

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log(
            "Running Late: Prototype layout created — " +
            "Left Train | Boulevard | Right Train."
        );
    }

    private static void CreateOrUpdateCube(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        int groundLayer
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject cube;

        if (existing == null)
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            cube = existing.gameObject;
            Undo.RecordObject(cube.transform, "Update " + objectName);
        }

        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        if (groundLayer != -1)
        {
            cube.layer = groundLayer;
        }

        EditorUtility.SetDirty(cube);
    }
}
#endif
