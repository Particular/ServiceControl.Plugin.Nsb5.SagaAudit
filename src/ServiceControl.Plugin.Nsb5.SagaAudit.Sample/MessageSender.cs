namespace Server
{
    using System;
    using NServiceBus;

    class MessageSender : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press Enter to start a saga");
            while (Console.ReadLine() != null)
            {
                Bus.SendLocal(new Message1
                {
                    SomeId = Guid.NewGuid()
                });
            }
        }

        public void Stop()
        {
        }
    }
}