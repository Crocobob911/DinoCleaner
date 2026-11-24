namespace DinoCleaner.Core
{
    // 돌발 상황 시작 (UI 경고 배너, 효과음)
    public readonly struct IncidentStartedEvent : IEvent
    {
        public readonly IncidentDefinition Incident;
        public IncidentStartedEvent(IncidentDefinition incident) { Incident = incident; }
    }

    // 돌발 상황 끝 (배너 닫기)
    public readonly struct IncidentEndedEvent : IEvent
    {
        public readonly IncidentDefinition Incident;
        public IncidentEndedEvent(IncidentDefinition incident) { Incident = incident; }
    }
}
