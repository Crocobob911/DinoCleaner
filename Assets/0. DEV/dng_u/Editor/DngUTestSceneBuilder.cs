using DinoCleaner.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DinoCleaner.Dev.DngU.Editor
{
    /// <summary>
    /// UILoader 테스트 환경(테스트 프리팹 + Addressables 등록 + dng_u 씬)을 생성합니다.
    /// 메뉴: DinoCleaner/Dev/Build dng_u UI Test Scene
    /// </summary>
    public static class DngUTestSceneBuilder
    {
        private const string PrefabDir = "Assets/0. DEV/dng_u/UI";
        private const string ScenePath = "Assets/1. Scenes/dng_u.unity";
        private const string AddressableGroupName = "UI";

        private static Font font;

        [MenuItem("DinoCleaner/Dev/Build dng_u UI Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            string fullScreenPath = SavePrefab(BuildTestFullScreen(), "Test_FullScreen");
            string popupPath = SavePrefab(BuildTestPopup(), "Test_Popup");
            string topPath = SavePrefab(BuildTestTop(), "Test_Top");

            RegisterAddressable(fullScreenPath, "Test_FullScreen");
            RegisterAddressable(popupPath, "Test_Popup");
            RegisterAddressable(topPath, "Test_Top");
            AssetDatabase.SaveAssets();

            BuildScene();

            Debug.Log($"[DngU] 테스트 씬 생성 완료: {ScenePath}");
        }

        #region Prefabs

        private static GameObject BuildTestFullScreen()
        {
            var root = CreateUIRoot("Test_FullScreen");
            AddImage(root, new Color(0.16f, 0.22f, 0.28f));

            AddText(root, "Title", "UILoader Test - FullScreen", 56, new Vector2(0, 300), new Vector2(1200, 100));

            var openPopup = AddButton(root, "Btn_OpenPopup", "Popup 열기 (string 데이터)", new Vector2(0, 100));
            var openTop = AddButton(root, "Btn_OpenTop", "Top 토스트 (ValueTuple)", new Vector2(0, -20));
            var unloadPopup = AddButton(root, "Btn_UnloadPopup", "Popup Unload (재로드 테스트)", new Vector2(0, -140));

            var comp = root.AddComponent<Test_FullScreen>();
            var so = new SerializedObject(comp);
            so.FindProperty("openPopupButton").objectReferenceValue = openPopup;
            so.FindProperty("openTopButton").objectReferenceValue = openTop;
            so.FindProperty("unloadPopupButton").objectReferenceValue = unloadPopup;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject BuildTestPopup()
        {
            var root = CreateUIRoot("Test_Popup");

            var bg = CreateChild(root, "Dim");
            Stretch(bg);
            AddImage(bg, new Color(0, 0, 0, 0.6f));
            var bgGroup = bg.AddComponent<CanvasGroup>();

            var window = CreateChild(root, "Window");
            var windowRect = (RectTransform)window.transform;
            windowRect.sizeDelta = new Vector2(800, 450);
            AddImage(window, new Color(0.95f, 0.93f, 0.85f));

            var message = AddText(window, "Message", "message", 40, new Vector2(0, 60), new Vector2(700, 200));
            message.color = Color.black;
            var close = AddButton(window, "Btn_Close", "닫기", new Vector2(0, -140));

            var comp = root.AddComponent<Test_Popup>();
            var so = new SerializedObject(comp);
            so.FindProperty("messageText").objectReferenceValue = message;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("bgCanvasGroup").objectReferenceValue = bgGroup;
            so.FindProperty("windowRect").objectReferenceValue = windowRect;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject BuildTestTop()
        {
            var root = CreateUIRoot("Test_Top");

            // 토스트가 뒤쪽 클릭을 막지 않도록 레이캐스트 끔
            var bar = CreateChild(root, "Bar");
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0, -40);
            barRect.sizeDelta = new Vector2(1000, 120);
            AddImage(bar, new Color(0.85f, 0.25f, 0.25f, 0.9f)).raycastTarget = false;

            var message = AddText(bar, "Message", "message", 40, Vector2.zero, new Vector2(960, 110));
            message.raycastTarget = false;

            var comp = root.AddComponent<Test_Top>();
            var so = new SerializedObject(comp);
            so.FindProperty("messageText").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static string SavePrefab(GameObject root, string name)
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/0. DEV/dng_u", "UI");
            }
            string path = $"{PrefabDir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        private static void RegisterAddressable(string assetPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(AddressableGroupName)
                        ?? settings.CreateGroup(AddressableGroupName, false, false, true, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
        }

        #endregion

        #region Scene

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();

            var canvasFull = CreateCanvas("Canvas_FullScreen", 0);
            var canvasPopup = CreateCanvas("Canvas_Popup", 5);
            var canvasTop = CreateCanvas("Canvas_Top", 10);

            var loaderGo = new GameObject("UILoader");
            var loader = loaderGo.AddComponent<UILoader>();
            var so = new SerializedObject(loader);
            so.FindProperty("canvas_FullScreen").objectReferenceValue = canvasFull;
            so.FindProperty("canvas_Popup").objectReferenceValue = canvasPopup;
            so.FindProperty("canvas_Top").objectReferenceValue = canvasTop;
            var list = so.FindProperty("awakeUIList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).stringValue = "Test_FullScreen";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
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

        private static GameObject CreateUIRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Stretch(go);
            return go;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
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
            text.color = Color.white;
            return text;
        }

        private static Button AddButton(GameObject parent, string name, string label, Vector2 pos)
        {
            var go = CreateChild(parent, name);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(600, 90);

            var image = AddImage(go, new Color(0.3f, 0.55f, 0.85f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var text = AddText(go, "Label", label, 34, Vector2.zero, rt.sizeDelta);
            text.raycastTarget = false;
            return button;
        }

        #endregion
    }
}
