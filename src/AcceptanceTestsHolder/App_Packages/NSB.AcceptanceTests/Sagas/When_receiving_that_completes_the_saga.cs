﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_receiving_that_completes_the_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_hydrate_and_complete_the_existing_instance()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<ReceiveCompletesSagaEndpoint>(b =>
                {
                    b.When((session, ctx) => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = ctx.Id
                    }));
                    b.When(ctx => ctx.StartSagaMessageReceived, (session, c) =>
                    {
                        c.AddTrace("CompleteSagaMessage sent");

                        return session.SendLocal(new CompleteSagaMessage
                        {
                            SomeId = c.Id
                        });
                    });
                })
                .Done(c => c.SagaCompleted)
                .Run();

            Assert.True(context.SagaCompleted);
        }

        [Test]
        public async Task Should_ignore_messages_afterwards()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<ReceiveCompletesSagaEndpoint>(b =>
                {
                    b.When((session, c) => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = c.Id
                    }));
                    b.When(c => c.StartSagaMessageReceived, (session, c) =>
                    {
                        c.AddTrace("CompleteSagaMessage sent");
                        return session.SendLocal(new CompleteSagaMessage
                        {
                            SomeId = c.Id
                        });
                    });
                    b.When(c => c.SagaCompleted, (session, c) => session.SendLocal(new AnotherMessage
                    {
                        SomeId = c.Id
                    }));
                })
                .Done(c => c.AnotherMessageReceived)
                .Run();

            Assert.True(context.AnotherMessageReceived, "AnotherMessage should have been delivered to the handler outside the saga");
            Assert.False(context.SagaReceivedAnotherMessage, "AnotherMessage should not be delivered to the saga after completion");
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public bool SagaCompleted { get; set; }
            public bool AnotherMessageReceived { get; set; }
            public bool SagaReceivedAnotherMessage { get; set; }
        }

        public class ReceiveCompletesSagaEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveCompletesSagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.ExecuteTheseHandlersFirst(typeof(TestSaga10));
                    b.LimitMessageProcessingConcurrencyTo(1); // This test only works if the endpoints processes messages sequentially
                });
            }

            public class TestSaga10 : SqlSaga<TestSagaData10>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<CompleteSagaMessage>,
                IHandleMessages<AnotherMessage>
            {
                protected override string CorrelationPropertyName => nameof(TestSagaData10.SomeId);

                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.AddTrace("Saga started");

                    Data.SomeId = message.SomeId;

                    Context.StartSagaMessageReceived = true;

                    return Task.FromResult(0);
                }

                public Task Handle(AnotherMessage message, IMessageHandlerContext context)
                {
                    Context.AddTrace("AnotherMessage received");
                    Context.SagaReceivedAnotherMessage = true;
                    return Task.FromResult(0);
                }

                public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
                {
                    Context.AddTrace("CompleteSagaMessage received");
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId);
                    mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId);
                    mapper.ConfigureMapping<AnotherMessage>(m => m.SomeId);
                }
            }

            public class TestSagaData10 : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class CompletionHandler : IHandleMessages<AnotherMessage>
        {
            public Context Context { get; set; }

            public Task Handle(AnotherMessage message, IMessageHandlerContext context)
            {
                Context.AnotherMessageReceived = true;
                return Task.FromResult(0);
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class CompleteSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class AnotherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}