using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ControlS
{
    public static class RuntimeUI
    {
        private static Font font;
        private static Sprite whiteSprite;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                if (!Application.isPlaying)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    return font;
                }
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 24);
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null) return whiteSprite;
                whiteSprite = Resources.Load<Sprite>("ControlS/WhitePixel");
                if (whiteSprite != null) return whiteSprite;
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "ControlS_WhitePixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
                whiteSprite.name = "ControlS_WhiteSprite";
                return whiteSprite;
            }
        }

        public static void ResetCachedAssets()
        {
            font = null;
            whiteSprite = null;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            return image;
        }

        public static Text Text(string name, Transform parent, string value, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rt = Rect(name, parent);
            Stretch(rt);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button Button(string name, Transform parent, string label, UnityAction onClick,
            Color normal, Color highlighted, int fontSize = 22)
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = normal;
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = Color.Lerp(highlighted, Color.black, .18f);
            colors.selectedColor = highlighted;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            var labelText = Text("Label", rt, label, fontSize, Color.white, TextAnchor.MiddleCenter);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 12;
            labelText.resizeTextMaxSize = fontSize;
            labelText.rectTransform.offsetMin = new Vector2(8, 4);
            labelText.rectTransform.offsetMax = new Vector2(-8, -4);
            return button;
        }

        public static InputField Input(string name, Transform parent, string placeholder, string initial = "")
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = new Color(.05f, .06f, .075f, 1f);

            var input = rt.gameObject.AddComponent<InputField>();
            var typed = Text("Text", rt, initial, 24, new Color(.84f, .95f, .9f), TextAnchor.MiddleLeft);
            typed.supportRichText = false;
            typed.rectTransform.offsetMin = new Vector2(14, 4);
            typed.rectTransform.offsetMax = new Vector2(-14, -4);
            var ghost = Text("Placeholder", rt, placeholder, 22, new Color(.55f, .58f, .62f, .75f), TextAnchor.MiddleLeft);
            ghost.fontStyle = FontStyle.Italic;
            ghost.rectTransform.offsetMin = new Vector2(14, 4);
            ghost.rectTransform.offsetMax = new Vector2(-14, -4);
            input.textComponent = typed;
            input.placeholder = ghost;
            input.text = initial;
            input.caretColor = Color.white;
            input.selectionColor = new Color(.2f, .65f, .55f, .45f);
            return input;
        }

        public static Slider Slider(string name, Transform parent, float value)
        {
            var root = Rect(name, parent);
            var background = Panel("Background", root, new Color(.08f, .09f, .11f, 1f),
                new Vector2(0, .35f), new Vector2(1, .65f), Vector2.zero, Vector2.zero);
            var fillArea = Rect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0, .25f);
            fillArea.anchorMax = new Vector2(1, .75f);
            fillArea.offsetMin = new Vector2(5, 0);
            fillArea.offsetMax = new Vector2(-5, 0);
            var fill = Panel("Fill", fillArea, new Color(.2f, .72f, .62f, 1f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var handleArea = Rect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10, 0);
            handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Panel("Handle", handleArea, new Color(.88f, .93f, .9f, 1f),
                new Vector2(0, .15f), new Vector2(0, .85f), new Vector2(-10, 0), new Vector2(10, 0));

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            background.raycastTarget = false;
            fill.raycastTarget = false;
            return slider;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, float x, float y, float width, float height,
            Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = pivot ?? new Vector2(.5f, .5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
