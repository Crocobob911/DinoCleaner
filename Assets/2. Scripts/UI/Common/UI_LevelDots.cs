using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 업그레이드 단계를 점 개수로 보여줍니다. 최대 단계에 맞춰 점을 만듭니다.
    /// </summary>
    public class UI_LevelDots : MonoBehaviour
    {
        [SerializeField] private Image dotTemplate;
        [SerializeField] private Color reachedColor = new Color(0f, 0.69f, 0.94f);
        [SerializeField] private Color lockedColor = new Color(0.65f, 0.65f, 0.65f);

        [Tooltip("지정하면 색 대신 이 Sprite로 단계를 구분합니다.")]
        [SerializeField] private Sprite reachedSprite;
        [SerializeField] private Sprite lockedSprite;

        private readonly List<Image> dots = new();

        public void Set(int level, int maxLevel)
        {
            while (dots.Count < maxLevel)
            {
                Image dot = Instantiate(dotTemplate, dotTemplate.transform.parent);
                dot.gameObject.SetActive(true);
                dots.Add(dot);
            }

            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].gameObject.SetActive(i < maxLevel);

                bool isReached = i < level;
                Sprite sprite = isReached ? reachedSprite : lockedSprite;
                if (sprite != null)
                {
                    dots[i].sprite = sprite;
                    dots[i].color = Color.white;
                }
                else
                {
                    dots[i].sprite = dotTemplate.sprite;
                    dots[i].color = isReached ? reachedColor : lockedColor;
                }
            }
        }
    }
}
