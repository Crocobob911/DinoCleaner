using UnityEngine;
using DinoCleaner.Data;

namespace DinoCleaner.Core
{
    public readonly struct CleaningToolStateChangedEvent : IEvent
    {
        public readonly int CurrentLength;
        public readonly int CurrentWidth;
        public readonly int MaxWidthLimit; // UI가 빈 칸을 표시할 수 있게 최대치도 전달 (선택)
        public readonly CastShape castShape;
        public readonly CleanerFlags flags;
        public readonly NozzleDirection Direction;

        public CleaningToolStateChangedEvent(int length, int width, int maxWidth, CastShape shape, CleanerFlags cleanerFlags, NozzleDirection direction)
        {
            CurrentLength = length;
            CurrentWidth = width;
            MaxWidthLimit = maxWidth;
            castShape = shape;
            flags = cleanerFlags;
            Direction = direction;
        }
    }

    // 청소기 <-> 건설기 전환 이벤트 (건설기일 때는 UI 가리기)
    public readonly struct CurrentToolChangedEvent : IEvent
    {
        public readonly ToolType toolType;

        public CurrentToolChangedEvent(ToolType tool)
        {
            toolType = tool;
        }
    }

    public readonly struct UsingFairyDinoEvent : IEvent
    {
        public readonly bool isUsing;

        public UsingFairyDinoEvent(bool usingFairy)
        {
            isUsing = usingFairy;
        }
    }
}
