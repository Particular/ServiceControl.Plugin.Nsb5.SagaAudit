namespace ServiceControl.Plugin.SagaAudit
{
    using System;
    using EndpointPlugin.Messages.SagaState;
    using NServiceBus;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;

    class CaptureSagaStateBehavior : IBehavior<IncomingContext>
    {
        Configure configure;
        ISendMessages sendMessages;
        readonly CriticalError criticalError;

        public CaptureSagaStateBehavior(Configure configure, ISendMessages sendMessages, CriticalError criticalError)
        {
            this.configure = configure;
            this.sendMessages = sendMessages;
            this.criticalError = criticalError;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            var saga = context.MessageHandler.Instance as Saga;

            if (saga == null)
            {
                next();
                return;
            }

            sagaAudit = new SagaUpdatedMessage
                {
                    StartTime = DateTime.UtcNow
                };
            context.Set(sagaAudit);
            next();

            if (saga.Entity == null)
            {
                return; // Message was not handled by the saga
            }

            sagaAudit.FinishTime = DateTime.UtcNow;
            AuditSaga(saga, context);
        }

        void AuditSaga(Saga saga, IncomingContext context)
        {
            string messageId;

            if (!context.IncomingLogicalMessage.Headers.TryGetValue(Headers.MessageId, out messageId))
            {
                return;
            }

            var activeSagaInstance = context.Get<ActiveSagaInstance>();
            var sagaStateString = Serializer.Serialize(saga.Entity);
            var headers = context.IncomingLogicalMessage.Headers;
            var originatingMachine = headers["NServiceBus.OriginatingMachine"];
            var originatingEndpoint = headers[Headers.OriginatingEndpoint];
            var timeSent = DateTimeExtensions.ToUtcDateTime(headers[Headers.TimeSent]);
            var intent = headers.ContainsKey(Headers.MessageIntent) ? headers[Headers.MessageIntent] : "Send"; // Just in case the received message is from an early version that does not have intent, should be a rare occasion.

            sagaAudit.Initiator = new SagaChangeInitiator
                {
                    IsSagaTimeoutMessage = IsTimeoutMessage(context.IncomingLogicalMessage),
                    InitiatingMessageId = messageId,
                    OriginatingMachine = originatingMachine,
                    OriginatingEndpoint = originatingEndpoint,
                    MessageType = context.IncomingLogicalMessage.MessageType.FullName,
                    TimeSent = timeSent,
                    Intent = intent
                };
            sagaAudit.IsNew = activeSagaInstance.IsNew;
            sagaAudit.IsCompleted = saga.Completed;
            sagaAudit.Endpoint = configure.Settings.EndpointName();
            sagaAudit.SagaId = saga.Entity.Id;
            sagaAudit.SagaType = saga.GetType().FullName;
            sagaAudit.SagaState = sagaStateString;

            AssignSagaStateChangeCausedByMessage(context);

            var backend = new ServiceControlBackend(sendMessages, configure, criticalError);
            backend.Send(sagaAudit);
        }

        void AssignSagaStateChangeCausedByMessage(IncomingContext context)
        {
            string sagaStateChange;

            if (!context.PhysicalMessage.Headers.TryGetValue("ServiceControl.SagaStateChange", out sagaStateChange))
            {
                sagaStateChange = String.Empty;
            }

            var statechange = "Updated";
            if (sagaAudit.IsNew)
            {
                statechange = "New";
            }
            if (sagaAudit.IsCompleted)
            {
                statechange = "Completed";
            }

            if (!String.IsNullOrEmpty(sagaStateChange))
            {
                sagaStateChange += ";";
            }
            sagaStateChange += String.Format("{0}:{1}", sagaAudit.SagaId, statechange);

            context.PhysicalMessage.Headers["ServiceControl.SagaStateChange"] = sagaStateChange;
        }

        static bool IsTimeoutMessage(LogicalMessage message)
        {
            string isTimeoutString;
            if (message.Headers.TryGetValue(Headers.IsSagaTimeoutMessage, out isTimeoutString))
            {
                return isTimeoutString.ToLowerInvariant() == "true";
            }
            return false;
        }

        SagaUpdatedMessage sagaAudit;
    }
}