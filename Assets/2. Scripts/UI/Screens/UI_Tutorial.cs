using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 튜토리얼 안내 창. 진행 판정과 완료 저장은 Tutorial 모듈의 TutorialDataManager가 하고,
    /// 이 창은 StageUIController가 전달한 단계를 보여주기만 합니다.
    /// </summary>
    public class UI_Tutorial : MonoBehaviour, UI_ILayerInfo, UI_IDataReceiver<TutorialStepChangedEvent>
    {
        public EUILayer TargetLayer => EUILayer.Top;

        [SerializeField] private Text stepText;
        [SerializeField] private Text messageText;
        [Tooltip("단계를 완료하고 다음 단계를 기다리는 동안 켜지는 표시")]
        [SerializeField] private GameObject completedMark;

        private void OnEnable()
        {
            EventBus<TutorialStepCompletedEvent>.Subscribe(OnStepCompleted);
        }

        private void OnDisable()
        {
            EventBus<TutorialStepCompletedEvent>.Unsubscribe(OnStepCompleted);
        }

        public void ReceiveData(TutorialStepChangedEvent data)
        {
            stepText.text = $"TUTORIAL {data.StepNumber}/{data.StepCount}";
            messageText.text = data.Message;
            completedMark.SetActive(false);
        }

        private void OnStepCompleted(TutorialStepCompletedEvent e) => completedMark.SetActive(true);
    }
}
