using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 고른 공룡의 이름과 의뢰 문구를 보여주고 청소를 시작합니다.
    /// </summary>
    public class UI_RequestDetail : MonoBehaviour, UI_IDataReceiver<StageDefinition>
    {
        private const string EmptyDescription = "(No request text yet)";

        [SerializeField] private Button backButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Image dinoImage;
        [SerializeField] private Text imagePlaceholderText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text progressText;

        private StageDefinition stage;

        /// <summary>
        /// 카드 이미지가 있으면 표시하고, 없으면 자리 표시 문구를 보여줍니다.
        /// </summary>
        public static void ApplyImage(StageDefinition stage, Image image, Text placeholder)
        {
            bool hasImage = stage.cardImage != null;
            image.sprite = stage.cardImage;
            if (hasImage) image.color = Color.white;
            placeholder.gameObject.SetActive(!hasImage);
        }

        /// <summary>
        /// 중도 포기로 저장된 진행도가 있으면 "N%"로 표시하고, 없으면 숨깁니다. 연결되지 않은 칸은 건너뜁니다.
        /// </summary>
        public static void ApplyProgress(StageDefinition stage, GameObject root, Text text)
        {
            bool hasProgress = GameServices.Progress.TryGetStageProgress(stage.stageId, out float progress);
            if (text != null && hasProgress) text.text = $"{Mathf.FloorToInt(progress * 100f)}%";

            GameObject target = root != null ? root : text != null ? text.gameObject : null;
            if (target != null) target.SetActive(hasProgress);
        }

        private void Awake()
        {
            backButton.onClick.AddListener(OnClickBack);
            startButton.onClick.AddListener(() => GameSession.EnterStage(stage));
        }

        public void ReceiveData(StageDefinition data)
        {
            stage = data;
            nameText.text = data.displayName;
            descriptionText.text = string.IsNullOrEmpty(data.description) ? EmptyDescription : data.description;
            ApplyImage(data, dinoImage, imagePlaceholderText);
            ApplyProgress(data, null, progressText);
        }

        private void OnClickBack()
        {
            UILoader.Instance.HideUI(UIName.RequestDetail);
            HomeUIController.Instance.ShowRequestList();
        }
    }
}
