namespace ServiceControl.Features
{
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using Plugin.SagaAudit;
    using ServiceControl.Plugin;

    public class SagaAudit : Feature
    {
        static ILog Log = LogManager.GetLogger<SagaAudit>();

        public SagaAudit()
        {
            EnableByDefault();
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ServiceControlBackend>(DependencyLifecycle.SingleInstance);

            context.Pipeline.Register<CaptureSagaStateRegistration>();
            context.Pipeline.Register<CaptureSagaResultingMessageRegistration>();

            Log.Warn("The ServiceControl.Plugin.Nsb5.SagaAudit package has been replaced by the NServiceBus.SagaAudit package. See the upgrade guide for more details.");
        }

        class CaptureSagaStateRegistration : RegisterStep
        {
            public CaptureSagaStateRegistration()
                : base("CaptureSagaState", typeof(CaptureSagaStateBehavior), "Records saga state changes")
            {
                InsertBefore(WellKnownStep.InvokeSaga);
            }
        }

        class CaptureSagaResultingMessageRegistration : RegisterStep
        {
            public CaptureSagaResultingMessageRegistration()
                : base("ReportSagaStateChanges", typeof(CaptureSagaResultingMessagesBehavior), "Reports the saga state changes to ServiceControl")
            {
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}