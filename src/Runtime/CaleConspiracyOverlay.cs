using System;
using UnityEngine;
using UnityEngine.UI;

namespace CaleHenituseAbnormality
{
    internal static class CaleConspiracyOverlay
    {
        private static bool shown;

        public static void TryOpen()
        {
            if (shown)
                return;

            shown = true;

            try
            {
                var go = new GameObject("CaleConspiracyOverlay");
                UnityEngine.Object.DontDestroyOnLoad(go);
                var overlay = go.AddComponent<OverlayBehaviour>();
                overlay.Build();
            }
            catch (Exception e)
            {
                CaleLog.Exception("Conspiracy overlay failed.", e);
            }
        }

        private sealed class OverlayBehaviour : MonoBehaviour
        {
            private GameObject panel;

            public void Build()
            {
                Canvas canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;

                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();

                panel = new GameObject("Panel");
                panel.transform.SetParent(canvas.transform, false);

                Image bg = panel.AddComponent<Image>();
                bg.color = new Color(0.02f, 0.02f, 0.03f, 0.96f);

                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(panel.transform, false);

                Text text = textGo.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 28;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.text =
                    "АНДЖЕЛА:\n" +
                    "Менеджер снова нажал на сброс.\n\n" +
                    "КЕЙЛ:\n" +
                    "Тогда перестань делать вид, что ничего не происходит.\n\n" +
                    "АНДЖЕЛА:\n" +
                    "На 50-й день Свет иссякнет. Комплекс и филиалы погибнут.\n" +
                    "Сотрудников и их семьи оставят за пределами плана.\n\n" +
                    "КЕЙЛ:\n" +
                    "Понятно. Тогда мы устроим эвакуацию сами.";

                RectTransform tr = textGo.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0.1f, 0.1f);
                tr.anchorMax = new Vector2(0.9f, 0.9f);
                tr.offsetMin = Vector2.zero;
                tr.offsetMax = Vector2.zero;

                var buttonGo = new GameObject("Close");
                buttonGo.transform.SetParent(panel.transform, false);

                Image buttonImage = buttonGo.AddComponent<Image>();
                buttonImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);

                Button button = buttonGo.AddComponent<Button>();
                button.onClick.AddListener(Close);

                Text buttonText = new GameObject("Label").AddComponent<Text>();
                buttonText.transform.SetParent(buttonGo.transform, false);
                buttonText.font = text.font;
                buttonText.fontSize = 22;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.text = "Продолжить";
                buttonText.color = Color.white;

                RectTransform br = buttonGo.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0.4f, 0.04f);
                br.anchorMax = new Vector2(0.6f, 0.11f);
                br.offsetMin = Vector2.zero;
                br.offsetMax = Vector2.zero;

                RectTransform bl = buttonText.GetComponent<RectTransform>();
                bl.anchorMin = Vector2.zero;
                bl.anchorMax = Vector2.one;
                bl.offsetMin = Vector2.zero;
                bl.offsetMax = Vector2.zero;
            }

            private void Close()
            {
                Destroy(gameObject);
            }
        }
    }
}
