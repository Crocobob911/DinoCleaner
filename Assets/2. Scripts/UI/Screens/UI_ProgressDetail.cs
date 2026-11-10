using System;
using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.UI
{
    /// <summary>
    /// Tab으로 여는 부위별 진행도 창. 청소를 계속할 수 있도록 입력은 막지 않습니다.
    /// </summary>
    public class UI_ProgressDetail : UI_PopupBase, UI_IDataReceiver<ICleanProgress>
    {
        protected override bool IsModal => false;

        [SerializeField] private UI_ProgressDetailRow rowTemplate;
        [SerializeField] private UI_ProgressBar totalBar;
        [SerializeField] private GameObject measuringLabel;

        private readonly List<UI_ProgressDetailRow> rows = new();
        private ICleanProgress progress;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (progress != null) progress.Changed += OnProgressChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (progress != null) progress.Changed -= OnProgressChanged;
        }

        public void ReceiveData(ICleanProgress data)
        {
            if (progress != null) progress.Changed -= OnProgressChanged;
            progress = data;
            progress.Changed += OnProgressChanged;
            Refresh();
        }

        private void OnProgressChanged(ICleanProgress source) => Refresh();

        private void Refresh()
        {
            bool isReady = progress.IsReady;
            measuringLabel.SetActive(!isReady);
            totalBar.SetValue(isReady ? progress.TotalProgress : 0f);

            IReadOnlyList<RegionProgress> regions = isReady ? progress.Regions : Array.Empty<RegionProgress>();
            while (rows.Count < regions.Count)
            {
                UI_ProgressDetailRow row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                bool isUsed = i < regions.Count;
                rows[i].gameObject.SetActive(isUsed);
                if (isUsed) rows[i].Refresh(regions[i]);
            }
        }
    }
}
