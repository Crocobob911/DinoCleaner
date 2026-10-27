using System;

namespace DinoCleaner.Core
{
    public static class EventBus<T> where T : struct, IEvent
    {
        private static event Action<T> onEvent;
        public static void Unsubscribe(Action<T> listener)
        {
            onEvent -= listener;
        }

        public static void Subscribe(Action<T> listener)
        {
            onEvent += listener;
        }

        public static void Publish(T e)
        {
            onEvent?.Invoke(e);
        }


    }
}
