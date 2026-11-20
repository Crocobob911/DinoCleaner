using System.Collections.Generic;
using System.Linq;
using DinoCleaner.Core;
using DinoCleaner.Tutorial;
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
    /// Sprite와 폰트도 여기서 지정하므로, 프리팹을 덮어써도 그래픽이 유지됩니다. 그래픽을 바꿀 때는 Theme 영역을 고칩니다.
    /// 메뉴: DinoCleaner/UI/Build Game UI, DinoCleaner/UI/Rebuild All UI Prefabs (Overwrite)
    /// </summary>
    public static class GameUIBuilder
    {
        private const string PrefabRoot = "Assets/5. UI";
        private const string PrefabDir = "Assets/5. UI/Prefabs";
        private const string HomeScenePath = "Assets/1. Scenes/동우/Home.unity";
        private const string StageScenePath = "Assets/1. Scenes/동우/Stage.unity";
        private const string PlayerPrefabPath = "Assets/3. Prefabs/Player.prefab";
        private const string AddressableGroupName = "UI";

        // 씬을 다시 구성할 때 지우는 오브젝트. 기존 임시 UI(Dev~)도 포함합니다.
        private static readonly string[] SceneUIObjects =
        {
            "DevHomeScreen", "DevProgressHUD",
            "EventSystem", "Canvas_FullScreen", "Canvas_Popup", "Canvas_Top", "UILoader",
            "HomeUIController", "StageUIController", "DevPlayerHudInput", "PlayerHudBridge",
        };

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
                "UI 프리팹 10개를 새로 만듭니다. 빌더 밖에서 프리팹에 직접 고친 내용은 사라집니다. (빌더가 지정하는 Sprite와 폰트는 다시 들어갑니다)",
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

            Theme.Load();
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

        #region Theme

        /// <summary>
        /// UI에 쓰는 폰트, Sprite, 색. 쓰는 PNG는 Load에서 Sprite로 가져오기 설정과 9-slice 경계를 지정합니다.
        /// </summary>
        private static class Theme
        {
            private const string RpgDir = "Assets/5. UI/Sprite/kenney_ui-pack-rpg-expansion/PNG/";
            private const string IconDir = "Assets/5. UI/Sprite/kenney_game-icons/PNG/White/2x/";
            private const string BorderDir = "Assets/5. UI/Sprite/kenney_fantasy-ui-borders/PNG/Default/";
            // 팩에 없는 그림. 에임 괄호는 호의 세로 중심이 이미지 정중앙이 되도록 직접 그렸습니다.
            private const string CustomDir = "Assets/5. UI/Sprite/Custom/";
            private const string BodyFontPath = "Assets/5. UI/Font/Luckiest_Guy/LuckiestGuy-Regular.ttf";
            private const string LogoFontPath = "Assets/5. UI/Font/stone-age-font/StoneAge-8MPD.ttf";

            // 9-slice 경계 (왼쪽, 아래, 오른쪽, 위). Kenney 이미지의 모서리와 그림자 두께에 맞춘 값입니다.
            private static readonly Vector4 PanelBorder = new Vector4(10, 10, 10, 10);
            private static readonly Vector4 ButtonBorder = new Vector4(10, 12, 10, 8);
            private static readonly Vector4 PressedBorder = new Vector4(10, 8, 10, 8); // 눌린 버튼은 아래 그림자가 얇음
            private static readonly Vector4 BarBorder = new Vector4(0, 4, 0, 8);
            private static readonly Vector4 RoundBorder = new Vector4(8, 8, 8, 8);
            private static readonly Vector4 OutlineBorder = new Vector4(14, 14, 14, 14);

            public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
            public static readonly Color TextDark = new Color(0.27f, 0.17f, 0.09f);
            public static readonly Color TextLight = new Color(0.98f, 0.94f, 0.85f);
            public static readonly Color SelectedColor = new Color(1f, 0.78f, 0.1f);
            public static readonly Color LogoColor = new Color(1f, 0.86f, 0.45f);
            public static readonly Color ProgressBadgeColor = new Color(0.35f, 0.65f, 1f); // 회색 원 Sprite에 곱해 파란 배지로 만듦

            public static Font BodyFont;
            public static Font LogoFont;

            public static Sprite Window;      // 창, 패널
            public static Sprite Card;        // 카드
            public static Sprite Inset;       // 막대 배경, 이미지 자리
            public static Sprite Board;       // 전체 화면 배경, 로비 메뉴판 (갈색 나무판)
            public static Sprite Button;      // 기본 버튼 (갈색)
            public static Sprite ButtonPressed;
            public static Sprite AccentButton; // 강조 버튼 (베이지)
            public static Sprite AccentButtonPressed;
            public static Sprite ButtonDisabled;
            public static Sprite AccentButtonDisabled;
            public static Sprite SquareButton;
            public static Sprite SquareButtonPressed;
            public static Sprite SquareButtonDisabled;
            public static Sprite ArrowLeft;
            public static Sprite ArrowRight;
            public static Sprite BarFill;
            public static Sprite SliderFill;
            public static Sprite Round;       // 흰 원. 색을 곱해 노즐, 단계 점에 사용
            public static Sprite RoundReached;
            public static Sprite Knob;
            public static Sprite SelectedOutline;

            public static Sprite IconBack, IconSettings, IconClose, IconHome, IconShop, IconRequest, IconQuit,
                IconSave, IconTutorial, IconCheck, IconTrophy, IconWaterAim, IconFoamAim,
                AimBracketLeft, AimBracketRight;

            public static DefaultControls.Resources ControlResources;

            public static void Load()
            {
                BodyFont = LoadFont(BodyFontPath);
                LogoFont = LoadFont(LogoFontPath);

                Window = LoadSprite(RpgDir + "panel_beigeLight.png", PanelBorder);
                Card = LoadSprite(RpgDir + "panel_beige.png", PanelBorder);
                Inset = LoadSprite(RpgDir + "panelInset_brown.png", PanelBorder);
                Board = LoadSprite(RpgDir + "panel_brown.png", PanelBorder);
                Button = LoadSprite(RpgDir + "buttonLong_brown.png", ButtonBorder);
                ButtonPressed = LoadSprite(RpgDir + "buttonLong_brown_pressed.png", PressedBorder);
                AccentButton = LoadSprite(RpgDir + "buttonLong_beige.png", ButtonBorder);
                AccentButtonPressed = LoadSprite(RpgDir + "buttonLong_beige_pressed.png", PressedBorder);
                // 밝은 글자를 쓰는 갈색 버튼은 진한 청회색, 어두운 글자를 쓰는 베이지 버튼은 밝은 회색으로 비활성 표시
                ButtonDisabled = LoadSprite(RpgDir + "buttonLong_blue.png", ButtonBorder);
                AccentButtonDisabled = LoadSprite(RpgDir + "buttonLong_grey.png", ButtonBorder);
                SquareButton = LoadSprite(RpgDir + "buttonSquare_brown.png", ButtonBorder);
                SquareButtonPressed = LoadSprite(RpgDir + "buttonSquare_brown_pressed.png", PressedBorder);
                SquareButtonDisabled = LoadSprite(RpgDir + "buttonSquare_blue.png", ButtonBorder);
                ArrowLeft = LoadSprite(RpgDir + "arrowBeige_left.png");
                ArrowRight = LoadSprite(RpgDir + "arrowBeige_right.png");
                BarFill = LoadSprite(RpgDir + "barGreen_horizontalMid.png", BarBorder);
                SliderFill = LoadSprite(RpgDir + "barYellow_horizontalMid.png", BarBorder);
                Round = LoadSprite(RpgDir + "buttonRound_grey.png", RoundBorder);
                RoundReached = LoadSprite(RpgDir + "buttonRound_brown.png", RoundBorder);
                Knob = LoadSprite(RpgDir + "buttonRound_beige.png", RoundBorder);
                SelectedOutline = LoadSprite(BorderDir + "Border/panel-border-015.png", OutlineBorder, FilterMode.Point);

                IconBack = LoadSprite(IconDir + "arrowLeft.png");
                IconSettings = LoadSprite(IconDir + "gear.png");
                IconClose = LoadSprite(IconDir + "cross.png");
                IconHome = LoadSprite(IconDir + "home.png");
                IconShop = LoadSprite(IconDir + "shoppingCart.png");
                IconRequest = LoadSprite(IconDir + "menuList.png");
                IconQuit = LoadSprite(IconDir + "power.png");
                IconSave = LoadSprite(IconDir + "save.png");
                IconTutorial = LoadSprite(IconDir + "exclamation.png");
                IconCheck = LoadSprite(IconDir + "checkmark.png");
                IconTrophy = LoadSprite(IconDir + "trophy.png");
                IconWaterAim = LoadSprite(IconDir + "target.png");
                IconFoamAim = Round;
                AimBracketLeft = LoadSprite(CustomDir + "aim_bracket_left.png");
                AimBracketRight = LoadSprite(CustomDir + "aim_bracket_right.png");

                // DefaultControls: 슬라이더 배경·스크롤바 배경 = background, 슬라이더 Fill·스크롤바 손잡이 = standard, 슬라이더 손잡이 = knob
                ControlResources = new DefaultControls.Resources
                {
                    standard = SliderFill,
                    background = Inset,
                    knob = Knob,
                };
            }

            private static Font LoadFont(string path)
            {
                var loaded = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (loaded != null) return loaded;

                Debug.LogWarning($"[GameUI] 폰트를 찾지 못해 기본 폰트를 씁니다: {path}");
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // PNG를 Sprite로 가져오도록 설정한 뒤 불러옵니다. 설정이 이미 맞으면 다시 가져오지 않습니다.
            private static Sprite LoadSprite(string path, Vector4 border = default, FilterMode filter = FilterMode.Bilinear)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    Debug.LogError($"[GameUI] Sprite 파일이 없습니다: {path}");
                    return null;
                }

                bool needsReimport = importer.textureType != TextureImporterType.Sprite
                                     || importer.spriteImportMode != SpriteImportMode.Single
                                     || importer.spriteBorder != border
                                     || importer.mipmapEnabled
                                     || importer.filterMode != filter;
                if (needsReimport)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteBorder = border;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = filter;
                    importer.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }

        // 버튼 모양. 갈색은 밝은 글자, 베이지는 어두운 글자를 씁니다.
        private enum ButtonStyle
        {
            Brown,
            Beige,
            Square,
        }

        #endregion

        #region Prefabs - Home

        private static GameObject BuildLobby()
        {
            var root = CreateUIRoot(UIName.Lobby);

            // 로비 뒤는 3D 배경이 보이므로, 버튼 뒤에만 나무판을 둬 글자가 잘 보이게 합니다.
            AddPanel(root, "MenuBoard", new Vector2(0, -105), new Vector2(640, 590), Theme.Board).pixelsPerUnitMultiplier = 0.3f;

            var logo = AddText(root, "LogoText", "DinoCleaner", 150, new Vector2(0, 300), new Vector2(1400, 260), Theme.LogoColor);
            logo.font = Theme.LogoFont;
            AddShadow(logo.gameObject, new Vector2(6, -6));

            var request = AddButton(root, "Btn_Request", "REQUESTS", new Vector2(0, 90), new Vector2(520, 110), ButtonStyle.Brown, Theme.IconRequest);
            var shop = AddButton(root, "Btn_Shop", "SHOP", new Vector2(0, -40), new Vector2(520, 110), ButtonStyle.Brown, Theme.IconShop);
            var settings = AddButton(root, "Btn_Settings", "SETTINGS", new Vector2(0, -170), new Vector2(520, 110), ButtonStyle.Brown, Theme.IconSettings);
            var quit = AddButton(root, "Btn_Quit", "QUIT", new Vector2(0, -300), new Vector2(520, 110), ButtonStyle.Brown, Theme.IconQuit);

            Bind(root.AddComponent<UI_Lobby>(),
                ("requestButton", request), ("shopButton", shop), ("settingsButton", settings), ("quitButton", quit));
            return root;
        }

        private static GameObject BuildRequestList()
        {
            var root = CreateUIRoot(UIName.RequestList);
            AddScreenBackground(root);

            var back = AddButton(root, "Btn_Back", "BACK", new Vector2(-790, 460), new Vector2(250, 90), ButtonStyle.Brown, Theme.IconBack);
            var settings = AddButton(root, "Btn_Settings", "", new Vector2(870, 460), new Vector2(100, 100), ButtonStyle.Square, Theme.IconSettings);

            var content = AddScroll(root, "Scroll", new Vector2(0, 40), new Vector2(1760, 640), true, new Vector2(0, -380));
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddText(root, "Hint", "MOUSE WHEEL / DRAG", 30, new Vector2(0, -320), new Vector2(600, 60), Theme.TextLight);
            var prev = AddArrowButton(root, "Btn_Prev", Theme.ArrowLeft, new Vector2(-360, -320));
            var next = AddArrowButton(root, "Btn_Next", Theme.ArrowRight, new Vector2(360, -320));

            var card = BuildRequestCardTemplate(content);
            Bind(root.AddComponent<UI_RequestList>(),
                ("backButton", back), ("settingsButton", settings), ("cardTemplate", card),
                ("scrollRect", content.GetComponentInParent<ScrollRect>()), ("prevButton", prev), ("nextButton", next));
            return root;
        }

        private static UI_RequestCard BuildRequestCardTemplate(RectTransform content)
        {
            var cardGo = CreateChild(content.gameObject, "CardTemplate");
            ((RectTransform)cardGo.transform).sizeDelta = new Vector2(460, 580);
            var image = AddSlicedImage(cardGo, Theme.Card);
            var button = cardGo.AddComponent<Button>();
            button.targetGraphic = image;

            var dinoImage = AddPanel(cardGo, "DinoImage", new Vector2(0, 70), new Vector2(380, 380), Theme.Inset);
            dinoImage.raycastTarget = false;
            var placeholder = AddText(cardGo, "ImagePlaceholder", "DINO IMAGE", 30, new Vector2(0, 70), new Vector2(360, 80), Theme.TextLight);
            placeholder.raycastTarget = false;
            var nameText = AddText(cardGo, "Name", "Dino", 40, new Vector2(0, -210), new Vector2(420, 90));
            nameText.raycastTarget = false;

            var lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/5. UI/Sprite/kenney_game-icons/PNG/White/2x/locked.png");
            var lockIcon = AddIcon(cardGo, "LockIcon", lockSprite, Vector2.zero, new Vector2(160, 160), Color.white).gameObject;
            lockIcon.SetActive(false);

            // 중도 포기한 공룡의 진행도 배지 (카드 오른쪽 위)
            var progressBadge = AddIcon(cardGo, "ProgressBadge", Theme.Round, new Vector2(200, 260), new Vector2(120, 120), Theme.ProgressBadgeColor).gameObject;
            var progressText = AddText(progressBadge, "ProgressText", "0%", 36, Vector2.zero, new Vector2(100, 60), Theme.TextLight);
            progressText.raycastTarget = false;
            AddShadow(progressText.gameObject, new Vector2(2, -2));
            progressBadge.SetActive(false);

            AddHoverLift(cardGo, 16f, 1.03f);

            var card = cardGo.AddComponent<UI_RequestCard>();
            Bind(card, ("button", button), ("dinoImage", dinoImage), ("imagePlaceholderText", placeholder), ("nameText", nameText),
                ("lockIcon", lockIcon), ("progressBadge", progressBadge), ("progressText", progressText));
            cardGo.SetActive(false);
            return card;
        }

        private static GameObject BuildRequestDetail()
        {
            var root = CreateUIRoot(UIName.RequestDetail);
            AddScreenBackground(root);

            var back = AddButton(root, "Btn_Back", "BACK", new Vector2(-790, 460), new Vector2(250, 90), ButtonStyle.Brown, Theme.IconBack);
            var panel = AddPanel(root, "Panel", new Vector2(0, -50), new Vector2(1500, 840), Theme.Window).gameObject;

            var dinoImage = AddPanel(panel, "DinoImage", new Vector2(-400, 40), new Vector2(580, 620), Theme.Inset);
            var placeholder = AddText(panel, "ImagePlaceholder", "DINO IMAGE", 34, new Vector2(-400, 40), new Vector2(500, 90), Theme.TextLight);
            var nameText = AddText(panel, "Name", "Dino Name", 54, new Vector2(330, 320), new Vector2(740, 100));
            var description = AddText(panel, "Description", "", 34, new Vector2(330, 30), new Vector2(740, 440));
            description.alignment = TextAnchor.UpperLeft;
            description.resizeTextForBestFit = false;
            var start = AddButton(panel, "Btn_Start", "START CLEANING", new Vector2(470, -320), new Vector2(460, 120), ButtonStyle.Brown);
            // 중도 포기한 공룡의 전체 진행도 (청소 시작 버튼 왼쪽)
            var progressText = AddText(panel, "ProgressText", "0%", 54, new Vector2(140, -320), new Vector2(180, 100));
            progressText.gameObject.SetActive(false);

            Bind(root.AddComponent<UI_RequestDetail>(),
                ("backButton", back), ("startButton", start), ("dinoImage", dinoImage),
                ("imagePlaceholderText", placeholder), ("nameText", nameText), ("descriptionText", description),
                ("progressText", progressText));
            return root;
        }

        private static GameObject BuildShop()
        {
            var root = CreateUIRoot(UIName.Shop);
            AddScreenBackground(root);

            var back = AddButton(root, "Btn_Back", "BACK", new Vector2(-790, 460), new Vector2(250, 90), ButtonStyle.Brown, Theme.IconBack);
            AddPanel(root, "MoneyPanel", new Vector2(620, 460), new Vector2(600, 90), Theme.Window);
            var money = AddText(root, "Money", "MONEY : 0", 40, new Vector2(620, 460), new Vector2(580, 80));

            var content = AddScroll(root, "Scroll", new Vector2(-330, -60), new Vector2(1160, 800), false, null);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(550, 340);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(10, 10, 10, 10);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var card = BuildUpgradeCardTemplate(content);

            var detail = AddPanel(root, "Detail", new Vector2(610, -60), new Vector2(560, 800), Theme.Window).gameObject;
            var detailName = AddText(detail, "Name", "Pressure", 54, new Vector2(0, 320), new Vector2(500, 90));
            var detailLevel = AddText(detail, "Level", "Lv. 1", 36, new Vector2(0, 245), new Vector2(500, 60));
            var detailDots = AddLevelDots(detail, "Dots", new Vector2(0, 165), new Vector2(480, 70));
            var detailDescription = AddText(detail, "Description", "", 32, new Vector2(0, -20), new Vector2(490, 260));
            detailDescription.resizeTextForBestFit = false;
            var detailCost = AddText(detail, "Cost", "COST : 100", 38, new Vector2(0, -210), new Vector2(490, 60));
            var upgrade = AddButton(detail, "Btn_Upgrade", "UPGRADE", new Vector2(0, -310), new Vector2(460, 110), ButtonStyle.Brown);
            // 상점의 핵심 버튼이라 금색 글자에 그림자를 넣어 갈색 바탕에서 잘 보이게 합니다.
            var upgradeLabel = upgrade.transform.Find("Label").GetComponent<Text>();
            upgradeLabel.color = Theme.SelectedColor;
            upgradeLabel.fontSize = upgradeLabel.resizeTextMaxSize = 48;
            AddShadow(upgradeLabel.gameObject, new Vector2(3, -3));

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
            Stretch(outline, -10f);
            var outlineImage = AddSlicedImage(outline, Theme.SelectedOutline, 0.5f);
            outlineImage.color = Theme.SelectedColor;
            outlineImage.raycastTarget = false;

            var body = CreateChild(cardGo, "Body");
            Stretch(body, 0f);
            var bodyImage = AddSlicedImage(body, Theme.Card);
            var button = cardGo.AddComponent<Button>();
            button.targetGraphic = bodyImage;

            var nameText = AddText(body, "Name", "Pressure", 42, new Vector2(0, 100), new Vector2(500, 70));
            var level = AddText(body, "Level", "Lv. 1", 32, new Vector2(0, 40), new Vector2(500, 50));
            var dots = AddLevelDots(body, "Dots", new Vector2(0, -70), new Vector2(480, 70));

            AddHoverLift(cardGo, 12f, 1.03f, bodyImage);

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
            var panel = AddPanel(root, "ProgressPanel", Vector2.zero, Vector2.zero, Theme.Window).gameObject;
            Anchor(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(660, 250));
            var region = AddText(panel, "CurrentRegion", "PART : -", 36, new Vector2(0, 75), new Vector2(620, 60));
            region.alignment = TextAnchor.MiddleLeft;
            AddBarRow(panel, "THIS PART", "RegionBar", 0f, out var regionBar);
            AddBarRow(panel, "TOTAL", "TotalBar", -75f, out var totalBar);

            // 중앙 에임. 괄호는 글자로 그리고 노즐 단계 색을 곱합니다.
            var aim = CreateChild(root, "Aim");
            var aimRect = (RectTransform)aim.transform;
            aimRect.sizeDelta = new Vector2(400, 400);
            // 괄호 이미지는 호가 정중앙에 오도록 그려져 있어, 가로(좌우)와 세로(위아래, 90도 회전) 모두 조준점과 중심이 맞습니다.
            var firstBracket = AddIconOrText(aim, "FirstBracket", "(", 100, new Vector2(-44, 0), new Vector2(40, 160));
            var secondBracket = AddIconOrText(aim, "SecondBracket", ")", 100, new Vector2(44, 0), new Vector2(40, 160));
            AddShadow(firstBracket.gameObject, new Vector2(2, -2), true);
            AddShadow(secondBracket.gameObject, new Vector2(2, -2), true);
            var dot = AddIconOrText(aim, "Dot", "+", 40, Vector2.zero, new Vector2(44, 44));

            // 우하단 상태. 자세와 물/거품 아이콘이 생기면 UI_HUD의 postureSprites, waterSprite, foamSprite에 넣습니다.
            var posturePanel = AddPanel(root, "PosturePanel", Vector2.zero, Vector2.zero, Theme.Window).gameObject;
            Anchor(posturePanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-340, 30), new Vector2(150, 150));
            var posture = AddIconOrText(posturePanel, "Posture", "STAND", 26, Vector2.zero, new Vector2(130, 130));

            var toolPanel = AddPanel(root, "ToolPanel", Vector2.zero, Vector2.zero, Theme.Window).gameObject;
            Anchor(toolPanel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 30), new Vector2(150, 150));
            var tool = AddIconOrText(toolPanel, "Tool", "WATER", 26, Vector2.zero, new Vector2(130, 130));

            var nozzle = CreateChild(root, "Nozzle");
            Anchor(nozzle, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(140, 140));
            var nozzleIcon = AddImage(nozzle, Color.red);
            nozzleIcon.sprite = Theme.Round;
            nozzleIcon.preserveAspect = true;
            var nozzleNumber = AddText(nozzle, "Number", "1", 56, new Vector2(0, 4), new Vector2(120, 120));

            // 노즐 길이 막대. 자세 패널 왼쪽 끝(-490)부터 노즐 오른쪽 끝(-30)까지 덮고, 버튼 위(y=180)에서 20px 띄웁니다.
            const float lengthBarWidth = 460f;
            const float lengthBarHeight = 24f;
            const float lengthSegmentGap = 4f;
            const int lengthSegmentCount = 4;
            var lengthBar = CreateChild(root, "NozzleLengthBar");
            Anchor(lengthBar, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 200), new Vector2(lengthBarWidth, lengthBarHeight));
            float segmentWidth = (lengthBarWidth - lengthSegmentGap * (lengthSegmentCount - 1)) / lengthSegmentCount;
            var lengthSegments = new Object[lengthSegmentCount];
            for (int i = 0; i < lengthSegmentCount; i++)
            {
                var segmentGo = CreateChild(lengthBar, $"Segment{i + 1}");
                Anchor(segmentGo, new Vector2(0, 0), new Vector2(0, 0), new Vector2(i * (segmentWidth + lengthSegmentGap), 0),
                    new Vector2(segmentWidth, lengthBarHeight));
                var segment = AddImage(segmentGo, i == 0 ? new Color(0f, 0.69f, 0.94f) : Color.white);
                segment.raycastTarget = false;
                lengthSegments[i] = segment;
            }

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            var hud = root.AddComponent<UI_HUD>();
            Bind(hud,
                ("currentRegionText", region), ("regionBar", regionBar), ("totalBar", totalBar),
                ("aimRoot", aimRect), ("firstBracket", firstBracket), ("secondBracket", secondBracket),
                ("firstBracketSprite", Theme.AimBracketLeft), ("secondBracketSprite", Theme.AimBracketRight),
                ("aimDot", dot), ("waterAimSprite", Theme.IconWaterAim), ("foamAimSprite", Theme.IconFoamAim),
                ("postureIcon", posture), ("toolIcon", tool),
                ("nozzleIcon", nozzleIcon), ("nozzleNumberText", nozzleNumber),
                ("nozzleLengthRoot", lengthBar));
            BindArray(hud, "cleaningToolOnly", aim, toolPanel, nozzle);
            BindArray(hud, "nozzleLengthSegments", lengthSegments);
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
            AddScreenBackground(root);

            AddPanel(root, "CompletePanel", new Vector2(0, 230), new Vector2(760, 430), Theme.Window);
            AddIcon(root, "Trophy", Theme.IconTrophy, new Vector2(0, 340), new Vector2(150, 150), Theme.SelectedColor);
            var complete = AddText(root, "CompleteText", "COMPLETE!", 110, new Vector2(0, 170), new Vector2(720, 200), Theme.TextDark);
            complete.font = Theme.LogoFont;

            AddPanel(root, "SummaryPanel", new Vector2(0, -120), new Vector2(860, 110), Theme.Window);
            var summary = AddText(root, "Summary", "EARNED : 0    MONEY : 0", 40, new Vector2(0, -120), new Vector2(840, 100));

            var stay = AddButton(root, "Btn_Stay", "STAY", new Vector2(-270, -340), new Vector2(460, 120), ButtonStyle.Beige);
            var home = AddButton(root, "Btn_Home", "GO HOME", new Vector2(270, -340), new Vector2(460, 120), ButtonStyle.Brown, Theme.IconHome);

            Bind(root.AddComponent<UI_Settlement>(), ("summaryText", summary), ("stayButton", stay), ("homeButton", home));
            return root;
        }

        private static GameObject BuildProgressDetail()
        {
            var root = BuildPopupRoot(UIName.ProgressDetail, new Vector2(1320, 900), false, out var window, out var dim);

            AddText(window, "Title", "CLEANING PROGRESS", 54, new Vector2(0, 385), new Vector2(1000, 90));
            var measuring = AddText(window, "Measuring", "MEASURING...", 38, new Vector2(0, 40), new Vector2(800, 80));

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
            var rowName = AddText(rowGo, "Name", "Part", 34, new Vector2(-470, 0), new Vector2(220, 60));
            rowName.alignment = TextAnchor.MiddleLeft;
            var rowBar = AddProgressBar(rowGo, "Bar", new Vector2(150, 0), new Vector2(780, 44), 90f);
            var row = rowGo.AddComponent<UI_ProgressDetailRow>();
            Bind(row, ("nameText", rowName), ("bar", rowBar));
            rowGo.SetActive(false);

            var totalLabel = AddText(window, "TotalLabel", "TOTAL", 40, new Vector2(-470, -340), new Vector2(220, 70));
            totalLabel.alignment = TextAnchor.MiddleLeft;
            var totalBar = AddProgressBar(window, "TotalBar", new Vector2(150, -340), new Vector2(780, 50), 90f);

            var comp = root.AddComponent<UI_ProgressDetail>();
            Bind(comp, ("bgCanvasGroup", dim), ("windowRect", window.transform),
                ("rowTemplate", row), ("totalBar", totalBar), ("measuringLabel", measuring.gameObject));
            return root;
        }

        private static GameObject BuildTutorial()
        {
            var root = CreateUIRoot(UIName.Tutorial);

            var panel = AddPanel(root, "Panel", Vector2.zero, Vector2.zero, Theme.Window).gameObject;
            Anchor(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -300), new Vector2(460, 360));
            AddIcon(panel, "TitleIcon", Theme.IconTutorial, new Vector2(-185, 135), new Vector2(50, 50), Theme.TextDark);
            var step = AddText(panel, "Step", "TUTORIAL 1/1", 34, new Vector2(20, 135), new Vector2(360, 60));
            step.alignment = TextAnchor.MiddleLeft;
            var message = AddText(panel, "Message", "", 30, new Vector2(0, -15), new Vector2(410, 220));
            message.alignment = TextAnchor.UpperLeft;
            message.resizeTextForBestFit = false;
            // 건너뛰기 키는 TutorialDataManager의 _skipKey(기본 F)와 맞춥니다.
            var skip = AddText(panel, "SkipHint", "PRESS F TO SKIP", 24, new Vector2(0, -150), new Vector2(410, 40));
            skip.alignment = TextAnchor.MiddleRight;
            skip.color = new Color(Theme.TextDark.r, Theme.TextDark.g, Theme.TextDark.b, 0.6f);

            var completed = AddIcon(panel, "CompletedMark", Theme.IconCheck, new Vector2(190, 135), new Vector2(56, 56),
                new Color(0.3f, 0.65f, 0.2f));
            completed.gameObject.SetActive(false);

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            Bind(root.AddComponent<UI_Tutorial>(),
                ("stepText", step), ("messageText", message), ("completedMark", completed.gameObject));
            return root;
        }

        #endregion

        #region Prefabs - Common popups

        private static GameObject BuildSettings()
        {
            var root = BuildPopupRoot(UIName.Settings, new Vector2(960, 880), true, out var window, out var dim);

            AddText(window, "Title", "SETTINGS", 60, new Vector2(0, 370), new Vector2(500, 90));
            var close = AddButton(window, "Btn_Close", "", new Vector2(400, 370), new Vector2(90, 90), ButtonStyle.Square, Theme.IconClose);

            AddSliderRow(window, "Bgm", "BGM", 210f, out var bgm, out var bgmValue);
            AddSliderRow(window, "Sfx", "SFX", 70f, out var sfx, out var sfxValue);
            AddSliderRow(window, "Sensitivity", "MOUSE", -70f, out var sensitivity, out var sensitivityValue);

            var inGame = CreateChild(window, "InGameButtons");
            var inGameRect = (RectTransform)inGame.transform;
            inGameRect.anchoredPosition = new Vector2(0, -290);
            inGameRect.sizeDelta = new Vector2(860, 130);
            var save = AddButton(inGame, "Btn_Save", "SAVE", new Vector2(-220, 0), new Vector2(400, 120), ButtonStyle.Beige, Theme.IconSave);
            var returnHome = AddButton(inGame, "Btn_Return", "GO HOME", new Vector2(220, 0), new Vector2(400, 120), ButtonStyle.Brown, Theme.IconHome);

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
            var labelText = AddText(window, name + "Label", label, 38, new Vector2(-280, y), new Vector2(300, 70));
            labelText.alignment = TextAnchor.MiddleLeft;
            value = AddText(window, name + "Value", "0", 36, new Vector2(-70, y), new Vector2(100, 70));

            var go = DefaultControls.CreateSlider(Theme.ControlResources);
            go.name = name + "Slider";
            go.transform.SetParent(window.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = new Vector2(210, y);
            rect.sizeDelta = new Vector2(420, 40);

            // 손잡이는 세로로 슬라이더 높이에 맞춰 늘어나므로, 가로세로를 같은 48로 맞추고 그림 비율을 지켜 원이 찌그러지지 않게 합니다.
            // (슬라이더 높이 40 + sizeDelta.y 8 = 48)
            var handle = (RectTransform)go.transform.Find("Handle Slide Area/Handle");
            if (handle != null)
            {
                handle.sizeDelta = new Vector2(48, 8);
                var handleImage = handle.GetComponent<Image>();
                handleImage.type = Image.Type.Simple;
                handleImage.preserveAspect = true;
            }

            slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
        }

        private static GameObject BuildConfirmPopup()
        {
            var root = BuildPopupRoot(UIName.ConfirmPopup, new Vector2(800, 460), true, out var window, out var dim);

            var message = AddText(window, "Message", "", 40, new Vector2(0, 70), new Vector2(720, 220));
            message.resizeTextForBestFit = false;
            var confirm = AddButton(window, "Btn_Confirm", "OK", new Vector2(-170, -140), new Vector2(280, 100), ButtonStyle.Brown);
            var cancel = AddButton(window, "Btn_Cancel", "CANCEL", new Vector2(170, -140), new Vector2(280, 100), ButtonStyle.Beige);

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
            var dimImage = AddImage(dim, isModal ? Theme.DimColor : new Color(0f, 0f, 0f, 0.2f));
            dimImage.raycastTarget = isModal;
            dimGroup = dim.AddComponent<CanvasGroup>();
            dimGroup.blocksRaycasts = isModal;

            window = AddPanel(root, "Window", Vector2.zero, windowSize, Theme.Window).gameObject;
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
            new GameObject("PlayerHudBridge").AddComponent<PlayerHudBridge>();
            EnsureTutorialDataManager(scene);
            EnsurePlayer(scene);

            EditorSceneManager.SaveScene(scene);
        }

        // 튜토리얼 진행을 맡는 Tutorial 모듈 컴포넌트. 담당자가 인스펙터 값을 바꿀 수 있으므로 없을 때만 만듭니다.
        private static void EnsureTutorialDataManager(Scene scene)
        {
            bool exists = scene.GetRootGameObjects().Any(go => go.GetComponentInChildren<TutorialDataManager>(true) != null);
            if (!exists) new GameObject("TutorialDataManager").AddComponent<TutorialDataManager>();
        }

        // Player 프리팹이 없으면 WashPoint 앞에 놓고, 씬에 따로 있던 Main Camera를 끕니다. 프리팹 자체는 수정하지 않습니다.
        private static void EnsurePlayer(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject player = roots.FirstOrDefault(go =>
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == PlayerPrefabPath);

            if (player == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[GameUI] Player 프리팹이 없습니다: {PlayerPrefabPath}");
                    return;
                }

                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                player.name = "Player";

                GameObject washPoint = roots.FirstOrDefault(go => go.name == "WashPoint");
                Vector3 target = washPoint != null ? washPoint.transform.position : Vector3.zero;
                player.transform.position = target + new Vector3(0f, 0.1f, -6f);
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            }

            foreach (GameObject go in roots)
            {
                if (go != player && go.name == "Main Camera" && go.activeSelf)
                {
                    go.SetActive(false);
                    Debug.Log("[GameUI] Player 카메라를 쓰므로 씬의 Main Camera를 껐습니다.");
                }
            }
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

        private static void BindArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"[GameUI] {target.GetType().Name}에 {field} 배열 필드가 없습니다.");
                return;
            }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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

        // 9-slice 이미지. ppuMultiplier가 1보다 작으면 모서리가 그만큼 크게 그려집니다.
        private static Image AddSlicedImage(GameObject go, Sprite sprite, float ppuMultiplier = 0.5f)
        {
            var image = AddImage(go, Color.white);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = ppuMultiplier;
            return image;
        }

        // 전체 화면 배경. 갈색 나무판을 화면에 맞춰 늘리고 테두리를 두껍게 그립니다.
        // Kenney 화살표 그림 자체를 버튼으로 씁니다. 끝에 닿아 비활성이면 반투명해집니다.
        private static Button AddArrowButton(GameObject parent, string name, Sprite sprite, Vector2 pos)
        {
            var image = AddIcon(parent, name, sprite, pos, new Vector2(66, 63), Color.white);
            image.raycastTarget = true;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            button.colors = colors;

            AddHoverLift(image.gameObject, 8f, 1.1f);
            return button;
        }

        // 마우스를 올리면 떠오르는 효과. 떠오른 만큼 아래로 늘어난 투명 HitArea를 맨 뒤에 깔아 깜빡임을 막습니다.
        private static void AddHoverLift(GameObject target, float liftHeight = 12f, float liftScale = 1.04f, Graphic tintTarget = null)
        {
            if (tintTarget == null) tintTarget = target.GetComponent<Graphic>();

            var hitArea = CreateChild(target, "HitArea");
            hitArea.transform.SetAsFirstSibling();
            var hitRect = (RectTransform)hitArea.transform;
            hitRect.anchorMin = Vector2.zero;
            hitRect.anchorMax = Vector2.one;
            hitRect.offsetMin = new Vector2(0f, -liftHeight);
            hitRect.offsetMax = Vector2.zero;
            AddImage(hitArea, new Color(1f, 1f, 1f, 0f));

            var lift = target.AddComponent<UI_HoverLift>();
            var so = new SerializedObject(lift);
            so.FindProperty("liftHeight").floatValue = liftHeight;
            so.FindProperty("liftScale").floatValue = liftScale;
            so.FindProperty("tintTarget").objectReferenceValue = tintTarget;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddScreenBackground(GameObject root)
        {
            var image = AddSlicedImage(root, Theme.Board, 0.25f);
            image.color = new Color(0.82f, 0.78f, 0.74f);
        }

        private static Image AddPanel(GameObject parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return AddSlicedImage(go, sprite);
        }

        private static Image AddIcon(GameObject parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color color)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var image = AddImage(go, color);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Text AddText(GameObject parent, string name, string content, int size, Vector2 pos, Vector2 boxSize)
        {
            return AddText(parent, name, content, size, pos, boxSize, Theme.TextDark);
        }

        // 긴 문구가 칸을 넘지 않도록 Best Fit으로 글자 크기를 줄입니다. 여러 줄 문구는 호출한 곳에서 끕니다.
        private static Text AddText(GameObject parent, string name, string content, int size, Vector2 pos, Vector2 boxSize, Color color)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;

            var text = go.AddComponent<Text>();
            text.font = Theme.BodyFont;
            text.text = content;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(16, size);
            text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        private static void AddShadow(GameObject go, Vector2 distance, bool inChildren = false)
        {
            var targets = inChildren ? go.GetComponentsInChildren<Graphic>(true).Select(g => g.gameObject) : new[] { go };
            foreach (GameObject target in targets)
            {
                var shadow = target.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                shadow.effectDistance = distance;
            }
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

        // label이 비어 있으면 아이콘만 가운데에, 있으면 아이콘을 왼쪽에 둡니다.
        private static Button AddButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size,
                                        ButtonStyle style, Sprite icon = null)
        {
            (Sprite normal, Sprite pressed, Sprite disabled) = style switch
            {
                ButtonStyle.Beige => (Theme.AccentButton, Theme.AccentButtonPressed, Theme.AccentButtonDisabled),
                ButtonStyle.Square => (Theme.SquareButton, Theme.SquareButtonPressed, Theme.SquareButtonDisabled),
                _ => (Theme.Button, Theme.ButtonPressed, Theme.ButtonDisabled),
            };
            Color contentColor = style == ButtonStyle.Beige ? Theme.TextDark : Theme.TextLight;

            // 누르면 그림자가 얇은 눌린 그림으로, 비활성이면 회색 그림으로 바뀝니다.
            var image = AddPanel(parent, name, pos, size, normal);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = normal,
                selectedSprite = normal,
                pressedSprite = pressed,
                disabledSprite = disabled,
            };

            bool hasLabel = !string.IsNullOrEmpty(label);
            float iconSize = Mathf.Min(size.y * 0.5f, 56f);
            if (icon != null)
            {
                Vector2 iconPos = hasLabel ? new Vector2(-size.x * 0.5f + size.y * 0.45f, 2f) : new Vector2(0f, 2f);
                AddIcon(image.gameObject, "Icon", icon, iconPos, new Vector2(iconSize, iconSize), contentColor);
            }

            if (hasLabel)
            {
                // 아이콘이 있으면 글자 칸을 오른쪽으로 비켜 겹치지 않게 합니다.
                float iconSpace = icon != null ? size.y * 0.7f : 0f;
                var text = AddText(image.gameObject, "Label", label, 40, new Vector2(iconSpace * 0.5f, 2f),
                    new Vector2(size.x - iconSpace - 30f, size.y - 20f), contentColor);
                text.raycastTarget = false;
            }

            // 모든 버튼에 마우스를 올리면 떠오르는 효과를 줍니다.
            AddHoverLift(image.gameObject);
            return button;
        }

        private static UI_ProgressBar AddProgressBar(GameObject parent, string name, Vector2 pos, Vector2 size, float percentWidth)
        {
            var back = AddPanel(parent, name, pos, size, Theme.Inset);
            back.pixelsPerUnitMultiplier = 1f;

            var fill = CreateChild(back.gameObject, "Fill");
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            AddSlicedImage(fill, Theme.BarFill, 1f);

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
            ((RectTransform)dotGo.transform).sizeDelta = new Vector2(56, 60);
            var dotImage = AddImage(dotGo, Color.white);
            dotImage.sprite = Theme.Round;
            dotImage.preserveAspect = true;
            dotImage.raycastTarget = false;
            dotGo.SetActive(false);

            var dots = go.AddComponent<UI_LevelDots>();
            Bind(dots, ("dotTemplate", dotImage), ("reachedSprite", Theme.RoundReached), ("lockedSprite", Theme.Round));
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
            // ScrollRect는 가로 목록에서 휠을 위로 굴리면 오른쪽으로 넘어가므로, 가로 목록만 부호를 바꿔 위 = 왼쪽, 아래 = 오른쪽으로 합니다.
            scroll.scrollSensitivity = isHorizontal ? -60f : 60f;

            if (scrollbarPos.HasValue)
            {
                var barGo = DefaultControls.CreateScrollbar(Theme.ControlResources);
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
