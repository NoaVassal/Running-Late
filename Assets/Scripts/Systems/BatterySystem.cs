using System;
using System.Collections;
using UnityEngine;

namespace RunningLate
{
    public class BatterySystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        private Coroutine drainCoroutine;

        public float CurrentBattery { get; private set; }

        public event Action<float> OnBatteryChanged;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    "BatterySystem: GameConfig is not assigned."
                );

                enabled = false;
                return;
            }

            CurrentBattery = config.maxBattery;
        }

        private void Start()
        {
            if (config == null)
                return;

            OnBatteryChanged?.Invoke(CurrentBattery);

            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "BatterySystem: GameManager was not found in the scene."
                );
                return;
            }

            GameManager.Instance.OnRunReset += ResetBattery;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            if (GameManager.Instance.IsRunning)
            {
                StartDrainCoroutine();
            }
        }

        public void RestoreBattery()
        {
            if (config == null)
                return;

            SetBattery(
                CurrentBattery + config.batteryPickupValue
            );
        }

        private void HandleGameStateChanged(
            GameManager.GameState newState
        )
        {
            if (newState == GameManager.GameState.Running)
            {
                StartDrainCoroutine();
            }
            else
            {
                StopDrainCoroutine();
            }
        }

        private void StartDrainCoroutine()
        {
            if (drainCoroutine != null || config == null)
                return;

            drainCoroutine = StartCoroutine(DrainBatteryRoutine());
        }

        private void StopDrainCoroutine()
        {
            if (drainCoroutine == null)
                return;

            StopCoroutine(drainCoroutine);
            drainCoroutine = null;
        }

        private IEnumerator DrainBatteryRoutine()
        {
            while (GameManager.Instance != null &&
                   GameManager.Instance.IsRunning)
            {
                yield return new WaitForSeconds(
                    config.batteryDrainInterval
                );

                if (GameManager.Instance == null ||
                    !GameManager.Instance.IsRunning)
                {
                    break;
                }

                SetBattery(
                    CurrentBattery - config.batteryDrainAmount
                );

                if (CurrentBattery <= 0f)
                {
                    GameManager.Instance.TriggerGameOver(
                        "Battery reached zero"
                    );
                    break;
                }
            }

            drainCoroutine = null;
        }

        private void SetBattery(float value)
        {
            float clampedValue = Mathf.Clamp(
                value,
                0f,
                config.maxBattery
            );

            if (Mathf.Approximately(CurrentBattery, clampedValue))
                return;

            CurrentBattery = clampedValue;
            OnBatteryChanged?.Invoke(CurrentBattery);
        }

        private void ResetBattery()
        {
            if (config == null)
                return;

            CurrentBattery = Mathf.Clamp(
                config.maxBattery,
                0f,
                config.maxBattery
            );

            OnBatteryChanged?.Invoke(CurrentBattery);
        }

        private void OnDestroy()
        {
            StopDrainCoroutine();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -= ResetBattery;
                GameManager.Instance.OnGameStateChanged -=
                    HandleGameStateChanged;
            }
        }
    }
}
