using DG.Tweening;
using DinoCleaner.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.Dev.DngU
{
    /// <summary>
    /// UILoader 테스트용 Top. Top 레이어 + ValueTuple 데이터 수신 + 연출 없는 Hide 확인용.
    /// </summary>
    public class Test_Top : MonoBehaviour, UI_ILayerInfo, UI_IDataReceiver<(string message, float seconds)>
    {
        public EUILayer TargetLayer => EUILayer.Top;

        [SerializeField] private Text messageText;

        private Tween hideTimer;

        public void ReceiveData((string message, float seconds) data)
        {
            messageText.text = data.message;

            hideTimer?.Kill();
            hideTimer = DOVirtual.DelayedCall(data.seconds, () => UILoader.Instance.HideUI("Test_Top"));
        }

        private void OnDisable()
        {
            hideTimer?.Kill();
        }
    }
}
