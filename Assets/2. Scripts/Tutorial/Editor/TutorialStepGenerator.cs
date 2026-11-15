using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoCleaner.Tutorial.Editor
{
    // 기획에서 확정한 기본/날기 튜토리얼 단계를 에셋으로 만들고 각 시퀀스에 순서대로 연결한다.
    // 기존 에셋은 지우지 않고 값만 갱신하므로 GUID가 유지되고, 여러 번 실행해도 결과가 같다.
    public static class TutorialStepGenerator
    {
        private const string Folder = "Assets/6. ScriptableObjects/TutorialStep";
        private const string SequencePath = Folder + "/TutorialSequence.asset";
        private const string FlyFolder = Folder + "/Fly";
        private const string FlySequencePath = FlyFolder + "/FlyTutorialSequence.asset";

        private readonly struct StepDef
        {
            public readonly string Message;
            public readonly TutorialInputType InputType;
            public readonly Key[] Keys;

            public StepDef(string message, TutorialInputType inputType, params Key[] keys)
            {
                Message = message;
                InputType = inputType;
                Keys = keys;
            }
        }

        private static readonly StepDef[] Steps =
        {
            new("WASD를 눌러 이동할 수 있습니다.", TutorialInputType.Key, Key.W, Key.A, Key.S, Key.D),
            new("마우스를 움직여 주위를 둘러볼 수 있습니다.", TutorialInputType.MouseMove),
            new("Shift를 누른 채로 이동하면 달릴 수 있습니다.", TutorialInputType.KeyWhileMoving, Key.LeftShift),
            new("Space를 눌러 점프할 수 있습니다.", TutorialInputType.Key, Key.Space),
            new("Ctrl을 눌러 자세를 바꿀 수 있습니다. 서기 → 앉기 → 웅크리기 순서로 바뀝니다.", TutorialInputType.Key, Key.LeftCtrl),
            new("좌클릭을 누르고 있으면 물을 발사할 수 있습니다.", TutorialInputType.LeftClick),
            new("마우스 휠을 굴려 물줄기 너비를 바꿀 수 있습니다. 좁을수록 강하고, 넓을수록 약합니다.", TutorialInputType.Scroll),
            new("2를 눌러 노즐 길이를 바꿀 수 있습니다. 노즐이 길수록 물이 멀리 나가지만, 물줄기는 좁아집니다.", TutorialInputType.Key, Key.Digit2),
            new("R을 눌러 물줄기 방향을 가로/세로로 바꿀 수 있습니다.", TutorialInputType.Key, Key.R),
            new("1을 눌러 물과 거품을 바꿀 수 있습니다. 거품을 뿌린 뒤 물을 뿌리면 오물이 더 쉽게 지워집니다.", TutorialInputType.Key, Key.Digit1),
            new("C를 눌러 에임 모드를 바꿀 수 있습니다. 자유 모드에서는 화면을 멈춘 채 마우스로 조준할 수 있습니다.", TutorialInputType.Key, Key.C),
            new("F5를 눌러 1인칭/3인칭 시점을 바꿀 수 있습니다.", TutorialInputType.Key, Key.F5),
            new("Tab을 눌러 청소 진행도를 자세히 볼 수 있습니다.", TutorialInputType.Key, Key.Tab),
        };

        private static readonly StepDef[] FlySteps =
        {
            new("이제 날 수 있습니다! 점프한 뒤 공중에서 Space를 한 번 더 누르면 날아오릅니다.", TutorialInputType.FairyEquipped),
            new("날고 있는 동안 Space를 누르고 있으면 위로 올라갑니다.", TutorialInputType.KeyWhileFlying, Key.Space),
            new("날고 있는 동안 Ctrl을 누르고 있으면 아래로 내려갑니다.", TutorialInputType.KeyWhileFlying, Key.LeftCtrl),
            new("땅에 닿으면 날기가 풀립니다. 다시 날려면 점프한 뒤 Space를 한 번 더 누르세요.", TutorialInputType.FairyUnequipped),
        };

        [MenuItem("DinoCleaner/Tutorial/첫 튜토리얼 단계 생성")]
        private static void Generate()
        {
            var sequence = AssetDatabase.LoadAssetAtPath<TutorialSequence>(SequencePath);
            if (sequence == null)
            {
                Debug.LogError($"TutorialSequence를 찾을 수 없습니다: {SequencePath}");
                return;
            }

            // 같은 경로에 다른 타입 에셋이 있으면 덮어쓰지 않도록 먼저 전부 검사
            for (int i = 0; i < Steps.Length; i++)
            {
                string path = GetStepPath(i + 1);
                if (AssetDatabase.LoadMainAssetAtPath(path) != null
                    && AssetDatabase.LoadAssetAtPath<TutorialStepData>(path) == null)
                {
                    Debug.LogError($"TutorialStepData가 아닌 에셋이 이미 있습니다: {path}");
                    return;
                }
            }

            var stepAssets = new TutorialStepData[Steps.Length];
            for (int i = 0; i < Steps.Length; i++)
            {
                stepAssets[i] = LoadOrCreateStep(GetStepPath(i + 1));
                ApplyStep(stepAssets[i], Steps[i]);
            }

            var sequenceSo = new SerializedObject(sequence);
            SerializedProperty stepsProp = sequenceSo.FindProperty("_steps");
            stepsProp.arraySize = stepAssets.Length;
            for (int i = 0; i < stepAssets.Length; i++)
            {
                stepsProp.GetArrayElementAtIndex(i).objectReferenceValue = stepAssets[i];
            }
            sequenceSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sequence);

            AssetDatabase.SaveAssets();
            Debug.Log($"튜토리얼 단계 {Steps.Length}개 갱신/생성 완료");
        }

        [MenuItem("DinoCleaner/Tutorial/날기 튜토리얼 단계 생성")]
        private static void GenerateFly()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogError($"튜토리얼 폴더를 찾을 수 없습니다: {Folder}");
                return;
            }

            var sequence = AssetDatabase.LoadAssetAtPath<TutorialSequence>(FlySequencePath);
            if (AssetDatabase.LoadMainAssetAtPath(FlySequencePath) != null && sequence == null)
            {
                Debug.LogError($"TutorialSequence가 아닌 에셋이 이미 있습니다: {FlySequencePath}");
                return;
            }

            for (int i = 0; i < FlySteps.Length; i++)
            {
                string path = GetFlyStepPath(i + 1);
                if (AssetDatabase.LoadMainAssetAtPath(path) != null
                    && AssetDatabase.LoadAssetAtPath<TutorialStepData>(path) == null)
                {
                    Debug.LogError($"TutorialStepData가 아닌 에셋이 이미 있습니다: {path}");
                    return;
                }
            }

            if (!AssetDatabase.IsValidFolder(FlyFolder)
                && string.IsNullOrEmpty(AssetDatabase.CreateFolder(Folder, "Fly")))
            {
                Debug.LogError($"날기 튜토리얼 폴더를 만들 수 없습니다: {FlyFolder}");
                return;
            }

            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<TutorialSequence>();
                AssetDatabase.CreateAsset(sequence, FlySequencePath);
            }

            var stepAssets = new TutorialStepData[FlySteps.Length];
            for (int i = 0; i < FlySteps.Length; i++)
            {
                stepAssets[i] = LoadOrCreateStep(GetFlyStepPath(i + 1));
                ApplyStep(stepAssets[i], FlySteps[i]);
            }

            var sequenceSo = new SerializedObject(sequence);
            SerializedProperty stepsProp = sequenceSo.FindProperty("_steps");
            stepsProp.arraySize = stepAssets.Length;
            for (int i = 0; i < stepAssets.Length; i++)
            {
                stepsProp.GetArrayElementAtIndex(i).objectReferenceValue = stepAssets[i];
            }
            sequenceSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sequence);

            AssetDatabase.SaveAssets();
            Debug.Log("날기 튜토리얼 단계 4개 갱신/생성 완료");
        }

        private static string GetStepPath(int n) => $"{Folder}/TutorialStep_{n}.asset";

        private static string GetFlyStepPath(int n) => $"{FlyFolder}/FlyTutorialStep_{n}.asset";

        private static TutorialStepData LoadOrCreateStep(string path)
        {
            var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            if (step != null) return step;

            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, path);
            return step;
        }

        private static void ApplyStep(TutorialStepData step, StepDef def)
        {
            var so = new SerializedObject(step);
            so.FindProperty("_message").stringValue = def.Message;
            so.FindProperty("_inputType").intValue = (int)def.InputType;

            SerializedProperty keysProp = so.FindProperty("_keys");
            keysProp.arraySize = def.Keys.Length;
            for (int i = 0; i < def.Keys.Length; i++)
            {
                keysProp.GetArrayElementAtIndex(i).intValue = (int)def.Keys[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(step);
        }
    }
}
