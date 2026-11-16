using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 인게임 HUD. 진행도, 에임, 자세, 도구, 노즐 단계를 표시합니다.
    /// </summary>
    public class UI_HUD : MonoBehaviour, UI_IDataReceiver<(ICleanProgress progress, PlayerHudState state)>
    {
        private const float RegionChangeEpsilon = 0.001f;
        private const float EllipseGapScale = 1.4f;

        private static readonly float[] AimGaps = { 44f, 60f, 80f, 104f };
        private static readonly string[] PostureNames = { "서기", "앉기", "눕기", "날기" };

        [Header("Progress")]
        [SerializeField] private Text currentRegionText;
        [SerializeField] private UI_ProgressBar regionBar;
        [SerializeField] private UI_ProgressBar totalBar;

        [Header("Aim")]
        [SerializeField] private RectTransform aimRoot;
        [SerializeField] private UI_IconOrText firstBracket;
        [SerializeField] private UI_IconOrText secondBracket;
        [SerializeField] private UI_IconOrText aimDot;
        [Tooltip("노즐 단계 색이 곱해지므로 흰색으로 그린 Sprite를 권장합니다.")]
        [SerializeField] private Sprite firstBracketSprite;
        [SerializeField] private Sprite secondBracketSprite;
        [SerializeField] private Sprite waterAimSprite;
        [SerializeField] private Sprite foamAimSprite;

        [Header("Status")]
        [SerializeField] private UI_IconOrText postureIcon;
        [Tooltip("서기, 앉기, 눕기, 날기 순서")]
        [SerializeField] private Sprite[] postureSprites = new Sprite[4];
        [SerializeField] private UI_IconOrText toolIcon;
        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite foamSprite;
        [SerializeField] private Image nozzleIcon;
        [SerializeField] private Text nozzleNumberText;
        [Tooltip("1~4단계 순서. Sprite를 지정한 단계는 색을 곱하지 않습니다.")]
        [SerializeField] private Sprite[] nozzleSprites = new Sprite[4];
        [SerializeField] private Color[] nozzleColors =
        {
            new Color(0.84f, 0.16f, 0.16f),
            new Color(1f, 0.75f, 0f),
            new Color(0f, 0.69f, 0.31f),
            Color.white,
        };

        private Sprite defaultNozzleSprite;

        private readonly List<float> lastRegionProgress = new();
        private ICleanProgress progress;
        private PlayerHudState state = PlayerHudState.Default;
        private int currentRegionIndex = -1;

        private void Awake()
        {
            defaultNozzleSprite = nozzleIcon.sprite;
        }

        private void OnEnable()
        {
            EventBus<PlayerHudStateChangedEvent>.Subscribe(OnHudStateChanged);
            if (progress != null) progress.Changed += OnProgressChanged;
        }

        private void OnDisable()
        {
            EventBus<PlayerHudStateChangedEvent>.Unsubscribe(OnHudStateChanged);
            if (progress != null) progress.Changed -= OnProgressChanged;
        }

        public void ReceiveData((ICleanProgress progress, PlayerHudState state) data)
        {
            if (progress != null) progress.Changed -= OnProgressChanged;
            progress = data.progress;
            if (progress != null) progress.Changed += OnProgressChanged;

            state = data.state;
            lastRegionProgress.Clear();
            currentRegionIndex = -1;

            RefreshProgress();
            RefreshState();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (!state.IsAimFree || mouse == null)
            {
                aimRoot.anchoredPosition = Vector2.zero;
                return;
            }

            var parent = (RectTransform)aimRoot.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, mouse.position.ReadValue(), null, out Vector2 local))
            {
                aimRoot.anchoredPosition = local;
            }
        }

        private void OnProgressChanged(ICleanProgress source) => RefreshProgress();

        private void OnHudStateChanged(PlayerHudStateChangedEvent e)
        {
            state = e.State;
            RefreshState();
        }

        private void RefreshProgress()
        {
            if (progress == null || !progress.IsReady)
            {
                currentRegionText.text = "현재 부위 : 측정 중";
                regionBar.SetValue(0f);
                totalBar.SetValue(0f);
                return;
            }

            totalBar.SetValue(progress.TotalProgress);
            UpdateCurrentRegion(progress.Regions);

            if (currentRegionIndex < 0)
            {
                currentRegionText.text = "현재 부위 : -";
                regionBar.SetValue(0f);
                return;
            }

            RegionProgress region = progress.Regions[currentRegionIndex];
            currentRegionText.text = $"현재 부위 : {region.DisplayName}";
            regionBar.SetValue(region.Progress);
        }

        // 조준 부위 API가 없어, 진행도가 가장 최근에 오른 부위를 현재 부위로 봅니다.
        private void UpdateCurrentRegion(IReadOnlyList<RegionProgress> regions)
        {
            if (lastRegionProgress.Count != regions.Count)
            {
                lastRegionProgress.Clear();
                for (int i = 0; i < regions.Count; i++) lastRegionProgress.Add(regions[i].Progress);
                return;
            }

            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i].Progress > lastRegionProgress[i] + RegionChangeEpsilon) currentRegionIndex = i;
                lastRegionProgress[i] = regions[i].Progress;
            }
        }

        private void RefreshState()
        {
            int levelIndex = Mathf.Clamp(state.NozzleLevel - PlayerHudState.MinNozzleLevel, 0, AimGaps.Length - 1);
            Color color = GetAt(nozzleColors, levelIndex, Color.white);
            bool isWater = state.Tool == CleaningToolType.Water;

            postureIcon.Set(GetAt(postureSprites, (int)state.Posture, null), $"자세\n{PostureNames[(int)state.Posture]}");
            toolIcon.Set(isWater ? waterSprite : foamSprite, isWater ? "물" : "거품");

            Sprite nozzleSprite = GetAt(nozzleSprites, levelIndex, null);
            nozzleIcon.sprite = nozzleSprite != null ? nozzleSprite : defaultNozzleSprite;
            nozzleIcon.color = nozzleSprite != null ? Color.white : color;
            nozzleNumberText.text = state.NozzleLevel.ToString();

            firstBracket.Set(firstBracketSprite, "(");
            secondBracket.Set(secondBracketSprite, ")");
            firstBracket.SetColor(color);
            secondBracket.SetColor(color);
            aimDot.Set(isWater ? waterAimSprite : foamAimSprite, isWater ? "●" : "○");

            float gap = AimGaps[levelIndex] * (state.Shape == AimShape.Circle ? 1f : EllipseGapScale);
            bool isVertical = state.Shape == AimShape.VerticalEllipse;

            // 세로 타원은 괄호를 90도 돌려 위아래에 둡니다.
            RectTransform first = firstBracket.Rect;
            RectTransform second = secondBracket.Rect;
            first.anchoredPosition = isVertical ? Vector2.up * gap : Vector2.left * gap;
            second.anchoredPosition = isVertical ? Vector2.down * gap : Vector2.right * gap;
            Quaternion rotation = Quaternion.Euler(0f, 0f, isVertical ? -90f : 0f);
            first.localRotation = rotation;
            second.localRotation = rotation;
        }

        private static T GetAt<T>(T[] items, int index, T fallback)
        {
            return items != null && index < items.Length ? items[index] : fallback;
        }
    }
}
