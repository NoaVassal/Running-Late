RUNNING LATE - BONUS POINTS +5

Prefab: Assets/Prefabs/Collectibles/BonusPoints5.prefab

This prefab is made for your existing Running-Late project (Unity 6).
It uses the existing GradePlus5 sprite and ProjectPointCollectible script.
It includes an enabled Sprite Renderer with full opacity and a Sphere Collider
with Is Trigger enabled. The initial scale is 0.08; the spawner controls scale
and position during a run.

INSTALL
1. Stop Play Mode and open your Running-Late project in Unity.
2. Assets > Import Package > Custom Package. Choose BonusPoints5.unitypackage
   and click Import.
3. Drag Assets/Prefabs/Collectibles/BonusPoints5.prefab into the Hierarchy.
   Keep the new object active. Its initial position is (0, 1.05, 5).
4. Disable the old ProjectPoint_Test scene object using the checkbox beside
   its name. Keep exactly one active scene object with ProjectPointCollectible
   before Play: the spawner currently finds the first active instance and
   clones it. The old object can stay disabled for rollback.
5. Save the scene and start a run.

SCORE
The existing ProjectPointCollectible calls ScoreSystem.AddProjectPoints().
The project's GameConfig.projectPointValue is 5 in the inspected repository.
Keep Project Point Value at 5 on the GameConfig used by ScoreSystem to award
exactly five points per pickup. The collectible prevents collecting twice
and deactivates itself for reuse by the existing pool.

DEPENDENCIES ALREADY IN YOUR PROJECT
- Assets/Art/Collectibles/GradePlus5.png (Sprite, Single)
  GUID: 7cbd905a918f80945bf071d9abd04c43
- Assets/Scripts/Collectibles/ProjectPointCollectible.cs
  GUID: c2d2e8acd76794ad98942e69f7c3f709
- Existing ScoreSystem, GameManager and ProjectPointRandomSpawner.

The texture and existing scripts are referenced, not bundled or replaced.
If Sprite shows None/Missing locally, assign GradePlus5 from Assets/Art/
Collectibles to the Sprite Renderer on the prefab. If the component says
Missing Script, replace that missing component with ProjectPointCollectible.

The spawner intentionally disables the original scene template in Play Mode.
Active pooled copies are placed along the route. Initial placement is random,
so a pickup is not guaranteed in the first segment.

VALIDATION
Prefab YAML, component references, trigger settings, package contents, and
dependency GUIDs were checked against the repository. The package has not
been imported or play-tested in a Unity Editor in this environment.
