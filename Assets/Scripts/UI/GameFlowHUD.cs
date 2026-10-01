using UnityEngine;

namespace RunningLate
{
    public class GameFlowHUD : MonoBehaviour
    {
        private ScoreSystem scoreSystem;
        private BatterySystem batterySystem;

        private string gameOverReason = "";

        private bool passed = false;
        private int finalGrade = 0;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle resultStyle;

        private Texture2D overlayTexture;
        private Texture2D panelTexture;

        private void Start()
        {
            scoreSystem =
                Object.FindFirstObjectByType<ScoreSystem>();

            batterySystem =
                Object.FindFirstObjectByType<BatterySystem>();

            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "GameFlowHUD: GameManager was not found."
                );

                enabled = false;
                return;
            }

            GameManager.Instance.OnGameOver +=
                HandleGameOver;

            GameManager.Instance.OnRunFinished +=
                HandleRunFinished;

            GameManager.Instance.OnRunReset +=
                HandleRunReset;
        }

        // ==================================================
        // EVENTS
        // ==================================================

        private void HandleGameOver(
            string reason
        )
        {
            gameOverReason = reason;
        }

        private void HandleRunFinished(
            bool didPass,
            int grade
        )
        {
            passed = didPass;
            finalGrade = grade;
        }

        private void HandleRunReset()
        {
            gameOverReason = "";
            passed = false;
            finalGrade = 0;
        }

        // ==================================================
        // GUI
        // ==================================================

        private void OnGUI()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            CreateStylesIfNeeded();

            switch (GameManager.Instance.State)
            {
                case GameManager.GameState.GetReady:
                    DrawStartScreen();
                    break;

                case GameManager.GameState.Results:
                    DrawResultsScreen();
                    break;

                case GameManager.GameState.GameOver:
                    DrawGameOverScreen();
                    break;
            }
        }

        // ==================================================
        // START SCREEN
        // ==================================================

        private void DrawStartScreen()
        {
            DrawFullScreenOverlay();

            float panelWidth = 620f;
            float panelHeight = 360f;

            Rect panel =
                CenterRect(
                    panelWidth,
                    panelHeight
                );

            GUI.DrawTexture(
                panel,
                panelTexture
            );

            Rect titleRect =
                new Rect(
                    panel.x,
                    panel.y + 35f,
                    panel.width,
                    70f
                );

            GUI.Label(
                titleRect,
                "RUNNING LATE",
                titleStyle
            );

            Rect subtitleRect =
                new Rect(
                    panel.x + 40f,
                    panel.y + 115f,
                    panel.width - 80f,
                    55f
                );

            GUI.Label(
                subtitleRect,
                "Get to class before 5:30!",
                subtitleStyle
            );

            Rect controlsRect =
                new Rect(
                    panel.x + 40f,
                    panel.y + 175f,
                    panel.width - 80f,
                    90f
                );

            GUI.Label(
                controlsRect,
                "A / D or Arrows  -  Move\n" +
                "SPACE / W / Up  -  Jump\n" +
                "S / Down  -  Slide",
                bodyStyle
            );

            Rect startRect =
                new Rect(
                    panel.x,
                    panel.y + 290f,
                    panel.width,
                    45f
                );

            GUI.Label(
                startRect,
                "PRESS SPACE OR ENTER TO START",
                resultStyle
            );
        }

        // ==================================================
        // RESULTS SCREEN
        // ==================================================

        private void DrawResultsScreen()
        {
            DrawFullScreenOverlay();

            int points =
                scoreSystem != null
                    ? scoreSystem.ProjectPoints
                    : finalGrade;

            float battery =
                batterySystem != null
                    ? batterySystem.CurrentBattery
                    : 0f;

            string resultText =
                passed
                    ? "PASSED!"
                    : "FAILED";

            DrawEndPanel(
                resultText,
                "FINAL GRADE: " + finalGrade,
                "PROJECT POINTS: " + points,
                "BATTERY: " +
                Mathf.RoundToInt(battery) +
                "%"
            );
        }

        // ==================================================
        // GAME OVER SCREEN
        // ==================================================

        private void DrawGameOverScreen()
        {
            DrawFullScreenOverlay();

            int points =
                scoreSystem != null
                    ? scoreSystem.ProjectPoints
                    : 0;

            float battery =
                batterySystem != null
                    ? batterySystem.CurrentBattery
                    : 0f;

            string reason =
                ConvertReasonToText(
                    gameOverReason
                );

            DrawEndPanel(
                "GAME OVER",
                reason,
                "PROJECT POINTS: " + points,
                "BATTERY: " +
                Mathf.RoundToInt(battery) +
                "%"
            );
        }

        // ==================================================
        // END PANEL
        // ==================================================

        private void DrawEndPanel(
            string heading,
            string line1,
            string line2,
            string line3
        )
        {
            float panelWidth = 600f;
            float panelHeight = 340f;

            Rect panel =
                CenterRect(
                    panelWidth,
                    panelHeight
                );

            GUI.DrawTexture(
                panel,
                panelTexture
            );

            GUI.Label(
                new Rect(
                    panel.x,
                    panel.y + 30f,
                    panel.width,
                    65f
                ),
                heading,
                titleStyle
            );

            GUI.Label(
                new Rect(
                    panel.x + 30f,
                    panel.y + 115f,
                    panel.width - 60f,
                    45f
                ),
                line1,
                subtitleStyle
            );

            GUI.Label(
                new Rect(
                    panel.x + 30f,
                    panel.y + 165f,
                    panel.width - 60f,
                    40f
                ),
                line2,
                bodyStyle
            );

            GUI.Label(
                new Rect(
                    panel.x + 30f,
                    panel.y + 205f,
                    panel.width - 60f,
                    40f
                ),
                line3,
                bodyStyle
            );

            GUI.Label(
                new Rect(
                    panel.x,
                    panel.y + 280f,
                    panel.width,
                    40f
                ),
                "PRESS SPACE OR ENTER TO RETRY",
                resultStyle
            );
        }

        // ==================================================
        // GAME OVER REASONS
        // ==================================================

        private string ConvertReasonToText(
            string reason
        )
        {
            if (string.IsNullOrEmpty(reason))
            {
                return "RUN FAILED";
            }

            if (reason.Contains("Timer"))
            {
                return "YOU WERE LATE - 5:30!";
            }

            if (reason.Contains("Battery"))
            {
                return "BATTERY EMPTY!";
            }

            if (reason.Contains("gap") ||
                reason.Contains("Gap"))
            {
                return "YOU FELL INTO A GAP!";
            }

            if (reason.Contains("obstacle") ||
                reason.Contains("Obstacle"))
            {
                return "YOU HIT AN OBSTACLE!";
            }

            return reason.ToUpper();
        }

        // ==================================================
        // LAYOUT HELPERS
        // ==================================================

        private Rect CenterRect(
            float width,
            float height
        )
        {
            return new Rect(
                (Screen.width - width) / 2f,
                (Screen.height - height) / 2f,
                width,
                height
            );
        }

        private void DrawFullScreenOverlay()
        {
            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height
                ),
                overlayTexture
            );
        }

        // ==================================================
        // STYLE SETUP
        // ==================================================

        private void CreateStylesIfNeeded()
        {
            if (overlayTexture == null)
            {
                overlayTexture =
                    CreateColorTexture(
                        new Color(
                            0f,
                            0f,
                            0f,
                            0.65f
                        )
                    );
            }

            if (panelTexture == null)
            {
                panelTexture =
                    CreateColorTexture(
                        new Color(
                            0.04f,
                            0.06f,
                            0.09f,
                            0.95f
                        )
                    );
            }

            if (titleStyle == null)
            {
                titleStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );

                titleStyle.alignment =
                    TextAnchor.MiddleCenter;

                titleStyle.fontSize = 42;

                titleStyle.fontStyle =
                    FontStyle.Bold;

                titleStyle.normal.textColor =
                    Color.white;
            }

            if (subtitleStyle == null)
            {
                subtitleStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );

                subtitleStyle.alignment =
                    TextAnchor.MiddleCenter;

                subtitleStyle.fontSize = 24;

                subtitleStyle.fontStyle =
                    FontStyle.Bold;

                subtitleStyle.normal.textColor =
                    Color.white;
            }

            if (bodyStyle == null)
            {
                bodyStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );

                bodyStyle.alignment =
                    TextAnchor.MiddleCenter;

                bodyStyle.fontSize = 19;

                bodyStyle.normal.textColor =
                    Color.white;
            }

            if (resultStyle == null)
            {
                resultStyle =
                    new GUIStyle(
                        GUI.skin.label
                    );

                resultStyle.alignment =
                    TextAnchor.MiddleCenter;

                resultStyle.fontSize = 20;

                resultStyle.fontStyle =
                    FontStyle.Bold;

                resultStyle.normal.textColor =
                    Color.yellow;
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

        // ==================================================
        // CLEANUP
        // ==================================================

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -=
                    HandleGameOver;

                GameManager.Instance.OnRunFinished -=
                    HandleRunFinished;

                GameManager.Instance.OnRunReset -=
                    HandleRunReset;
            }

            if (overlayTexture != null)
            {
                Destroy(
                    overlayTexture
                );
            }

            if (panelTexture != null)
            {
                Destroy(
                    panelTexture
                );
            }
        }
    }
}