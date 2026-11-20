using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoCleaner.UI.Editor
{
    /// <summary>
    /// 크레딧 화면 프리팹(UI_Credits.prefab)을 만듭니다. 열린 씬은 건드리지 않도록 Preview Scene에서 조립합니다.
    /// 메뉴: DinoCleaner/UI/Build Credits Prefab
    /// </summary>
    public static class CreditsPrefabBuilder
    {
        private const string ParentFolder = "Assets/5. UI";
        private const string CreditsFolderName = "Credits";
        private const string CreditsFolder = ParentFolder + "/" + CreditsFolderName;
        private const string PrefabPath = CreditsFolder + "/UI_Credits.prefab";
        private const string JsonPath = CreditsFolder + "/credits.json";
        private const string FontPath = "Assets/5. UI/Font/Luckiest_Guy/LuckiestGuy-Regular.ttf";
        private const string SpriteDir = "Assets/5. UI/Sprite/kenney_ui-pack-rpg-expansion/PNG/";
        private const string BackgroundSpritePath = SpriteDir + "panel_brown.png";
        private const string ButtonSpritePath = SpriteDir + "buttonLong_brown.png";
        private const string ButtonPressedSpritePath = SpriteDir + "buttonLong_brown_pressed.png";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const int CanvasSortingOrder = 100;

        // 스크롤 영역 여백 (왼쪽, 아래, 오른쪽, 위). 아래쪽은 Back 버튼 자리입니다.
        private static readonly Vector4 ScrollMargin = new Vector4(260f, 140f, 260f, 60f);
        private const int ContentPaddingTop = 40;
        private const int ContentPaddingBottom = 160;
        private const float ContentSpacing = 8f;
        private const float ScreenMatch = 0.5f;

        private static readonly Vector2 BackButtonSize = new Vector2(260f, 80f);
        private static readonly Vector2 BackButtonOffset = new Vector2(40f, 40f);
        private const int BackFontSize = 36;

        private static readonly Color BackgroundFallbackColor = new Color(0.16f, 0.1f, 0.06f, 1f);
        private static readonly Color TextLight = new Color(0.98f, 0.94f, 0.85f);

        [MenuItem("DinoCleaner/UI/Build Credits Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Credits] Play 모드를 끈 뒤 실행하세요.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                bool isConfirmed = EditorUtility.DisplayDialog("크레딧 프리팹 덮어쓰기",
                    $"{PrefabPath} 이(가) 이미 있습니다. 새로 만들면 프리팹에 직접 고친 내용은 사라집니다. 덮어쓸까요?",
                    "덮어쓰기", "취소");
                if (!isConfirmed) return;
            }

            if (!AssetDatabase.IsValidFolder(CreditsFolder))
            {
                AssetDatabase.CreateFolder(ParentFolder, CreditsFolderName);
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = CreateRoot(previewScene);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool isSaved);
                if (isSaved) Debug.Log($"[Credits] 프리팹 생성 완료: {PrefabPath}");
                else Debug.LogError($"[Credits] 프리팹 저장 실패: {PrefabPath}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static GameObject CreateRoot(Scene scene)
        {
            var root = new GameObject("UI_Credits", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ScreenMatch;

            root.AddComponent<GraphicRaycaster>();

            Font font = LoadFont();

            CreateBackground(root.transform);
            ScrollRect scrollRect = CreateScrollView(root.transform, out RectTransform content);
            Button backButton = CreateBackButton(root.transform, font);

            var screen = root.AddComponent<UI_CreditsScreen>();
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_creditsJson").objectReferenceValue = LoadJson();
            serialized.FindProperty("_font").objectReferenceValue = font;
            serialized.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
            serialized.FindProperty("_content").objectReferenceValue = content;
            serialized.FindProperty("_backButton").objectReferenceValue = backButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void CreateBackground(Transform parent)
        {
            RectTransform rect = CreateRect("Background", parent);
            Stretch(rect);

            var image = rect.gameObject.AddComponent<Image>();
            Sprite sprite = LoadSprite(BackgroundSpritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.color = BackgroundFallbackColor;
            }
        }

        private static ScrollRect CreateScrollView(Transform parent, out RectTransform content)
        {
            RectTransform scrollView = CreateRect("ScrollView", parent);
            Stretch(scrollView);
            scrollView.offsetMin = new Vector2(ScrollMargin.x, ScrollMargin.y);
            scrollView.offsetMax = new Vector2(-ScrollMargin.z, -ScrollMargin.w);

            RectTransform viewport = CreateRect("Viewport", scrollView);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, ContentPaddingTop, ContentPaddingBottom);
            layout.spacing = ContentSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 휠과 자동 스크롤은 UI_CreditsScreen이 직접 처리하므로 ScrollRect 자체의 휠 입력은 끕니다.
            var scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 0f;
            return scrollRect;
        }

        private static Button CreateBackButton(Transform parent, Font font)
        {
            RectTransform rect = CreateRect("BackButton", parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = BackButtonOffset;
            rect.sizeDelta = BackButtonSize;

            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Sprite sprite = LoadSprite(ButtonSpritePath);
            Sprite pressed = LoadSprite(ButtonPressedSpritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            if (pressed != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { pressedSprite = pressed, highlightedSprite = sprite, selectedSprite = sprite };
            }

            RectTransform labelRect = CreateRect("Label", rect);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<Text>();
            label.text = "Back";
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = BackFontSize;
            label.color = TextLight;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextAsset LoadJson()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if (json == null) Debug.LogWarning($"[Credits] 크레딧 데이터를 찾지 못했습니다: {JsonPath}");
            return json;
        }

        private static Font LoadFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) Debug.LogWarning($"[Credits] 폰트를 찾지 못해 비워 둡니다 (런타임에 기본 폰트 사용): {FontPath}");
            return font;
        }

        // 임포트 설정(.meta)은 바꾸지 않고 불러오기만 합니다. Sprite가 아니면 null입니다.
        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[Credits] Sprite를 찾지 못해 기본 모양을 씁니다: {path}");
            return sprite;
        }
    }
}
