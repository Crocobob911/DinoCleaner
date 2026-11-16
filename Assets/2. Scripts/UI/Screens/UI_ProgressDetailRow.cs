using DinoCleaner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 진행도 상세의 부위 한 줄입니다.
    /// </summary>
    public class UI_ProgressDetailRow : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private UI_ProgressBar bar;

        public void Refresh(RegionProgress region)
        {
            nameText.text = region.DisplayName;
            bar.SetValue(region.Progress);
        }
    }
}
