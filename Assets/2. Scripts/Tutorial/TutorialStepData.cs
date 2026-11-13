using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoCleaner.Tutorial
{
    public enum TutorialInputType
    {
        Key,
        LeftClick,
        RightClick,
        MiddleClick,
        Scroll,
    }

    // 튜토리얼 안내 한 단계. 안내 문구와 다음 단계로 넘어가는 입력을 담는다.
    [CreateAssetMenu(menuName = "DinoCleaner/Tutorial Step", fileName = "TutorialStep_")]
    public class TutorialStepData : ScriptableObject
    {
        [Tooltip("튜토리얼 창에 보일 안내 문구")]
        [TextArea(2, 5)] [SerializeField] private string _message;

        [Tooltip("다음 단계로 넘어가게 하는 입력")]
        [SerializeField] private TutorialInputType _inputType = TutorialInputType.Key;

        [Tooltip("Input Type이 Key일 때만 사용. 이 중 하나라도 누르면 다음 단계로 넘어감")]
        [SerializeField] private Key[] _keys;

        public string Message => _message;

        /// <summary>
        /// 이번 프레임에 이 단계의 입력이 들어왔는지 확인합니다.
        /// </summary>
        public bool IsDone(Mouse mouse, Keyboard keyboard)
        {
            switch (_inputType)
            {
                case TutorialInputType.LeftClick: return mouse.leftButton.wasPressedThisFrame;
                case TutorialInputType.RightClick: return mouse.rightButton.wasPressedThisFrame;
                case TutorialInputType.MiddleClick: return mouse.middleButton.wasPressedThisFrame;
                case TutorialInputType.Scroll: return mouse.scroll.ReadValue().y != 0f;
                case TutorialInputType.Key:
                    if (_keys == null) return false;
                    foreach (Key key in _keys)
                    {
                        // Key.None은 키보드 인덱서가 예외를 던지므로 건너뜀
                        if (key == Key.None) continue;
                        if (keyboard[key].wasPressedThisFrame) return true;
                    }
                    return false;
                default: return false;
            }
        }
    }
}
