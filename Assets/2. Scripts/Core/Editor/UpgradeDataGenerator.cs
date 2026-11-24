using UnityEditor;
using UnityEngine;

namespace DinoCleaner.Core.Editor
{
    // 기획 문서(도구(업그레이드) 목록)의 상점 업그레이드 에셋과 카탈로그를 기본값으로 만든다.
    // 이미 있는 업그레이드 에셋은 값을 덮어쓰지 않으므로, 인스펙터에서 고친 값은 다시 실행해도 유지된다.
    public static class UpgradeDataGenerator
    {
        private const string Folder = "Assets/6. ScriptableObjects/Upgrades";
        private const string CatalogFolder = "Assets/Resources";
        private const string CatalogPath = CatalogFolder + "/" + UpgradeCatalog.ResourcesPath + ".asset";

        private readonly struct UpgradeDef
        {
            public readonly string FileName;
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly int StartLevel;
            public readonly int MaxLevel;
            public readonly int[] Costs;

            public UpgradeDef(string fileName, string id, string displayName, string description, int startLevel, int maxLevel, params int[] costs)
            {
                FileName = fileName;
                Id = id;
                DisplayName = displayName;
                Description = description;
                StartLevel = startLevel;
                MaxLevel = maxLevel;
                Costs = costs;
            }
        }

        // id는 세이브 키. pressure·foamWidth·fairyDino는 기존 세이브와 호환되도록 유지
        private static readonly UpgradeDef[] Defs =
        {
            // 화면 문구는 영어 (2026-11-17 결정). 적용 대상: 수압 = 물, 노즐 길이 = 물·거품, 거품 너비 = 거품
            new("Upgrade_Pressure", UpgradeIds.Pressure, "Pressure", "Stronger water pressure washes dirt off faster. (Water only)", 1, 3, 100, 200),
            new("Upgrade_NozzleLength", UpgradeIds.NozzleLength, "Nozzle Length", "A longer nozzle lets you reach farther. (Water and foam)", 1, 4, 100, 200, 300),
            new("Upgrade_FoamWidth", UpgradeIds.FoamWidth, "Foam Width", "Spray foam over a wider area at once. (Foam only)", 1, 3, 100, 200),
            new("Upgrade_FairyDino", UpgradeIds.FairyDino, "Fairy Dino", "A fairy dino helps you fly. Jump, then press Space again in the air.", 0, 1, 500),
        };

        [MenuItem("DinoCleaner/Upgrade/상점 업그레이드 에셋 생성")]
        private static void Generate()
        {
            if (!EnsureFolder("Assets/6. ScriptableObjects", "Upgrades") || !EnsureFolder("Assets", "Resources")) return;

            var assets = new UpgradeData[Defs.Length];
            int created = 0;
            for (int i = 0; i < Defs.Length; i++)
            {
                string path = $"{Folder}/{Defs[i].FileName}.asset";
                assets[i] = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                if (assets[i] != null) continue;

                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    Debug.LogError($"UpgradeData가 아닌 에셋이 이미 있습니다: {path}");
                    return;
                }

                assets[i] = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(assets[i], path);
                Apply(assets[i], Defs[i]);
                created++;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UpgradeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                SetCatalog(catalog, assets);
            }
            else
            {
                Debug.Log($"카탈로그가 이미 있어 목록은 바꾸지 않았습니다: {CatalogPath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"업그레이드 에셋 {created}개 생성, {Defs.Length - created}개는 기존 값 유지");
        }

        private static bool EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (AssetDatabase.IsValidFolder(path)) return true;
            if (!string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name))) return true;

            Debug.LogError($"폴더를 만들 수 없습니다: {path}");
            return false;
        }

        private static void Apply(UpgradeData data, UpgradeDef def)
        {
            var so = new SerializedObject(data);
            so.FindProperty("_id").stringValue = def.Id;
            so.FindProperty("_displayName").stringValue = def.DisplayName;
            so.FindProperty("_description").stringValue = def.Description;
            so.FindProperty("_startLevel").intValue = def.StartLevel;
            so.FindProperty("_maxLevel").intValue = def.MaxLevel;

            SerializedProperty costs = so.FindProperty("_costs");
            costs.arraySize = def.Costs.Length;
            for (int i = 0; i < def.Costs.Length; i++)
            {
                costs.GetArrayElementAtIndex(i).intValue = def.Costs[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void SetCatalog(UpgradeCatalog catalog, UpgradeData[] assets)
        {
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_upgrades");
            list.arraySize = assets.Length;
            for (int i = 0; i < assets.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
