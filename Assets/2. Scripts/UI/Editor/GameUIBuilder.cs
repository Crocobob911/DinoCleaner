using System.Collections.Generic;
using System.Linq;
using DinoCleaner.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DinoCleaner.UI.Editor
{
    /// <summary>
    /// 와이어프레임의 UI 프리팹을 만들어 Addressables에 등록하고, 동우 폴더의 Home/Stage 씬에 UI를 구성합니다.
    /// 이미 있는 프리팹은 Sprite 작업을 지키기 위해 건너뛰고, 씬의 UI 오브젝트만 새로 만듭니다.
    /// 메뉴: DinoCleaner/UI/Build Game UI, DinoCleaner/UI/Rebuild All UI Prefabs (Overwrite)
    /// </summary>
    public static class GameUIBuilder
    {
        private const string PrefabRoot = "Assets/5. UI";
        private const string PrefabDir = "Assets/5. UI/Prefabs";
        private const string HomeScenePath = "Assets/1. Scenes/동우/Home.unity";
        private const string StageScenePath = "Assets/1. Scenes/동우/Stage.unity";
        private const string AddressableGroupName = "UI";

        private static readonly Color ScreenColor = new Color(0.93f, 0.93f, 0.93f);
        private static readonly Color PanelColor = Color.white;
        private static readonly Color BarFillColor = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color PlaceholderColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color AccentColor = new Color(0f, 0.69f, 0.94f);
        private static readonly Color SelectedColor = new Color(1f, 0.75f, 0f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color TextColor = new Color(0.1f, 0.1f, 0.1f);

        // 씬을 다시 구성할 때 지우는 오브젝트. 기존 임시 UI(Dev~)도 포함합니다.
        private static readonly string[] SceneUIObjects =
        {
            "DevHomeScreen", "DevProgressHUD",
            "EventSystem", "Canvas_FullScreen", "Canvas_Popup", "Canvas_Top", "UILoader",
            "HomeUIController", "StageUIController", "DevPlayerHudInput",
        };

        private static Font font;
        private static Sprite panelSprite;
        private static Sprite knobSprite;
        private static DefaultControls.Resources controlResources;
        private static bool overwriteExistingPrefabs;

        [MenuItem("DinoCleaner/UI/Build Game UI")]
        public static void Build()
        {
            Run(false);
        }

        [MenuItem("DinoCleaner/UI/Rebuild All UI Prefabs (Overwrite)")]
        public static void RebuildPrefabs()
        {
            bool isConfirmed = EditorUtility.DisplayDialog("UI 프리팹 덮어쓰기",
                "UI 프리팹 10개를 새로 만듭니다. 프리팹에 직접 넣은 Sprite와 수정 사항이 모두 사라집니다.",
                "덮어쓰기", "취소");
            if (isConfirmed) Run(true);
        }

        private static void Run(bool overwritePrefabs)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[GameUI] Play 모드를 끈 뒤 실행하세요.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LoadResources();
            overwriteExistingPrefabs = overwritePrefabs;

            SaveAndRegister(BuildLobby(), UIName.Lobby);
            SaveAndRegister(BuildRequestList(), UIName.RequestList);
            SaveAndRegister(BuildRequestDetail(), UIName.RequestDetail);
            SaveAndRegister(BuildShop(), UIName.Shop);
            SaveAndRegister(BuildHud(), UIName.Hud);
            SaveAndRegister(BuildSettlement(), UIName.Settlement);
            SaveAndRegister(BuildSettings(), UIName.Settings);
            SaveAndRegister(BuildConfirmPopup(), UIName.ConfirmPopup);
            SaveAndRegister(BuildProgressDetail(), UIName.ProgressDetail);
            SaveAndRegister(BuildTutorial(), UIName.Tutorial);
            AssetDatabase.SaveAssets();

            SetupStageScene();
            SetupHomeScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(HomeScenePath, true),
                new EditorBuildSettingsScene(StageScenePath, true),
            };

            Debug.Log($"[GameUI] UI 생성 완료: {PrefabDir}, {HomeScenePath}, {StageScenePath}");
        }

        private static void LoadResources()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            controlResources = new DefaultControls.Resources
            {
                standard = panelSprite,
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = knobSprite,
            };
        }

        #region Prefabs - Home

        private static GameObject BuildLobby()
        {
            var root = CreateUIRoot(UIName.Lobby);

            AddPanel(root, "Logo", new Vector2(0, 310), new Vector2(760, 210), PanelColor);
            AddText(root, "LogoText", "DinoCleaner", 72, new Vector2(0, 310), new Vector2(740, 190));

            var request = AddButton(root, "Btn_Request", "의뢰", new Vector2(0, 90), new Vector2(520, 110));
            var shop = AddButton(root, "Btn_Shop", "상점", new Vector2(0, -40), new Vector2(520, 110));
            var settings = AddButton(root, "Btn_Settings", "설정", new Vector2(0, -170), new Vector2(520, 110));
            var quit = AddButton(root, "Btn_Quit", "게임 종료", new Vector2(0, -300), new Vector2(520, 110));

            Bind(root.AddComponent<UI_Lobby>(),
                ("requestButton", request), ("shopButton", shop), ("settingsButton", settings), ("quitButton", quit));
            return root;
        }

        private static GameObject BuildRequestList()
        {
            var root = CreateUIRoot(UIName.RequestList);
            AddImage(root, ScreenColor);

            var back = AddButton(root, "Btn_Back", "◀ 뒤로", new Vector2(-790, 460), new Vector2(250, 90));
            var settings = AddButton(root, "Btn_Settings", "설정", new Vector2(790, 460), new Vector2(250, 90));

            var content = AddScroll(root, "Scroll", new Vector2(0, 40), new Vector2(1760, 640), true, new Vector2(0, -380));
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddText(root, "Hint", "◀  마우스 휠 / 좌우 드래그  ▶", 30, new Vector2(0, -320), new Vector2(800, 60));

            var card = BuildRequestCardTemplate(content);
            Bind(root.AddComponent<UI_RequestList>(),
                ("backButton", back), ("settingsButton", settings), ("cardTemplate", card));
            return root;
        }

        private static UI_RequestCard BuildRequestCardTemplate(RectTransform content)
        {
            var cardGo = CreateChild(content.gameObject, "CardTemplate");
            ((RectTransform)cardGo.transform).sizeDelta = new Vector2(460, 580);
            var image = AddPanelImage(cardGo, PanelColor);
            var button = cardGo.AddComponent<Button>();
            button.targetGraphic = image;

            var dinoImage = AddPanel(cardGo, "DinoImage", new Vector2(0, 70), new Vector2(380, 380), PlaceholderColor);
            dinoImage.raycastTarget = false;
            var placeholder = AddText(cardGo, "ImagePlaceholder", "공룡 이미지", 30, new Vector2(0, 70), new Vector2(360, 80));
            placeholder.raycastTarget = false;
            var nameText = AddText(cardGo, "Name", "공룡", 40, new Vector2(0, -210), new Vector2(420, 90));
            nameText.raycastTarget = false;

            var card = cardGo.AddComponent<UI_RequestCard>();
            Bind(card, ("button", button), ("dinoImage", dinoImage), ("imagePlaceholderText", placeholder), ("nameText", nameText));
            cardGo.SetActive(false);
            return card;
        }

        private static GameObject BuildRequestDetail()
        {
            var root = CreateUIRoot(UIName.RequestDetail);
            AddImage(root, ScreenColor);

            var back = AddButton(root, "Btn_Back", "◀ 뒤로", new Vector2(-790, 460), new Vector2(250, 90));
            var panel = AddPanel(root, "Panel", new Vector2(0, -50), new Vector2(1500, 840), PanelColor).gameObject;

            var dinoImage = AddPanel(panel, "DinoImage", new Vector2(-400, 40), new Vector2(580, 620), PlaceholderColor);
            var placeholder = AddText(panel, "ImagePlaceholder", "공룡 이미지", 34, new Vector2(-400, 40), new Vector2(500, 90));
            var nameText = AddText(panel, "Name", "공룡 이름", 48, new Vector2(330, 320), new Vector2(740, 100));
            var description = AddText(panel, "Description", "", 34, new Vector2(330, 30), new Vector2(740, 440));
            description.alignment = TextAnchor.UpperLeft;
            var start = AddButton(panel, "Btn_Start", "청소 시작", new Vector2(470, -320), new Vector2(460, 120));

            Bind(root.AddComponent<UI_RequestDetail>(),
                ("backButton", back), ("startButton", start), ("dinoImage", dinoImage),
                ("imagePlaceholderText", placeholder), ("nameText", nameText), ("descriptionText", description));
            return root;
        }

        private static GameObject BuildShop()
        {
            var root = CreateUIRoot(UIName.Shop);
            AddImage(root, ScreenColor);

            var back = AddButton(root, "Btn_Back", "◀ 뒤로", new Vector2(-790, 460), new Vector2(250, 90));
            AddPanel(root, "MoneyPanel", new Vector2(620, 460), new Vector2(600, 90), PanelColor);
            var money = AddText(root, "Money", "보유 돈 : 0", 36, new Vector2(620, 460), new Vector2(580, 80));

            var content = AddScroll(root, "Scroll", new Vector2(-330, -60), new Vector2(1160, 800), false, null);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(550, 340);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(10, 10, 10, 10);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var card = BuildUpgradeCardTemplate(content);

            var detail = AddPanel(root, "Detail", new Vector2(610, -60), new Vector2(560, 800), PanelColor).gameObject;
            var detailName = AddText(detail, "Name", "수압", 54, new Vector2(0, 320), new Vector2(500, 90));
            var detailLevel = AddText(detail, "Level", "Lv. 1", 34, new Vector2(0, 245), new Vector2(500, 60));
            var detailDots = AddLevelDots(detail, "Dots", new Vector2(0, 165), new Vector2(480, 70));
            var detailDescription = AddText(detail, "Description", "", 32, new Vector2(0, -20), new Vector2(490, 260));
            var detailCost = AddText(detail, "Cost", "비용 : 100", 36, new Vector2(0, -210), new Vector2(490, 60));
            var upgrade = AddButton(detail, "Btn_Upgrade", "업그레이드", new Vector2(0, -310), new Vector2(460, 110));
            upgrade.GetComponent<Image>().color = AccentColor;

            Bind(root.AddComponent<UI_Shop>(),
                ("backButton", back), ("moneyText", money), ("cardTemplate", card),
                ("detailNameText", detailName), ("detailLevelText", detailLevel), ("detailDots", detailDots),
                ("detailDescriptionText", detailDescription), ("detailCostText", detailCost), ("upgradeButton", upgrade));
            return root;
        }

        private static UI_UpgradeCard BuildUpgradeCardTemplate(RectTransform content)
        {
            var cardGo = CreateChild(content.gameObject, "CardTemplate");

            var outline = CreateChild(cardGo, "SelectedOutline");
            Stretch(outline, -8f);
            AddPanelImage(outline, SelectedColor).raycastTarget = false;

            var body = CreateChild(cardGo, "Body");
            Stretch(body, 0f);
            var bodyImage = AddPanelImage(body, PanelColor);
            var button = cardGo.AddComponent<Button>();
            button.targetGraphic = bodyImage;

            var nameText = AddText(body, "Name", "수압", 40, new Vector2(0, 100), new Vector2(500, 70));
            var level = AddText(body, "Level", "Lv. 1", 30, new Vector2(0, 40), new Vector2(500, 50));
            var dots = AddLevelDots(body, "Dots", new Vector2(0, -70), new Vector2(480, 70));

            var card = cardGo.AddComponent<UI_UpgradeCard>();
            Bind(card, ("button", button), ("nameText", nameText), ("levelText", level),
                ("levelDots", dots), ("selectedOutline", outline));
            cardGo.SetActive(false);
            return card;
        }

        #endregion

        #region Prefabs - Stage

        private static GameObject BuildHud()
        {
            var root = CreateUIRoot(UIName.Hud);

            // 좌상단 진행도
            var panel = AddPanel(root, "ProgressPanel", Vector2.zero, Vector2.zero, PanelColor).gameObject;
            Anchor(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(660, 250));
            var region = AddText(panel, "CurrentRegion", "현재 부위 : -", 34, new Vector2(0, 75), new Vector2(620, 60));
            region.alignment = TextAnchor.MiddleLeft;
            AddBarRow(panel, "부위 진행도", "RegionBar", 0f, out var regionBar);
            AddBarRow(panel, "전체 진행도", "TotalBar", -75f, out var totalBar);

            // 중앙 에임
            var aim = CreateChild(root, "Aim");
            var aimRect = (RectTransform)aim.transform;
            aimRect.sizeDelta = new Vector2(400, 400);
            var firstBracket = AddIconOrText(aim, "FirstBracket", "(", 100, new Vector2(-44, 0), new Vector2(120, 160));
            var secondBracket = AddIconOrText(aim, "SecondBracket", ")", 100, new Vector2(44, 0), new Vector2(120, 160));
            var dot = AddIconOrText(aim, "Dot", "●", 34, Vector2.zero, new Vector2(80, 80));

            // 우하단 상태
            var posturePanel = AddPanel(root, "PosturePanel", Vector2.zero, Vector2.zero, PanelColor).gameObject;
            Anchor(posturePanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-340, 30), new Vector2(150, 150));
            var posture = AddIconOrText(posturePanel, "Posture", "자세\n서기", 32, Vector2.zero, new Vector2(130, 130));

            var toolPanel = AddPanel(root, "ToolPanel", Vector2.zero, Vector2.zero, PanelColor).gameObject;
            Anchor(toolPanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 30), new Vector2(150, 150));
            var tool = AddIconOrText(toolPanel, "Tool", "물", 40, Vector2.zero, new Vector2(130, 130));

            var nozzle = CreateChild(root, "Nozzle");
            Anchor(nozzle, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(140, 140));
            var nozzleIcon = AddImage(nozzle, Color.red);
            nozzleIcon.sprite = knobSprite;
            var nozzleNumber = AddText(nozzle, "Number", "1", 52, Vector2.zero, new Vector2(120, 120));

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            Bind(root.AddComponent<UI_HUD>(),
                ("currentRegionText", region), ("regionBar", regionBar), ("totalBar", totalBar),
                ("aimRoot", aimRect), ("firstBracket", firstBracket), ("secondBracket", secondBracket),
                ("aimDot", dot), ("postureIcon", posture), ("toolIcon", tool),
                ("nozzleIcon", nozzleIcon), ("nozzleNumberText", nozzleNumber));
            return root;
        }

        private static void AddBarRow(GameObject panel, string label, string barName, float y, out UI_ProgressBar bar)
        {
            var text = AddText(panel, barName + "Label", label, 28, new Vector2(-210, y), new Vector2(200, 50));
            text.alignment = TextAnchor.MiddleLeft;
            bar = AddProgressBar(panel, barName, new Vector2(170, y), new Vector2(240, 40), 90f);
        }

        private static GameObject BuildSettlement()
        {
            var root = CreateUIRoot(UIName.Settlement);
            AddImage(root, Color.white);

            AddPanel(root, "CompletePanel", new Vector2(0, 230), new Vector2(660, 430), PanelColor);
            AddText(root, "CompleteText", "완료~!", 90, new Vector2(0, 230), new Vector2(620, 400));

            AddPanel(root, "SummaryPanel", new Vector2(0, -120), new Vector2(760, 110), PanelColor);
            var summary = AddText(root, "Summary", "수입 : 0    보유액 : 0", 40, new Vector2(0, -120), new Vector2(740, 100));

            var stay = AddButton(root, "Btn_Stay", "남기", new Vector2(-270, -340), new Vector2(460, 120));
            var home = AddButton(root, "Btn_Home", "집으로 돌아가기", new Vector2(270, -340), new Vector2(460, 120));

            Bind(root.AddComponent<UI_Settlement>(), ("summaryText", summary), ("stayButton", stay), ("homeButton", home));
            return root;
        }

        private static GameObject BuildProgressDetail()
        {
            var root = BuildPopupRoot(UIName.ProgressDetail, new Vector2(1320, 900), false, out var window, out var dim);

            AddText(window, "Title", "부위별 진행도", 50, new Vector2(0, 385), new Vector2(1000, 90));
            var measuring = AddText(window, "Measuring", "진행도 측정 중...", 36, new Vector2(0, 40), new Vector2(800, 80));

            var rows = CreateChild(window, "Rows");
            var rowsRect = (RectTransform)rows.transform;
            rowsRect.anchoredPosition = new Vector2(0, 30);
            rowsRect.sizeDelta = new Vector2(1180, 560);
            var layout = rows.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var rowGo = CreateChild(rows, "RowTemplate");
            ((RectTransform)rowGo.transform).sizeDelta = new Vector2(1180, 70);
            var rowName = AddText(rowGo, "Name", "부위", 34, new Vector2(-470, 0), new Vector2(220, 60));
            rowName.alignment = TextAnchor.MiddleLeft;
            var rowBar = AddProgressBar(rowGo, "Bar", new Vector2(150, 0), new Vector2(780, 44), 90f);
            var row = rowGo.AddComponent<UI_ProgressDetailRow>();
            Bind(row, ("nameText", rowName), ("bar", rowBar));
            rowGo.SetActive(false);

            var totalLabel = AddText(window, "TotalLabel", "전체", 38, new Vector2(-470, -340), new Vector2(220, 70));
            totalLabel.alignment = TextAnchor.MiddleLeft;
            totalLabel.fontStyle = FontStyle.Bold;
            var totalBar = AddProgressBar(window, "TotalBar", new Vector2(150, -340), new Vector2(780, 50), 90f);

            var comp = root.AddComponent<UI_ProgressDetail>();
            Bind(comp, ("bgCanvasGroup", dim), ("windowRect", window.transform),
                ("rowTemplate", row), ("totalBar", totalBar), ("measuringLabel", measuring.gameObject));
            return root;
        }

        private static GameObject BuildTutorial()
        {
            var root = CreateUIRoot(UIName.Tutorial);

            var panel = AddPanel(root, "Panel", Vector2.zero, Vector2.zero, PanelColor).gameObject;
            Anchor(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -300), new Vector2(420, 330));
            var title = AddText(panel, "Title", "( ! ) Tutorial", 34, new Vector2(0, 110), new Vector2(380, 60));
            title.alignment = TextAnchor.MiddleLeft;
            var message = AddText(panel, "Message", "", 32, new Vector2(0, -40), new Vector2(380, 220));
            message.alignment = TextAnchor.UpperLeft;

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            Bind(root.AddComponent<UI_Tutorial>(), ("messageText", message));
            return root;
        }

        #endregion

        #region Prefabs - Common popups

        private static GameObject BuildSettings()
        {
            var root = BuildPopupRoot(UIName.Settings, new Vector2(960, 880), true, out var window, out var dim);

            AddText(window, "Title", "설정", 52, new Vector2(0, 370), new Vector2(500, 90));
            var close = AddButton(window, "Btn_Close", "X", new Vector2(400, 370), new Vector2(90, 90));

            AddSliderRow(window, "Bgm", "BGM", 210f, out var bgm, out var bgmValue);
            AddSliderRow(window, "Sfx", "효과음", 70f, out var sfx, out var sfxValue);
            AddSliderRow(window, "Sensitivity", "마우스 감도", -70f, out var sensitivity, out var sensitivityValue);

            var inGame = CreateChild(window, "InGameButtons");
            var inGameRect = (RectTransform)inGame.transform;
            inGameRect.anchoredPosition = new Vector2(0, -290);
            inGameRect.sizeDelta = new Vector2(860, 130);
            var save = AddButton(inGame, "Btn_Save", "저장하기", new Vector2(-220, 0), new Vector2(400, 120));
            var returnHome = AddButton(inGame, "Btn_Return", "돌아가기", new Vector2(220, 0), new Vector2(400, 120));

            var comp = root.AddComponent<UI_Settings>();
            Bind(comp, ("bgCanvasGroup", dim), ("windowRect", window.transform),
                ("bgmSlider", bgm), ("bgmValueText", bgmValue),
                ("sfxSlider", sfx), ("sfxValueText", sfxValue),
                ("sensitivitySlider", sensitivity), ("sensitivityValueText", sensitivityValue),
                ("closeButton", close), ("inGameButtons", inGame), ("saveButton", save), ("returnButton", returnHome));
            return root;
        }

        private static void AddSliderRow(GameObject window, string name, string label, float y, out Slider slider, out Text value)
        {
            var labelText = AddText(window, name + "Label", label, 36, new Vector2(-280, y), new Vector2(300, 70));
            labelText.alignment = TextAnchor.MiddleLeft;
            value = AddText(window, name + "Value", "0", 34, new Vector2(-70, y), new Vector2(100, 70));

            var go = DefaultControls.CreateSlider(controlResources);
            go.name = name + "Slider";
            go.transform.SetParent(window.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = new Vector2(210, y);
            rect.sizeDelta = new Vector2(420, 40);

            slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
        }

        private static GameObject BuildConfirmPopup()
        {
            var root = BuildPopupRoot(UIName.ConfirmPopup, new Vector2(800, 460), true, out var window, out var dim);

            var message = AddText(window, "Message", "", 38, new Vector2(0, 70), new Vector2(720, 220));
            var confirm = AddButton(window, "Btn_Confirm", "확인", new Vector2(-170, -140), new Vector2(280, 100));
            var cancel = AddButton(window, "Btn_Cancel", "취소", new Vector2(170, -140), new Vector2(280, 100));

            var comp = root.AddComponent<UI_ConfirmPopup>();
            Bind(comp, ("bgCanvasGroup", dim), ("windowRect", window.transform),
                ("messageText", message), ("confirmButton", confirm), ("cancelButton", cancel));
            return root;
        }

        private static GameObject BuildPopupRoot(string name, Vector2 windowSize, bool isModal, out GameObject window, out CanvasGroup dimGroup)
        {
            var root = CreateUIRoot(name);

            var dim = CreateChild(root, "Dim");
            Stretch(dim, 0f);
            var dimImage = AddImage(dim, isModal ? DimColor : new Color(0f, 0f, 0f, 0.2f));
            dimImage.raycastTarget = isModal;
            dimGroup = dim.AddComponent<CanvasGroup>();
            dimGroup.blocksRaycasts = isModal;

            window = AddPanel(root, "Window", Vector2.zero, windowSize, PanelColor).gameObject;
            return root;
        }

        #endregion

        #region Assets

        private static void SaveAndRegister(GameObject root, string address)
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder(PrefabRoot, "Prefabs");
            }
            string path = $"{PrefabDir}/{address}.prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            if (!exists || overwriteExistingPrefabs)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            Object.DestroyImmediate(root);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(AddressableGroupName)
                        ?? settings.CreateGroup(AddressableGroupName, false, false, true, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, false, false);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
        }

        #endregion

        #region Scenes

        private static void SetupHomeScene()
        {
            var scene = OpenAndClearScene(HomeScenePath);
            CreateSceneUI(new[] { UIName.Lobby });

            var controller = new GameObject("HomeUIController").AddComponent<HomeUIController>();
            var so = new SerializedObject(controller);
            var stages = so.FindProperty("stages");
            List<StageDefinition> assets = AssetDatabase.FindAssets("t:StageDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<StageDefinition>)
                .ToList();
            stages.arraySize = assets.Count;
            for (int i = 0; i < assets.Count; i++)
            {
                stages.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupStageScene()
        {
            var scene = OpenAndClearScene(StageScenePath);
            CreateSceneUI(new string[0]);

            new GameObject("StageUIController").AddComponent<StageUIController>();
            new GameObject("DevPlayerHudInput").AddComponent<DevPlayerHudInput>();

            EditorSceneManager.SaveScene(scene);
        }

        private static Scene OpenAndClearScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
            {
                if (SceneUIObjects.Contains(go.name)) Object.DestroyImmediate(go);
            }
            return scene;
        }

        private static void CreateSceneUI(string[] awakeUI)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasFull = CreateCanvas("Canvas_FullScreen", 0);
            var canvasPopup = CreateCanvas("Canvas_Popup", 5);
            var canvasTop = CreateCanvas("Canvas_Top", 10);

            var loader = new GameObject("UILoader").AddComponent<UILoader>();
            var so = new SerializedObject(loader);
            so.FindProperty("canvas_FullScreen").objectReferenceValue = canvasFull;
            so.FindProperty("canvas_Popup").objectReferenceValue = canvasPopup;
            so.FindProperty("canvas_Top").objectReferenceValue = canvasTop;
            var list = so.FindProperty("awakeUIList");
            list.arraySize = awakeUI.Length;
            for (int i = 0; i < awakeUI.Length; i++)
            {
                list.GetArrayElementAtIndex(i).stringValue = awakeUI[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        #endregion

        #region UI helpers

        private static void Bind(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var property = so.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[GameUI] {target.GetType().Name}에 {field} 필드가 없습니다.");
                    continue;
                }
                property.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUIRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Stretch(go, 0f);
            return go;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // 부모에 맞춰 늘립니다. inset이 음수면 부모보다 커집니다.
        private static void Stretch(GameObject go, float inset)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Anchor(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image AddPanelImage(GameObject go, Color color)
        {
            var image = AddImage(go, color);
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        private static Image AddPanel(GameObject parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return AddPanelImage(go, color);
        }

        private static Text AddText(GameObject parent, string name, string content, int size, Vector2 pos, Vector2 boxSize)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;

            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            return text;
        }

        private static UI_IconOrText AddIconOrText(GameObject parent, string name, string fallback, int fontSize, Vector2 pos, Vector2 size)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var iconGo = CreateChild(go, "Icon");
            Stretch(iconGo, 0f);
            var icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            var text = AddText(go, "FallbackText", fallback, fontSize, Vector2.zero, size);

            var comp = go.AddComponent<UI_IconOrText>();
            Bind(comp, ("icon", icon), ("fallbackText", text));
            return comp;
        }

        private static Button AddButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var image = AddPanel(parent, name, pos, size, PanelColor);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = AddText(image.gameObject, "Label", label, 36, Vector2.zero, size);
            text.raycastTarget = false;
            return button;
        }

        private static UI_ProgressBar AddProgressBar(GameObject parent, string name, Vector2 pos, Vector2 size, float percentWidth)
        {
            var back = AddPanel(parent, name, pos, size, PlaceholderColor);

            var fill = CreateChild(back.gameObject, "Fill");
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            AddImage(fill, BarFillColor);

            float percentX = -size.x * 0.5f - percentWidth * 0.5f;
            var percent = AddText(back.gameObject, "Percent", "0%", 28, new Vector2(percentX, 0), new Vector2(percentWidth, size.y));

            var bar = back.gameObject.AddComponent<UI_ProgressBar>();
            Bind(bar, ("fillRect", fillRect), ("percentText", percent));
            return bar;
        }

        private static UI_LevelDots AddLevelDots(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var dotGo = CreateChild(go, "DotTemplate");
            ((RectTransform)dotGo.transform).sizeDelta = new Vector2(60, 60);
            var dotImage = AddImage(dotGo, Color.white);
            dotImage.sprite = knobSprite;
            dotImage.raycastTarget = false;
            dotGo.SetActive(false);

            var dots = go.AddComponent<UI_LevelDots>();
            Bind(dots, ("dotTemplate", dotImage));
            return dots;
        }

        // ScrollRect를 만들고 카드를 넣을 Content를 돌려줍니다. scrollbarPos가 있으면 가로 스크롤바를 답니다.
        private static RectTransform AddScroll(GameObject parent, string name, Vector2 pos, Vector2 size, bool isHorizontal, Vector2? scrollbarPos)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            AddImage(go, new Color(1f, 1f, 1f, 0f));

            var viewport = CreateChild(go, "Viewport");
            Stretch(viewport, 0f);
            viewport.AddComponent<RectMask2D>();

            var content = CreateChild(viewport, "Content");
            var contentRect = (RectTransform)content.transform;
            if (isHorizontal)
            {
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = new Vector2(0f, 1f);
                contentRect.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = Vector2.one;
                contentRect.pivot = new Vector2(0.5f, 1f);
            }
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRect;
            scroll.horizontal = isHorizontal;
            scroll.vertical = !isHorizontal;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;

            if (scrollbarPos.HasValue)
            {
                var barGo = DefaultControls.CreateScrollbar(controlResources);
                barGo.name = "Scrollbar";
                barGo.transform.SetParent(parent.transform, false);
                var barRect = (RectTransform)barGo.transform;
                barRect.anchoredPosition = scrollbarPos.Value;
                barRect.sizeDelta = new Vector2(1640, 30);

                scroll.horizontalScrollbar = barGo.GetComponent<Scrollbar>();
                scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            }
            return contentRect;
        }

        #endregion
    }
}
