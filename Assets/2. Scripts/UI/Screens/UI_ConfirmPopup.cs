using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    public readonly struct ConfirmPopupData
    {
        public readonly string Message;
        public readonly Action OnConfirm;

        public ConfirmPopupData(string message, Action onConfirm)
        {
            Message = message;
            OnConfirm = onConfirm;
        }
    }

    /// <summary>
    /// 문구와 확인 동작을 전달받는 공용 확인 팝업입니다.
    /// </summary>
    public class UI_ConfirmPopup : UI_PopupBase, UI_IDataReceiver<ConfirmPopupData>
    {
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnClickConfirm);
            cancelButton.onClick.AddListener(() => UILoader.Instance.HideUI(UIName.ConfirmPopup));
        }

        public void ReceiveData(ConfirmPopupData data)
        {
            messageText.text = data.Message;
            onConfirm = data.OnConfirm;
        }

        private void OnClickConfirm()
        {
            UILoader.Instance.HideUI(UIName.ConfirmPopup);
            onConfirm?.Invoke();
        }
    }
}
