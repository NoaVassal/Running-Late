using UnityEngine;

namespace RunningLate
{
    public class ScoreHUD : MonoBehaviour
    {
        private ScoreSystem scoreSystem;
        private int displayedScore = 0;

        private GUIStyle textStyle;
        private Texture2D panelTexture;

        private void Start()
        {
            scoreSystem =
                Object.FindFirstObjectByType<ScoreSystem>();

            if (scoreSystem == null)
            {
                Debug.LogError(
                    "ScoreHUD: ScoreSystem was not found."
                );

                enabled = false;
                return;
            }

            displayedScore =
                scoreSystem.ProjectPoints;

            scoreSystem.OnProjectPointsChanged +=
                HandleScoreChanged;
        }

        private void HandleScoreChanged(int value)
        {
            displayedScore = value;
        }

        private void OnGUI()
        {
            CreateStylesIfNeeded();

            float width = 145f;
            float height = 36f;

            float x = 8f;
            float y = 5f;

            Rect panelRect = new Rect(
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
                "POINTS: " + displayedScore,
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

                textStyle.fontSize = 16;

                textStyle.fontStyle =
                    FontStyle.Bold;

                textStyle.normal.textColor =
                    Color.white;
            }
        }

        private void OnDestroy()
        {
            if (scoreSystem != null)
            {
                scoreSystem.OnProjectPointsChanged -=
                    HandleScoreChanged;
            }

            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}