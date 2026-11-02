namespace DinoCleaner.Core
{
    public interface IEvent { }

    public readonly struct TotalCompleteEvent : IEvent
    {
        
    }
   
    public readonly struct RegionCompleteEvent : IEvent
    {
        public readonly ICleanProgress Source; // 어느 물체인지
        public readonly RegionProgress Region; // 어느 부위인지
        public RegionCompleteEvent(ICleanProgress source, RegionProgress region)
        {
            Source = source;
            Region = region;
        }
    }
}
