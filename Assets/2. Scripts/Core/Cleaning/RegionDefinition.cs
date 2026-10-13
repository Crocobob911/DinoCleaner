using System;
using UnityEngine;

namespace DinoCleaner.Core
{
    [Serializable]
    public class RegionDefinition
    {
        [Tooltip("1~255. 부위 맵 텍스처의 R 값(0~255)과 같아야 함(0은 '부위 없음')")]
        public int id = 1;

        [Tooltip("UI 체크리스트에 보일 이름")]
        public string displayName = "Head";
    }
}
