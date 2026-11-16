using System;
using System.Collections.Generic;

namespace DinoCleaner.Core
{
    /// <summary>
    /// 공룡(스테이지)별 진행도 슬롯 — save.json 안에 저장되는 "가벼운 정보"라 스테이지 밖(의뢰 게시판)에서도 읽을 수 있음.
    /// "무거운 정보"인 오물 지도 PNG는 StageSlots.DirtPath(stageId) 경로에 따로 둠 (쓰고 읽는 건 스테이지 쪽 담당).
    /// 읽기/쓰기는 StageSlots를 통해서.
    /// </summary>
    [Serializable]
    public class StageSlot
    {
        public int stageId;
        public float progress;                       // 전체 진행도 0~1 (게시판 "N% 진행 중" 표시용)
        public float washTime;                       // 청소 시간(초)
        public List<RegionSlot> regions = new();     // 부위별 진행도·완료 여부

        // 없으면 null
        public RegionSlot GetRegion(int regionId) => regions.Find(r => r.id == regionId);
    }

    // 부위 하나의 진행도
    [Serializable]
    public class RegionSlot
    {
        public int id;           // RegionDefinition.id
        public float progress;   // 0~1
        public bool completed;   // 한 번 완료된 부위인지 (완료 알림이 다시 울리지 않게 할 때 사용)
    }
}
