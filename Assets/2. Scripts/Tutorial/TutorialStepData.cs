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
        MouseMove,
        KeyWhileMoving,
        FairyEquipped,
        FairyUnequipped,
        KeyWhileFlying,
    }

    public readonly struct TutorialInputContext
    {
        public readonly Mouse Mouse;
        public readonly Keyboard Keyboard;
        public readonly bool IsFlying;

        /// <summary>입력 장치와 현재 날기 상태를 묶습니다.</summary>
        public TutorialInputContext(Mouse mouse, Keyboard keyboard, bool isFlying)
        {
            Mouse = mouse;
            Keyboard = keyboard;
            IsFlying = isFlying;
        }
    }

    // 튜토리얼 안내 한 단계. 안내 문구와 다음 단계로 넘어가는 입력을 담는다.
    [CreateAssetMenu(menuName = "DinoCleaner/Tutorial Step", fileName = "TutorialStep_")]
    public class TutorialStepData : ScriptableObject
    {
        [Tooltip("튜토리얼 창에 보일 안내 문구")]
        [TextArea(2, 5)] [SerializeField] private string _message;

        [Tooltip("다음 단계로 넘어가게 하는 입력")]
        [SerializeField] private TutorialInputType _inputType = TutorialInputType.Key;

        [Tooltip("Key, KeyWhileMoving, KeyWhileFlying일 때 사용")]
        [SerializeField] private Key[] _keys;

        public string Message => _message;

        /// <summary>
        /// 이번 프레임에 이 단계의 입력이 들어왔는지 확인합니다.
        /// </summary>
        public bool IsDone(in TutorialInputContext ctx)
        {
            switch (_inputType)
            {
                case TutorialInputType.LeftClick: return ctx.Mouse.leftButton.wasPressedThisFrame;
                case TutorialInputType.RightClick: return ctx.Mouse.rightButton.wasPressedThisFrame;
                case TutorialInputType.MiddleClick: return ctx.Mouse.middleButton.wasPressedThisFrame;
                case TutorialInputType.Scroll: return ctx.Mouse.scroll.ReadValue().y != 0f;
                case TutorialInputType.Key:
                    if (_keys == null) return false;
                    foreach (Key key in _keys)
                    {
                        // Key.None은 키보드 인덱서가 예외를 던지므로 건너뜀
                        if (key == Key.None) continue;
                        if (ctx.Keyboard[key].wasPressedThisFrame) return true;
                    }
                    return false;
                case TutorialInputType.MouseMove: return ctx.Mouse.delta.ReadValue() != Vector2.zero;
                case TutorialInputType.KeyWhileMoving:
                    if (_keys == null) return false;
                    bool isMoving = ctx.Keyboard.wKey.isPressed || ctx.Keyboard.aKey.isPressed
                                    || ctx.Keyboard.sKey.isPressed || ctx.Keyboard.dKey.isPressed;
                    if (!isMoving) return false;
                    foreach (Key key in _keys)
                    {
                        if (key == Key.None) continue;
                        if (ctx.Keyboard[key].isPressed) return true;
                    }
                    return false;
                case TutorialInputType.FairyEquipped: return ctx.IsFlying == true;
                case TutorialInputType.FairyUnequipped: return ctx.IsFlying == false;
                case TutorialInputType.KeyWhileFlying:
                    if (!ctx.IsFlying || _keys == null) return false;
                    foreach (Key key in _keys)
                    {
                        if (key == Key.None) continue;
                        if (ctx.Keyboard[key].wasPressedThisFrame) return true;
                    }
                    return false;
                default: return false;
            }
        }
    }
}
