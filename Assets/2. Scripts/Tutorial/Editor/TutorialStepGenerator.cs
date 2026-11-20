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
            new("Press WASD to move.", TutorialInputType.Key, Key.W, Key.A, Key.S, Key.D),
            new("Move the mouse to look around.", TutorialInputType.MouseMove),
            new("Hold Shift while moving to run.", TutorialInputType.KeyWhileMoving, Key.LeftShift),
            new("Press Space to jump.", TutorialInputType.Key, Key.Space),
            new("Press Ctrl to change your stance. It cycles through STAND > CROUCH > PRONE.", TutorialInputType.Key, Key.LeftCtrl),
            new("Hold Left Click to spray water.", TutorialInputType.LeftClick),
            new("Scroll the mouse wheel to change the stream width. Narrower is stronger; wider is weaker.", TutorialInputType.Scroll),
            new("Press 2 to change the nozzle length. A longer nozzle reaches farther, but the stream gets narrower.", TutorialInputType.Key, Key.Digit2),
            new("Press R to switch the stream between horizontal and vertical.", TutorialInputType.Key, Key.R),
            new("Press 1 to switch between water and foam. Spray foam first, then water, to wash dirt off more easily.", TutorialInputType.Key, Key.Digit1),
            new("Press C to change the aim mode. In free mode, the camera stays still while you aim with the mouse.", TutorialInputType.Key, Key.C),
            new("Press F5 to switch between first-person and third-person view.", TutorialInputType.Key, Key.F5),
            new("Press Tab to see detailed cleaning progress.", TutorialInputType.Key, Key.Tab),
        };

        private static readonly StepDef[] FlySteps =
        {
            new("You can fly now! Jump, then press Space again in midair to take off.", TutorialInputType.FairyEquipped),
            new("While flying, hold Space to go up.", TutorialInputType.KeyWhileFlying, Key.Space),
            new("While flying, hold Ctrl to go down.", TutorialInputType.KeyWhileFlying, Key.LeftCtrl),
            new("Flying ends when you touch the ground. To fly again, jump and press Space again in midair.", TutorialInputType.FairyUnequipped),
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
