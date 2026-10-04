using UnityEngine;

namespace RunningLate
{
    public class GameClockHUD : MonoBehaviour
    {
        private const int StartHour = 17;
        private const int StartMinute = 0;

        private const int EndHour = 17;
        private const int EndMinute = 30;

        private float runDuration = 180f;
        private float currentTimeRemaining = 180f;

        private GUIStyle textStyle;
        private Texture2D panelTexture;
        private Texture2D barBackgroundTexture;
        private Texture2D barFillTexture;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "GameClockHUD: GameManager was not found."
                );

                enabled = false;
                return;
            }

            runDuration =
                Mathf.Max(
                    1f,
                    GameManager.Instance.TimeRemaining
                );

            currentTimeRemaining =
                GameManager.Instance.TimeRemaining;

            GameManager.Instance.OnTimerChanged +=
                HandleTimerChanged;
        }

        private void HandleTimerChanged(
            float timeRemaining
        )
        {
            currentTimeRemaining =
                timeRemaining;
        }

        private void OnGUI()
        {
            // Hide clock and top progress bar during the ending movie.
            if (GameManager.Instance != null &&
                GameManager.Instance.State ==
                    GameManager.GameState.EndingVideo)
            {
                return;
            }

            CreateStylesIfNeeded();

            DrawTopTimeBar();
            DrawClockPanel();
        }

        private void DrawTopTimeBar()
        {
            float marginX = 0f;
            float y = 0f;
            float width = Screen.width;
            float height = 10f;

            Rect backgroundRect =
                new Rect(
                    marginX,
                    y,
                    width,
                    height
                );

            GUI.DrawTexture(
                backgroundRect,
                barBackgroundTexture,
                ScaleMode.StretchToFill
            );

            float ratio =
                Mathf.Clamp01(
                    currentTimeRemaining /
                    runDuration
                );

            Rect fillRect =
                new Rect(
                    marginX,
                    y,
                    width * ratio,
                    height
                );

            GUI.DrawTexture(
                fillRect,
                barFillTexture,
                ScaleMode.StretchToFill
            );
        }

        private void DrawClockPanel()
        {
            float width = 135f;
            float height = 34f;

            float x =
                (Screen.width - width) /
                2f;

            float y = 14f;

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

            GUI.Label(
                panelRect,
                GetDisplayedClock(),
                textStyle
            );
        }

        private string GetDisplayedClock()
        {
            float elapsed =
                runDuration -
                currentTimeRemaining;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    runDuration
                );

            int startTotalMinutes =
                StartHour * 60 +
                StartMinute;

            int endTotalMinutes =
                EndHour * 60 +
                EndMinute;

            int gameMinutes =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startTotalMinutes,
                        endTotalMinutes,
                        progress
                    )
                );

            int hour =
                gameMinutes /
                60;

            int minute =
                gameMinutes %
                60;

            return string.Format(
                "{0}:{1:00}",
                hour,
                minute
            );
        }

        private void CreateStylesIfNeeded()
        {
            if (panelTexture == null)
            {
                panelTexture =
                    CreateColorTexture(
                        new Color(
                            0.03f,
                            0.05f,
                            0.08f,
                            0.78f
                        )
                    );
            }

            if (barBackgroundTexture == null)
            {
                barBackgroundTexture =
                    CreateColorTexture(
                        new Color(
                            0.1f,
                            0.1f,
                            0.1f,
                            0.95f
                        )
                    );
            }

            if (barFillTexture == null)
            {
                barFillTexture =
                    CreateColorTexture(
                        new Color(
                            0.93f,
                            0.77f,
                            0.17f,
                            1f
                        )
                    );
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
                    18;

                textStyle.fontStyle =
                    FontStyle.Bold;

                textStyle.normal.textColor =
                    Color.white;
            }
        }

        private Texture2D CreateColorTexture(
            Color color
        )
        {
            Texture2D texture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );

            texture.SetPixel(
                0,
                0,
                color
            );

            texture.Apply();

            return texture;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimerChanged -=
                    HandleTimerChanged;
            }

            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }

            if (barBackgroundTexture != null)
            {
                Destroy(barBackgroundTexture);
            }

            if (barFillTexture != null)
            {
                Destroy(barFillTexture);
            }
        }
    }
}
