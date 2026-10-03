using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunningLate
{
    public class GameFlowHUD : MonoBehaviour
    {
        [Header("Screen Roots")]
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject resultsScreen;
        [SerializeField] private GameObject gameOverScreen;

        [Header("Start Screen")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text controlsText;
        [SerializeField] private Button playButton;

        [Header("Results Screen")]
        [SerializeField] private TMP_Text resultsHeadingText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text resultsPointsText;
        [SerializeField] private TMP_Text resultsBatteryText;
        [SerializeField] private Button resultsRetryButton;

        [Header("Game Over Screen")]
        [SerializeField] private TMP_Text gameOverHeadingText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text gameOverPointsText;
        [SerializeField] private TMP_Text gameOverBatteryText;
        [SerializeField] private Button gameOverRetryButton;

        private ScoreSystem scoreSystem;
        private BatterySystem batterySystem;

        private string gameOverReason = "";
        private bool passed;
        private int finalGrade;

        private void Start()
        {
            scoreSystem =
                Object.FindFirstObjectByType<ScoreSystem>();

            batterySystem =
                Object.FindFirstObjectByType<BatterySystem>();

            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "GameFlowHUD: GameManager was not found.",
                    this
                );

                enabled = false;
                return;
            }

            GameManager.Instance.OnGameStateChanged +=
                HandleGameStateChanged;

            GameManager.Instance.OnGameOver +=
                HandleGameOver;

            GameManager.Instance.OnRunFinished +=
                HandleRunFinished;

            GameManager.Instance.OnRunReset +=
                HandleRunReset;

            if (playButton != null)
            {
                playButton.onClick.AddListener(
                    HandlePlayClicked
                );
            }

            if (resultsRetryButton != null)
            {
                resultsRetryButton.onClick.AddListener(
                    HandleRetryClicked
                );
            }

            if (gameOverRetryButton != null)
            {
                gameOverRetryButton.onClick.AddListener(
                    HandleRetryClicked
                );
            }

            RefreshForState(
                GameManager.Instance.State
            );
        }

        private void HandleGameStateChanged(
            GameManager.GameState newState
        )
        {
            RefreshForState(newState);
        }

        private void HandleGameOver(
            string reason
        )
        {
            gameOverReason = reason;
            RefreshGameOverScreen();
        }

        private void HandleRunFinished(
            bool didPass,
            int grade
        )
        {
            passed = didPass;
            finalGrade = grade;
            RefreshResultsScreen();
        }

        private void HandleRunReset()
        {
            gameOverReason = "";
            passed = false;
            finalGrade = 0;
        }

        private void HandlePlayClicked()
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.State ==
                GameManager.GameState.GetReady)
            {
                GameManager.Instance.StartRun();
            }
        }

        private void HandleRetryClicked()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.GameState state =
                GameManager.Instance.State;

            if (state == GameManager.GameState.Results ||
                state == GameManager.GameState.GameOver)
            {
                GameManager.Instance.RestartRun();
            }
        }

        private void RefreshForState(
            GameManager.GameState state
        )
        {
            SetActive(
                startScreen,
                state == GameManager.GameState.GetReady
            );

            SetActive(
                resultsScreen,
                state == GameManager.GameState.Results
            );

            SetActive(
                gameOverScreen,
                state == GameManager.GameState.GameOver
            );

            if (state == GameManager.GameState.Results)
            {
                RefreshResultsScreen();
            }
            else if (state == GameManager.GameState.GameOver)
            {
                RefreshGameOverScreen();
            }
        }

        private void RefreshResultsScreen()
        {
            int points =
                scoreSystem != null
                    ? scoreSystem.ProjectPoints
                    : finalGrade;

            float battery =
                batterySystem != null
                    ? batterySystem.CurrentBattery
                    : 0f;

            SetText(
                resultsHeadingText,
                passed ? "PASSED!" : "FAILED"
            );

            SetText(
                gradeText,
                "FINAL GRADE: " + finalGrade
            );

            SetText(
                resultsPointsText,
                "PROJECT POINTS: " + points
            );

            SetText(
                resultsBatteryText,
                "BATTERY: " +
                Mathf.RoundToInt(battery) +
                "%"
            );
        }

        private void RefreshGameOverScreen()
        {
            int points =
                scoreSystem != null
                    ? scoreSystem.ProjectPoints
                    : 0;

            float battery =
                batterySystem != null
                    ? batterySystem.CurrentBattery
                    : 0f;

            SetText(
                gameOverHeadingText,
                "GAME OVER"
            );

            SetText(
                reasonText,
                ConvertReasonToText(gameOverReason)
            );

            SetText(
                gameOverPointsText,
                "PROJECT POINTS: " + points
            );

            SetText(
                gameOverBatteryText,
                "BATTERY: " +
                Mathf.RoundToInt(battery) +
                "%"
            );
        }

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

        private static void SetActive(
            GameObject screen,
            bool isActive
        )
        {
            if (screen != null &&
                screen.activeSelf != isActive)
            {
                screen.SetActive(isActive);
            }
        }

        private static void SetText(
            TMP_Text textComponent,
            string value
        )
        {
            if (textComponent != null)
            {
                textComponent.text = value;
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(
                    HandlePlayClicked
                );
            }

            if (resultsRetryButton != null)
            {
                resultsRetryButton.onClick.RemoveListener(
                    HandleRetryClicked
                );
            }

            if (gameOverRetryButton != null)
            {
                gameOverRetryButton.onClick.RemoveListener(
                    HandleRetryClicked
                );
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -=
                    HandleGameStateChanged;

                GameManager.Instance.OnGameOver -=
                    HandleGameOver;

                GameManager.Instance.OnRunFinished -=
                    HandleRunFinished;

                GameManager.Instance.OnRunReset -=
                    HandleRunReset;
            }
        }
    }
}
