using UnityEngine;

namespace RunningLate
{
    public class BatteryPickup : MonoBehaviour
    {
        private BatterySystem batterySystem;
        private bool collected = false;

        private void Awake()
        {
            FindBatterySystem();
        }

        private void OnEnable()
        {
            collected = false;

            if (batterySystem == null)
            {
                FindBatterySystem();
            }
        }

        private void FindBatterySystem()
        {
            batterySystem =
                Object.FindFirstObjectByType<BatterySystem>();

            if (batterySystem == null)
            {
                Debug.LogError(
                    "BatteryPickup: BatterySystem was not found in the scene."
                );
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (GameManager.Instance == null ||
                !GameManager.Instance.IsRunning)
            {
                return;
            }

            if (batterySystem == null)
                return;

            collected = true;

            batterySystem.RestoreBattery();

            Debug.Log(
                "Battery pickup collected!"
            );

            gameObject.SetActive(false);
        }
    }
}
