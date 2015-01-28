namespace ServiceControl.Plugin.SagaAudit.Sample
{
    using NServiceBus;
    using NServiceBus.Features;

    class CustomInitializer : INeedInitialization
    {
        public void Customize(BusConfiguration builder)
        {
            builder.DisableFeature<SecondLevelRetries>();
            builder.UseSerialization<JsonSerializer>();
        }
    }
}