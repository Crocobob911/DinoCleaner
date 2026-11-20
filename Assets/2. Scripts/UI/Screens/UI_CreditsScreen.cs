using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// credits.json으로 크레딧 텍스트를 만들고, 아래에서 위로 자동 스크롤합니다.
    /// 닫히면 OnClosed를 알리고 비활성화될 뿐, 다른 화면으로 넘어가지 않습니다.
    /// </summary>
    public class UI_CreditsScreen : MonoBehaviour, UI_ILayerInfo
    {
        [Serializable]
        private struct TextStyle
        {
            public int FontSize;
            public Color Color;
            public FontStyle FontStyle;

            public TextStyle(int fontSize, Color color, FontStyle fontStyle = FontStyle.Normal)
            {
                FontSize = fontSize;
                Color = color;
                FontStyle = fontStyle;
            }
        }

        private const string BuiltinFontName = "LegacyRuntime.ttf";

        private static int s_openCount;
        private static int s_lastEscapeCloseFrame = -1;

        [Header("Data")]
        [SerializeField] private TextAsset _creditsJson;
        [Tooltip("비어 있으면 Unity 기본 폰트(LegacyRuntime)를 씁니다.")]
        [SerializeField] private Font _font;

        [Header("Scroll")]
        [Tooltip("자동 스크롤 속도 (1920x1080 기준 px/초)")]
        [SerializeField] private float _scrollSpeed = 70f;
        [Tooltip("열린 뒤 자동 스크롤을 시작하기까지 기다리는 시간 (초)")]
        [SerializeField] private float _startDelay = 1.5f;
        [Tooltip("휠로 직접 움직인 뒤 자동 스크롤을 다시 시작하기까지 기다리는 시간 (초)")]
        [SerializeField] private float _resumeDelay = 2f;
        [Tooltip("휠 한 칸에 움직이는 거리 (px)")]
        [SerializeField] private float _wheelStep = 90f;

        [Header("References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Button _backButton;

        [Header("Text Styles")]
        [SerializeField] private TextStyle _gameTitleStyle = new TextStyle(96, new Color(1f, 0.86f, 0.45f));
        [SerializeField] private TextStyle _sectionTitleStyle = new TextStyle(56, new Color(1f, 0.78f, 0.1f));
        [SerializeField] private TextStyle _nameStyle = new TextStyle(44, new Color(0.98f, 0.94f, 0.85f));
        [SerializeField] private TextStyle _roleStyle = new TextStyle(28, new Color(0.75f, 0.7f, 0.62f), FontStyle.Italic);
        [SerializeField] private TextStyle _bodyStyle = new TextStyle(34, new Color(0.98f, 0.94f, 0.85f));
        [SerializeField] private TextStyle _assetStyle = new TextStyle(26, new Color(0.85f, 0.82f, 0.75f));
        [Tooltip("섹션 사이 빈 공간 (px)")]
        [SerializeField] private float _sectionSpacing = 80f;
        [Tooltip("사람 한 명(이름+역할) 아래 빈 공간 (px)")]
        [SerializeField] private float _personSpacing = 20f;

        private bool _isBuilt;
        private bool _isOpen;
        private float _startDelayTimer;
        private float _resumeTimer;

        /// <summary>
        /// 크레딧 화면이 닫힐 때(ESC 또는 Back) 비활성화 직전에 발생합니다.
        /// </summary>
        public event Action OnClosed;

        public EUILayer TargetLayer => EUILayer.FullScreen;

        /// <summary>
        /// 크레딧이 열려 있거나 이번 프레임에 ESC로 닫혔으면 true입니다.
        /// 다른 ESC 처리(설정창 열기 등)가 같은 키 입력에 반응하지 않도록 확인할 때 씁니다.
        /// </summary>
        public static bool IsHandlingEscape => s_openCount > 0 || s_lastEscapeCloseFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_openCount = 0;
            s_lastEscapeCloseFrame = -1;
        }

        /// <summary>
        /// 크레딧 화면을 켜고 스크롤을 처음 위치로 되돌립니다.
        /// </summary>
        public void Open()
        {
            if (gameObject.activeSelf) ResetScroll();
            else gameObject.SetActive(true);
        }

        /// <summary>
        /// OnClosed를 알린 뒤 크레딧 화면을 끕니다.
        /// </summary>
        public void Close()
        {
            if (!_isOpen) return;

            OnClosed?.Invoke();
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(Close);
            else Debug.LogWarning("[Credits] Back 버튼이 연결되지 않았습니다.", this);

            EnsureEventSystem();
        }

        private void OnEnable()
        {
            _isOpen = true;
            s_openCount++;

            BuildIfNeeded();
            ResetScroll();
        }

        private void OnDisable()
        {
            if (!_isOpen) return;
            _isOpen = false;
            s_openCount--;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                s_lastEscapeCloseFrame = Time.frameCount;
                Close();
                return;
            }

            if (_scrollRect == null || _content == null) return;

            float deltaTime = Time.unscaledDeltaTime;
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel != 0f)
                {
                    // 휠을 위로 굴리면 앞쪽(위) 내용을 다시 봅니다.
                    ScrollBy(-Mathf.Sign(wheel) * _wheelStep);
                    _resumeTimer = _resumeDelay;
                }
            }

            if (_startDelayTimer > 0f)
            {
                _startDelayTimer -= deltaTime;
                return;
            }
            if (_resumeTimer > 0f)
            {
                _resumeTimer -= deltaTime;
                return;
            }

            ScrollBy(_scrollSpeed * deltaTime);
        }

        private void OnDestroy()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(Close);
        }

        // 위쪽이 0, 아래로 갈수록 커지는 스크롤 위치. 끝에 닿으면 그 자리에서 멈춥니다.
        private void ScrollBy(float delta)
        {
            float maxY = Mathf.Max(0f, _content.rect.height - _scrollRect.viewport.rect.height);
            Vector2 position = _content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + delta, 0f, maxY);
            _content.anchoredPosition = position;
        }

        private void ResetScroll()
        {
            _startDelayTimer = _startDelay;
            _resumeTimer = 0f;
            if (_scrollRect == null || _content == null) return;

            _scrollRect.StopMovement();
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0f);
        }

        private void BuildIfNeeded()
        {
            if (_isBuilt) return;
            _isBuilt = true;

            if (_content == null || _scrollRect == null)
            {
                Debug.LogWarning("[Credits] ScrollRect 또는 Content가 연결되지 않았습니다.", this);
                return;
            }
            if (_creditsJson == null)
            {
                Debug.LogWarning("[Credits] creditsJson이 비어 있어 표시할 내용이 없습니다.", this);
                return;
            }

            CreditsData data = CreditsFormatter.Parse(_creditsJson.text, _creditsJson.name);
            if (data == null) return;

            Font font = _font != null ? _font : Resources.GetBuiltinResource<Font>(BuiltinFontName);

            if (!string.IsNullOrWhiteSpace(data.gameTitle))
                CreateText(data.gameTitle, _gameTitleStyle, font);

            List<CreditsSectionView> sections = CreditsFormatter.BuildSections(data, _creditsJson.name);
            foreach (CreditsSectionView section in sections)
            {
                CreateSpacer(_sectionSpacing);
                CreateText(section.Title, _sectionTitleStyle, font);
                foreach (CreditsLine line in section.Lines)
                {
                    CreateLine(line, font);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        // 새 줄 스타일이 생기면 여기에 case 하나만 추가합니다.
        private void CreateLine(CreditsLine line, Font font)
        {
            switch (line.Style)
            {
                case CreditsLineStyle.Person:
                    CreateText(line.Text, _nameStyle, font);
                    if (!string.IsNullOrEmpty(line.Detail)) CreateText(line.Detail, _roleStyle, font);
                    CreateSpacer(_personSpacing);
                    break;
                case CreditsLineStyle.Text:
                    CreateText(line.Text, _bodyStyle, font);
                    break;
                case CreditsLineStyle.Asset:
                    CreateText(line.Text, _assetStyle, font);
                    break;
                default:
                    Debug.LogWarning($"[Credits] 화면에 표시할 방법이 없는 줄 스타일입니다: {line.Style}", this);
                    CreateText(line.Text, _bodyStyle, font);
                    break;
            }
        }

        private void CreateText(string value, TextStyle style, Font font)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_content, false);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = style.FontSize;
            text.color = style.Color;
            text.fontStyle = style.FontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
        }

        private void CreateSpacer(float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(_content, false);
            go.GetComponent<LayoutElement>().minHeight = height;
        }

        // 빈 씬에서 바로 띄워도 Back 버튼을 누를 수 있도록, EventSystem이 없을 때만 하나 만듭니다.
        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Debug.Log("[Credits] 씬에 EventSystem이 없어 새로 만들었습니다.", go);
        }
    }
}
