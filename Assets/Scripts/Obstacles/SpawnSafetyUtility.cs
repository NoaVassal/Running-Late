using UnityEngine;

namespace RunningLate
{
    public static class SpawnSafetyUtility
    {
        private const float LaneThreshold = 0.75f;

        private const float LeftLaneX = -2.5f;
        private const float CenterLaneX = 0f;
        private const float RightLaneX = 2.5f;

        public static bool IsPickupTooClose(
            TrackSegment segment,
            float laneX,
            float localZ,
            float minZDistance
        )
        {
            if (IsTypeTooClose<BatteryPickup>(
                    segment,
                    laneX,
                    localZ,
                    minZDistance
                ))
            {
                return true;
            }

            if (IsTypeTooClose<ProjectPointCollectible>(
                    segment,
                    laneX,
                    localZ,
                    minZDistance
                ))
            {
                return true;
            }

            return false;
        }

        private static bool IsTypeTooClose<T>(
            TrackSegment segment,
            float laneX,
            float localZ,
            float minZDistance
        )
            where T : Component
        {
            T[] items =
                segment.GetComponentsInChildren<T>(
                    false
                );

            for (int i = 0; i < items.Length; i++)
            {
                Vector3 localPosition =
                    segment.transform
                        .InverseTransformPoint(
                            items[i]
                                .transform.position
                        );

                bool sameLane =
                    Mathf.Abs(
                        localPosition.x - laneX
                    ) < LaneThreshold;

                bool tooCloseZ =
                    Mathf.Abs(
                        localPosition.z - localZ
                    ) < minZDistance;

                if (sameLane && tooCloseZ)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsHazardTooClose(
            Vector3 candidateWorldPosition,
            float laneX,
            float minZDistance
        )
        {
            LethalObstacle[] hazards =
                Object.FindObjectsByType<
                    LethalObstacle
                >(
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] == null)
                    continue;

                if (!hazards[i]
                        .gameObject
                        .activeInHierarchy)
                {
                    continue;
                }

                Vector3 worldPosition =
                    hazards[i]
                        .transform.position;

                bool sameLane =
                    Mathf.Abs(
                        worldPosition.x - laneX
                    ) < LaneThreshold;

                bool tooCloseZ =
                    Mathf.Abs(
                        worldPosition.z -
                        candidateWorldPosition.z
                    ) < minZDistance;

                if (sameLane && tooCloseZ)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool WouldBlockAllLanes(
            Vector3 candidateWorldPosition,
            float candidateLaneX,
            float sameZWindow
        )
        {
            bool leftBlocked = false;
            bool centerBlocked = false;
            bool rightBlocked = false;

            MarkLaneBlocked(
                candidateLaneX,
                ref leftBlocked,
                ref centerBlocked,
                ref rightBlocked
            );

            LethalObstacle[] hazards =
                Object.FindObjectsByType<
                    LethalObstacle
                >(
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] == null)
                    continue;

                if (!hazards[i]
                        .gameObject
                        .activeInHierarchy)
                {
                    continue;
                }

                Vector3 worldPosition =
                    hazards[i]
                        .transform.position;

                bool sameZBand =
                    Mathf.Abs(
                        worldPosition.z -
                        candidateWorldPosition.z
                    ) < sameZWindow;

                if (!sameZBand)
                    continue;

                MarkLaneBlocked(
                    worldPosition.x,
                    ref leftBlocked,
                    ref centerBlocked,
                    ref rightBlocked
                );
            }

            return
                leftBlocked &&
                centerBlocked &&
                rightBlocked;
        }

        private static void MarkLaneBlocked(
            float x,
            ref bool leftBlocked,
            ref bool centerBlocked,
            ref bool rightBlocked
        )
        {
            float leftDistance =
                Mathf.Abs(x - LeftLaneX);

            float centerDistance =
                Mathf.Abs(x - CenterLaneX);

            float rightDistance =
                Mathf.Abs(x - RightLaneX);

            if (leftDistance <= centerDistance &&
                leftDistance <= rightDistance)
            {
                leftBlocked = true;
                return;
            }

            if (centerDistance <= leftDistance &&
                centerDistance <= rightDistance)
            {
                centerBlocked = true;
                return;
            }

            rightBlocked = true;
        }
    }
}