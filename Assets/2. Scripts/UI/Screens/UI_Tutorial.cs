using System;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 단축키를 하나씩 안내하고, 해당 키를 누르면 다음 안내로 넘어갑니다.
    /// </summary>
    public class UI_Tutorial : MonoBehaviour, UI_ILayerInfo
    {
        private readonly struct Step
        {
            public readonly string Message;
            public readonly Func<bool> IsDone;

            public Step(string message, Func<bool> isDone)
            {
                Message = message;
                IsDone = isDone;
            }
        }

        private static readonly Step[] Steps =
        {
            new("좌클릭으로 물을 쏠 수 있습니다.", () => Mouse.current.leftButton.wasPressedThisFrame),
            new("우클릭으로 물을 계속 쏠 수 있습니다.", () => Mouse.current.rightButton.wasPressedThisFrame),
            new("Tab을 눌러 진행도 상세를 볼 수 있습니다.", () => Keyboard.current.tabKey.wasPressedThisFrame),
            new("마우스 휠로 노즐 너비를 바꿀 수 있습니다.", () => Mouse.current.scroll.ReadValue().y != 0f),
            new("R을 눌러 에임 모양을 바꿀 수 있습니다.", () => Keyboard.current.rKey.wasPressedThisFrame),
            new("1, 2를 눌러 물과 거품을 바꿀 수 있습니다.",
                () => Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.digit2Key.wasPressedThisFrame),
            new("C를 눌러 에임 고정을 풀 수 있습니다.", () => Keyboard.current.cKey.wasPressedThisFrame),
        };

        public EUILayer TargetLayer => EUILayer.Top;

        [SerializeField] private Text messageText;

        private int stepIndex;

        private void OnEnable()
        {
            stepIndex = 0;
            messageText.text = Steps[stepIndex].Message;
        }

        private void Update()
        {
            if (UI_PopupBase.IsAnyModalOpen || Mouse.current == null || Keyboard.current == null) return;
            if (!Steps[stepIndex].IsDone()) return;

            stepIndex++;
            if (stepIndex < Steps.Length)
            {
                messageText.text = Steps[stepIndex].Message;
                return;
            }

            GameServices.Progress.IsTutorialCompleted = true;
            UILoader.Instance.HideUI(UIName.Tutorial);
        }
    }
}
