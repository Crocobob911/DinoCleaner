using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoCleaner.UI
{
    /// <summary>
    /// Home 씬의 UI 입력과 의뢰 목록을 맡습니다.
    /// 스테이지 참조를 UI 프리팹이 아닌 씬에 두어, UILoader가 씬 전환 때 Addressables를 Release해도
    /// GameSession.CurrentStage가 함께 해제되지 않게 합니다.
    /// </summary>
    public class HomeUIController : MonoBehaviour
    {
        public static HomeUIController Instance { get; private set; }

        [Tooltip("의뢰 리스트에 나올 스테이지들 (왼쪽부터 순서대로)")]
        [SerializeField] private StageDefinition[] stages;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowRequestList()
        {
            UILoader.Instance.ShowUI(UIName.RequestList, stages);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                UI_Settings.ToggleByEscape(SettingsMode.Menu);
            }
        }
    }
}
