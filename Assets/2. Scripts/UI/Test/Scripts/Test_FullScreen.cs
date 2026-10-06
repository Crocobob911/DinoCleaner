using DinoCleaner.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI.Test
{
    /// <summary>
    /// UILoader 테스트용 FullScreen. 레이어 미지정(기본 FullScreen) + 데이터 없는 Show 확인용.
    /// </summary>
    public class Test_FullScreen : MonoBehaviour
    {
        [SerializeField] private Button openPopupButton;
        [SerializeField] private Button openTopButton;
        [SerializeField] private Button unloadPopupButton;

        private int openCount;

        private void Awake()
        {
            openPopupButton.onClick.AddListener(OnOpenPopup);
            openTopButton.onClick.AddListener(OnOpenTop);
            unloadPopupButton.onClick.AddListener(() => UILoader.Instance.UnloadUI("Test_Popup"));
        }

        private void OnOpenPopup()
        {
            openCount++;
            UILoader.Instance.ShowUI("Test_Popup", $"Popup 열기 {openCount}회째");
        }

        private void OnOpenTop()
        {
            UILoader.Instance.ShowUI("Test_Top", ("Top 레이어 토스트 (ValueTuple 전달)", 1.5f));
        }
    }
}
