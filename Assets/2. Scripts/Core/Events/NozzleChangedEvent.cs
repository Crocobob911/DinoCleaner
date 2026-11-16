using UnityEngine;

namespace DinoCleaner.Core
{
    public readonly struct NozzleChangedEvent : IEvent
    {
        public readonly CleaningToolData currentTool;
        public NozzleChangedEvent(CleaningToolData next) {
            currentTool = next;
        }
    }
}
