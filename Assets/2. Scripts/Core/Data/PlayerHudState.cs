namespace DinoCleaner.Core
{
    public enum PlayerPosture { Stand, Crouch, Prone, Fly }
    public enum CleaningToolType { Water, Foam }
    public enum AimShape { Circle, HorizontalEllipse, VerticalEllipse }

    // HUD에 표시할 플레이어 상태. Player 모듈이 PlayerHudStateChangedEvent로 알려준다.
    public readonly struct PlayerHudState
    {
        public const int MinNozzleLevel = 1;
        public const int MaxNozzleLevel = 4;

        public readonly PlayerPosture Posture;
        public readonly CleaningToolType Tool;
        public readonly int NozzleLevel;    // 1(가장 좁음) ~ 4(가장 넓음)
        public readonly AimShape Shape;
        public readonly bool IsAimFree;     // true면 카메라는 고정되고 에임만 움직임

        public static PlayerHudState Default =>
            new PlayerHudState(PlayerPosture.Stand, CleaningToolType.Water, MinNozzleLevel, AimShape.Circle, false);

        public PlayerHudState(PlayerPosture posture, CleaningToolType tool, int nozzleLevel, AimShape shape, bool isAimFree)
        {
            Posture = posture;
            Tool = tool;
            NozzleLevel = nozzleLevel;
            Shape = shape;
            IsAimFree = isAimFree;
        }
    }
}
