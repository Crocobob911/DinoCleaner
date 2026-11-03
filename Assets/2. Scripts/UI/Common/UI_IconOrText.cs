using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// Sprite가 있으면 아이콘을, 없으면 대체 문구를 보여줍니다.
    /// </summary>
    public class UI_IconOrText : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text fallbackText;

        public RectTransform Rect => (RectTransform)transform;

        public void Set(Sprite sprite, string fallback)
        {
            bool hasSprite = sprite != null;
            icon.sprite = sprite;
            icon.enabled = hasSprite;
            fallbackText.enabled = !hasSprite;
            fallbackText.text = fallback;
        }

        public void SetColor(Color color)
        {
            icon.color = color;
            fallbackText.color = color;
        }
    }
}
