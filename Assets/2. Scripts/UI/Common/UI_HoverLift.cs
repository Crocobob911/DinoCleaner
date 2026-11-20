using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 마우스를 올리면 버튼이나 카드가 살짝 떠오르고 밝아집니다.
    /// 떠오른 만큼 아래로 늘어난 투명 영역(hitArea)이 있어, 마우스가 아래 가장자리에 있어도 깜빡이지 않습니다.
    /// </summary>
    public class UI_HoverLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float liftHeight = 12f;
        [SerializeField] private float liftScale = 1.04f;
        [SerializeField] private float duration = 0.12f;
        [Tooltip("떠오르는 동안 곱할 밝기. 흰색이면 색을 바꾸지 않습니다.")]
        [SerializeField] private Color hoverTint = new Color(1f, 0.96f, 0.86f);
        [SerializeField] private Graphic tintTarget;

        private RectTransform rect;
        private Selectable selectable;
        private Color baseColor = Color.white;
        private float baseY;
        private bool isLifted;

        private void Awake()
        {
            rect = (RectTransform)transform;
            selectable = GetComponent<Selectable>();
            if (tintTarget != null) baseColor = tintTarget.color;
        }

        private void OnDisable()
        {
            // 화면이 바뀌어 꺼질 때 떠오른 상태가 남지 않게 되돌립니다.
            if (isLifted) Drop(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLifted || (selectable != null && !selectable.IsInteractable())) return;

            // 레이아웃이 위치를 바꿨을 수 있으므로 떠오르기 직전의 위치를 기준으로 삼습니다.
            rect.DOKill();
            baseY = rect.anchoredPosition.y;
            isLifted = true;

            rect.DOAnchorPosY(baseY + liftHeight, duration).SetEase(Ease.OutQuad);
            rect.DOScale(liftScale, duration).SetEase(Ease.OutQuad);
            if (tintTarget != null)
            {
                tintTarget.DOKill();
                tintTarget.DOColor(baseColor * hoverTint, duration);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isLifted) Drop(false);
        }

        private void Drop(bool isImmediate)
        {
            isLifted = false;
            rect.DOKill();
            if (tintTarget != null) tintTarget.DOKill();

            if (isImmediate)
            {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, baseY);
                rect.localScale = Vector3.one;
                if (tintTarget != null) tintTarget.color = baseColor;
                return;
            }

            rect.DOAnchorPosY(baseY, duration).SetEase(Ease.OutQuad);
            rect.DOScale(1f, duration).SetEase(Ease.OutQuad);
            if (tintTarget != null) tintTarget.DOColor(baseColor, duration);
        }
    }
}
