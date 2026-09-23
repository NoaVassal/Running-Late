using UnityEngine;

namespace RunningLate
{
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "Running Late/Game Config"
    )]
    public class GameConfig : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)]
        public float baseScrollSpeed = 12f;

        [Min(0f)]
        public float laneChangeSpeed = 10f;

        [Min(0.1f)]
        public float laneWidth = 2.5f;

        [Min(0f)]
        public float jumpForce = 7f;

        [Min(0.1f)]
        public float slideDuration = 0.8f;

        [Header("Run")]
        [Min(1f)]
        public float classTimer = 180f;

        [Header("Battery")]
        [Range(1f, 100f)]
        public float maxBattery = 100f;

        [Min(0.1f)]
        public float batteryDrainInterval = 6f;

        [Min(0f)]
        public float batteryDrainAmount = 1f;

        [Min(0f)]
        public float batteryPickupValue = 5f;

        [Header("Project Points")]
        [Min(1)]
        public int projectPointValue = 10;

        [Min(0)]
        public int passingGrade = 60;
    }
}
