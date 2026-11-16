using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 청소 완료 정산. 이번 수입과 보유 돈을 보여줍니다.
    /// </summary>
    public class UI_Settlement : MonoBehaviour, UI_IDataReceiver<StageResult>
    {
        [SerializeField] private Text summaryText;
        [SerializeField] private Button stayButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            stayButton.onClick.AddListener(() => UILoader.Instance.HideUI(UIName.Settlement));
            homeButton.onClick.AddListener(OnClickHome);
        }

        public void ReceiveData(StageResult result)
        {
            summaryText.text = $"수입 : {result.Reward}    보유액 : {GameServices.Progress.Money}";
        }

        private void OnClickHome()
        {
            GameServices.Progress.SaveCleaningProgress();
            GameSession.GoHome();
        }
    }
}
