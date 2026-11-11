using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    public enum SettingsMode
    {
        Menu,
        InGame
    }

    /// <summary>
    /// 메뉴와 인게임이 함께 쓰는 설정 팝업. 인게임에서는 저장하기/돌아가기 버튼이 추가됩니다.
    /// </summary>
    public class UI_Settings : UI_PopupBase, UI_IDataReceiver<SettingsMode>
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Text bgmValueText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Text sfxValueText;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityValueText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject inGameButtons;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button returnButton;

        /// <summary>
        /// ESC 입력 처리: 확인 팝업 → 설정 순서로 닫고, 둘 다 닫혀 있으면 설정을 엽니다.
        /// </summary>
        public static void ToggleByEscape(SettingsMode mode)
        {
            UILoader loader = UILoader.Instance;
            if (loader.IsShowing(UIName.ConfirmPopup)) loader.HideUI(UIName.ConfirmPopup);
            else if (loader.IsShowing(UIName.Settings)) loader.HideUI(UIName.Settings);
            else loader.ShowUI(UIName.Settings, mode);
        }

        private void Awake()
        {
            IGameSettings settings = GameServices.Settings;

            bgmSlider.onValueChanged.AddListener(value =>
            {
                settings.BgmVolume = Mathf.RoundToInt(value);
                bgmValueText.text = settings.BgmVolume.ToString();
            });
            sfxSlider.onValueChanged.AddListener(value =>
            {
                settings.SfxVolume = Mathf.RoundToInt(value);
                sfxValueText.text = settings.SfxVolume.ToString();
            });
            sensitivitySlider.onValueChanged.AddListener(value =>
            {
                settings.MouseSensitivity = Mathf.RoundToInt(value);
                sensitivityValueText.text = settings.MouseSensitivity.ToString();
            });

            closeButton.onClick.AddListener(() => UILoader.Instance.HideUI(UIName.Settings));
            saveButton.onClick.AddListener(() => GameServices.Progress.SaveCleaningProgress());
            returnButton.onClick.AddListener(OnClickReturn);
        }

        // 설정 창이 닫힐 때 바뀐 설정을 한 번에 파일로 저장
        protected override void OnDisable()
        {
            base.OnDisable();
            SettingsSystem.SaveIfDirty();
        }

        public void ReceiveData(SettingsMode mode)
        {
            IGameSettings settings = GameServices.Settings;
            bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
            sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
            sensitivitySlider.SetValueWithoutNotify(settings.MouseSensitivity);
            bgmValueText.text = settings.BgmVolume.ToString();
            sfxValueText.text = settings.SfxVolume.ToString();
            sensitivityValueText.text = settings.MouseSensitivity.ToString();

            inGameButtons.SetActive(mode == SettingsMode.InGame);
        }

        private void OnClickReturn()
        {
            UILoader.Instance.ShowUI(UIName.ConfirmPopup, new ConfirmPopupData("집으로 돌아가시겠습니까?", () =>
            {
                GameServices.Progress.SaveCleaningProgress();
                GameSession.GoHome();
            }));
        }
    }
}
