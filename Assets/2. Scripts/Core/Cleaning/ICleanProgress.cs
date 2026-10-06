using System;
using System.Collections.Generic;

namespace DinoWash.Core
{
    // 청소 진행도를 읽기 위한 interface. For UI & Stage
    public interface ICleanProgress
    {
        // 첫 진행도 측정이 끝났는지 구분할 flag
        // gpu에서 mask정보를 가져오느라 계산이 Async로 완료되기 때문에 필요
        bool IsReady { get; }

        // 0~1 값
        float TotalProgress { get; }

        // 진행도가 갱신될 때마다 호출
        event Action<ICleanProgress> Changed;
    }
}