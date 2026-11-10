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

        public void Bind(StageDefinition stage, Action<StageDefinition> onClick)
        {
            nameText.text = stage.displayName;
            UI_RequestDetail.ApplyImage(stage, dinoImage, imagePlaceholderText);
            button.onClick.AddListener(() => onClick(stage));
        }
    }
}
