using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 0~1 값을 막대 길이와 퍼센트 문구로 보여줍니다.
    /// </summary>
    public class UI_ProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Text percentText;

        public void SetValue(float value)
        {
            value = Mathf.Clamp01(value);
            fillRect.anchorMax = new Vector2(value, 1f);
            percentText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
