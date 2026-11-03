using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 메인 로비. 게임을 켜면 가장 먼저 나오는 화면입니다.
    /// </summary>
    public class UI_Lobby : MonoBehaviour
    {
        [SerializeField] private Button requestButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            requestButton.onClick.AddListener(OnClickRequest);
            shopButton.onClick.AddListener(OnClickShop);
            settingsButton.onClick.AddListener(() => UILoader.Instance.ShowUI(UIName.Settings, SettingsMode.Menu));
            quitButton.onClick.AddListener(OnClickQuit);
        }

        private void OnClickRequest()
        {
            UILoader.Instance.HideUI(UIName.Lobby);
            HomeUIController.Instance.ShowRequestList();
        }

        private void OnClickShop()
        {
            UILoader.Instance.HideUI(UIName.Lobby);
            UILoader.Instance.ShowUI(UIName.Shop);
        }

        private void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
