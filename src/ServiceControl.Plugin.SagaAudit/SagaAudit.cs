﻿namespace ServiceControl.Features
{
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using Plugin.SagaAudit;

    public class SagaAudit : Feature
    {
        public SagaAudit()
        {
            EnableByDefault();
            DependsOn<Sagas>();
        }
        
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<CaptureSagaStateRegistration>();
            context.Pipeline.Register<CaptureSagaResultingMessageRegistration>();
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
                InsertAfter(WellKnownStep.InvokeSaga);
            }
        }
    }
}