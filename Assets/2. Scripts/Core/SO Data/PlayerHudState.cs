namespace DinoCleaner.Core
{
    public enum PlayerPosture { Stand, Crouch, Prone, Fly }
    public enum CleaningToolType { Water, Foam }
    public enum AimShape { Circle, HorizontalEllipse, VerticalEllipse }

    // HUD에 표시할 플레이어 상태. UI의 PlayerHudBridge가 Player 이벤트를 모아 PlayerHudStateChangedEvent로 알려준다.
    public readonly struct PlayerHudState
    {
        public const int MinNozzleLevel = 1;
        public const int MaxNozzleLevel = 4;

        public readonly PlayerPosture Posture;
        public readonly CleaningToolType Tool;
        public readonly int NozzleLevel;    // 1(가장 좁음) ~ 4(가장 넓음)
        public readonly AimShape Shape;
        public readonly bool IsAimFree;     // true면 카메라는 고정되고 에임만 움직임
        public readonly bool IsCleaningToolEquipped; // false면 사다리 등 다른 도구를 든 상태
        public readonly int NozzleLength;   // 1(가장 짧음) ~ 4(가장 김)

        // 청소 도구의 시작 상태(물, 1단계, 가로, 길이 1)와 같습니다.
        public static PlayerHudState Default =>
            new PlayerHudState(PlayerPosture.Stand, CleaningToolType.Water, MinNozzleLevel, AimShape.HorizontalEllipse, false, true,
                               MinNozzleLevel);

        public PlayerHudState(PlayerPosture posture, CleaningToolType tool, int nozzleLevel, AimShape shape,
                              bool isAimFree, bool isCleaningToolEquipped, int nozzleLength = MinNozzleLevel)
        {
            Posture = posture;
            Tool = tool;
            NozzleLevel = nozzleLevel;
            Shape = shape;
            IsAimFree = isAimFree;
            IsCleaningToolEquipped = isCleaningToolEquipped;
            NozzleLength = nozzleLength;
        }
    }
}
