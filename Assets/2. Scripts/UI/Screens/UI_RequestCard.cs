using System;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 의뢰 리스트의 공룡 카드 하나입니다.
    /// </summary>
    public class UI_RequestCard : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image dinoImage;
        [SerializeField] private Text imagePlaceholderText;
        [SerializeField] private Text nameText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject progressBadge;
        [SerializeField] private Text progressText;

        private const float LockedDarkness = 0.35f;

        private static bool _hasWarnedMissingLockIcon;

        /// <summary>
        /// 스테이지 정보를 카드에 넣습니다. 잠긴 스테이지는 어둡게 표시하고 클릭을 막습니다.
        /// </summary>
        public void Bind(StageDefinition stage, Action<StageDefinition> onClick)
        {
            nameText.text = stage.displayName;
            UI_RequestDetail.ApplyImage(stage, dinoImage, imagePlaceholderText);
            UI_RequestDetail.ApplyProgress(stage, progressBadge, progressText);

            bool isUnlocked = StageUnlockRules.IsUnlocked(stage.stageId, GameServices.Progress);

            if (lockIcon != null)
            {
                lockIcon.SetActive(!isUnlocked);
            }
            else if (!_hasWarnedMissingLockIcon)
            {
                _hasWarnedMissingLockIcon = true;
                Debug.LogWarning($"[UI_RequestCard] lockIcon이 연결되지 않았습니다: {name}", this);
            }

            if (isUnlocked)
            {
                button.onClick.AddListener(() => onClick(stage));
                return;
            }

            dinoImage.color = Darken(dinoImage.color);
            imagePlaceholderText.color = Darken(imagePlaceholderText.color);
            nameText.color = Darken(nameText.color);
            // 비활성 색이 한 번 더 곱해지지 않도록 전환 효과를 끕니다.
            button.interactable = false; button.transition = Selectable.Transition.None;
        }

        private static Color Darken(Color color)
        {
            return new Color(color.r * LockedDarkness, color.g * LockedDarkness, color.b * LockedDarkness, color.a);
        }
    }
}
