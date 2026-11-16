namespace DinoCleaner.UI
{
    /// <summary>
    /// UILoader.ShowUI&lt;T&gt;로 데이터를 전달받기 위한 인터페이스입니다.
    /// 다중 데이터는 ValueTuple 형태(예: UI_IDataReceiver&lt;(string, int)&gt;)로 구현합니다.
    /// </summary>
    public interface UI_IDataReceiver<in T>
    {
        void ReceiveData(T data);
    }
}
