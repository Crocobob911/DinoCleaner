using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 로비의 Credits 버튼에 붙여, 누르면 크레딧 화면을 로비 위에 띄웁니다.
    /// 크레딧 인스턴스는 처음 한 번만 만들고 이후에는 재사용합니다. 로비 오브젝트와 코드는 건드리지 않습니다.
    /// </summary>
    public class UI_CreditsLauncher : MonoBehaviour
    {
        [Tooltip("비어 있으면 같은 오브젝트의 Button을 씁니다.")]
        [SerializeField] private Button _button;
        [SerializeField] private UI_CreditsScreen _creditsPrefab;
        [Tooltip("비어 있으면 씬 루트에 크레딧 전용 Canvas로 생성합니다.")]
        [SerializeField] private Transform _spawnParent;

        private static readonly Vector2 FallbackReferenceResolution = new Vector2(1920f, 1080f);

        private UI_CreditsScreen _creditsInstance;

        // 크레딧의 ESC 닫기와 HomeUIController의 ESC(설정창 열기)가 같은 키 입력에 함께 반응하지 않도록,
        // 크레딧이 열린 동안 HomeUIController를 잠시 끄고 닫힌 다음 프레임에 다시 켭니다.
        private HomeUIController _suspendedEscapeHandler;
        private int _closedFrame = -1;

        /// <summary>
        /// 크레딧 화면을 엽니다. 처음 호출할 때 프리팹을 생성합니다.
        /// </summary>
        public void OpenCredits()
        {
            if (!EnsureInstance()) return;

            SuspendEscapeHandler();
            _creditsInstance.Open();

            // 로비 버튼이 선택된 채로 남으면 Enter/Space로 뒤의 버튼이 눌리므로 선택을 비웁니다.
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();

            if (_button != null) _button.onClick.AddListener(OpenCredits);
            else Debug.LogWarning("[Credits] UI_CreditsLauncher에 Button이 연결되지 않았습니다.", this);
        }

        private void Update()
        {
            if (_suspendedEscapeHandler != null && _closedFrame >= 0 && Time.frameCount > _closedFrame)
            {
                ResumeEscapeHandler();
            }
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OpenCredits);

            ResumeEscapeHandler();

            if (_creditsInstance != null)
            {
                _creditsInstance.OnClosed -= HandleCreditsClosed;
                // 로비가 UILoader로 해제될 때 크레딧만 씬에 남지 않도록 함께 정리합니다.
                Destroy(_creditsInstance.gameObject);
                _creditsInstance = null;
            }
        }

        private bool EnsureInstance()
        {
            if (_creditsInstance != null) return true;

            if (_creditsPrefab == null)
            {
                Debug.LogWarning("[Credits] UI_CreditsLauncher에 크레딧 프리팹이 연결되지 않았습니다.", this);
                return false;
            }

            _creditsInstance = Instantiate(_creditsPrefab, _spawnParent, false);
            _creditsInstance.name = _creditsPrefab.name;
            _creditsInstance.OnClosed += HandleCreditsClosed;
            PlaceAboveLobby(_creditsInstance.gameObject);
            return true;
        }

        // 크레딧 Canvas를 로비가 속한 최상위 Canvas보다 위에 그리고, Raycast도 크레딧이 먼저 받게 합니다.
        private void PlaceAboveLobby(GameObject credits)
        {
            if (!credits.TryGetComponent(out Canvas canvas))
            {
                Debug.LogWarning("[Credits] 크레딧 프리팹 루트에 Canvas가 없어 새로 추가합니다.", credits);
                canvas = credits.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = credits.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = FallbackReferenceResolution;
            }
            if (!credits.TryGetComponent(out GraphicRaycaster _)) credits.AddComponent<GraphicRaycaster>();

            if (_spawnParent != null) canvas.overrideSorting = true;

            Canvas lobbyCanvas = GetComponentInParent<Canvas>();
            if (lobbyCanvas == null) return;

            int minOrder = lobbyCanvas.rootCanvas.sortingOrder + 1;
            if (canvas.sortingOrder < minOrder) canvas.sortingOrder = minOrder;
        }

        private void HandleCreditsClosed()
        {
            _closedFrame = Time.frameCount;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && _button != null && _button.isActiveAndEnabled)
            {
                eventSystem.SetSelectedGameObject(_button.gameObject);
            }
        }

        private void SuspendEscapeHandler()
        {
            _closedFrame = -1;
            if (_suspendedEscapeHandler != null) return;

            HomeUIController handler = HomeUIController.Instance;
            if (handler == null || !handler.enabled) return;

            handler.enabled = false;
            _suspendedEscapeHandler = handler;
        }

        private void ResumeEscapeHandler()
        {
            if (_suspendedEscapeHandler != null) _suspendedEscapeHandler.enabled = true;
            _suspendedEscapeHandler = null;
            _closedFrame = -1;
        }
    }
}
