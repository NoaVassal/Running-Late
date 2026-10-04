using UnityEngine;

namespace RunningLate
{
    public class BatteryHUD : MonoBehaviour
    {
        private BatterySystem batterySystem;
        private float displayedBattery = 100f;

        private GUIStyle textStyle;
        private Texture2D panelTexture;

        private void Start()
        {
            batterySystem =
                UnityEngine.Object.FindFirstObjectByType<BatterySystem>();

            if (batterySystem == null)
            {
                Debug.LogError(
                    "BatteryHUD: BatterySystem was not found."
                );

                enabled = false;
                return;
            }

            displayedBattery =
                batterySystem.CurrentBattery;

            batterySystem.OnBatteryChanged +=
                HandleBatteryChanged;
        }

        private void HandleBatteryChanged(
            float value
        )
        {
            displayedBattery =
                value;
        }

        private void OnGUI()
        {
            // Hide gameplay HUD immediately during the ending movie.
            if (GameManager.Instance != null &&
                GameManager.Instance.State ==
                    GameManager.GameState.EndingVideo)
            {
                return;
            }

            CreateStylesIfNeeded();

            float width = 165f;
            float height = 36f;

            float x =
                Screen.width -
                width -
                8f;

            float y = 5f;

            Rect panelRect =
                new Rect(
                    x,
                    y,
                    width,
                    height
                );

            GUI.DrawTexture(
                panelRect,
                panelTexture,
                ScaleMode.StretchToFill
            );

            string batteryText =
                "BATTERY: " +
                Mathf.RoundToInt(
                    displayedBattery
                ) +
                "%";

            GUI.Label(
                panelRect,
                batteryText,
                textStyle
            );
        }

        private void CreateStylesIfNeeded()
        {
            if (panelTexture == null)
            {
                panelTexture =
                    new Texture2D(
                        1,
                        1,
                        TextureFormat.RGBA32,
                        false
                    );

                panelTexture.SetPixel(
                    0,
                    0,
                    new Color(
                        0.03f,
                        0.05f,
                        0.08f,
                        0.75f
                    )
                );

                panelTexture.Apply();
            }

            if (textStyle == null)
            {
                textStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );

                textStyle.alignment =
                    TextAnchor.MiddleCenter;

                textStyle.fontSize =
                    16;

                textStyle.fontStyle =
                    FontStyle.Bold;

                textStyle.normal.textColor =
                    Color.white;
            }
        }

        private void OnDestroy()
        {
            if (batterySystem != null)
            {
                batterySystem.OnBatteryChanged -=
                    HandleBatteryChanged;
            }

            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}
