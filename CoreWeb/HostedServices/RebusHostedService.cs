using System.Reflection;
using CoreBackend.Infrastructure.Jsons;
using CoreBackend.Infrastructure.Rebus;
using CoreBackend.Infrastructure.Rebus.MessageRouting;
using CoreDdd.Nhibernate.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Injection;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Rebus.Transport.InMem;

namespace CoreWeb.HostedServices;

public class RebusHostedService(
    IRebusHostedServiceContainerSetup containerSetup,
    string inputQueueName,
    string environmentName,
    string connectionString,
    RebusOptions rebusOptions,
    IEnumerable<Assembly> assembliesWithTypesDerivedFromBaseMessage,
    Func<IResolutionContext, IErrorHandler> errorHandlerFactory
) : IHostedService
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task StartAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var ioCContainer = containerSetup.GetContainer();

        var rebusConfigurer = containerSetup.CreateRebusConfigurerFactory()()
            .Logging(x => x.MicrosoftExtensionsLogging(ioCContainer.Resolve<ILoggerFactory>()));

        var environmentInputQueueName = $"{environmentName}{inputQueueName}";

        switch (rebusOptions.Transport)
        {
            case "RabbitMQ":
                var configuration = ioCContainer.Resolve<IConfiguration>();
                var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ");
                rebusConfigurer.Transport(x => x.UseRabbitMq(rabbitMqConnectionString, environmentInputQueueName));
                break;

            case "PostgreSql":
                // todo: command handler which sends a domain event over the message bus sends the domain event even when the command handler throws: https://github.com/rebus-org/Rebus.PostgreSql/issues/53
                rebusConfigurer
                    .Transport(x => x.UsePostgreSql(
                        connectionString,
                        "RebusQueue",
                        environmentInputQueueName
                    ));
                break;

            case "InMemory":
                var rebusInMemoryNetwork = ioCContainer.Resolve<InMemNetwork>();
                rebusConfigurer.Transport(x => x.UseInMemoryTransport(rebusInMemoryNetwork, environmentInputQueueName, registerSubscriptionStorage: false));
                break;

            default:
                throw new InvalidOperationException("Rebus transport is not configured. Set Rebus:Transport to RabbitMq, PostgreSql, or InMemory in appsettings.json.");
        }

        var numberOfWorkers = rebusOptions.GetNumberOfWorkers(inputQueueName);

        var bus = rebusConfigurer
            .Serialization(x => x.UseSystemTextJson(JsonHelper.DefaultOptions))
            .Routing(configurer => MessageRoutingConfigurator.ConfigureMessageRouting(configurer, environmentName, assembliesWithTypesDerivedFromBaseMessage))
            .Options(x =>
                {
                    containerSetup.ConfigureUnitOfWork(x);
                    x.RetryStrategy(
                        errorQueueName: $"{environmentInputQueueName}Error",
                        maxDeliveryAttempts: rebusOptions.MaxDeliveryAttempts,
                        errorTrackingMaxAgeMinutes: 1440 // 1440 minutes = 24h; this prevents never-ending number of retries for messages whose handling take a long time
                    );
                    x.SetNumberOfWorkers(numberOfWorkers);
                    x.SetMaxParallelism(numberOfWorkers);
                    x.Decorate<IPipeline>(resolutionContext =>
                    {
                        var messageLoggingIncomingStep = new MessageLoggingIncomingStep(ioCContainer.Resolve<ILogger<MessageLoggingIncomingStep>>());

                        var jobTrackingIncomingStep = new JobTrackingIncomingStep(
                            ioCContainer.Resolve<INhibernateConfigurator>(),
                            ioCContainer.Resolve<ILogger<JobTrackingIncomingStep>>(),
                            ioCContainer.Resolve<IJobCreator>(nameof(NewTransactionJobCreator)),
                            ioCContainer.Resolve<IRebusHandlingQueueGetter>()
                        );

                        var jobCompletionIncomingStep = new JobCompletionIncomingStep(
                            ioCContainer.Resolve<IJobRepositoryFactory>()
                        );

                        var pipeline = resolutionContext.Get<IPipeline>();
                        return new PipelineStepInjector(pipeline)
                                .OnReceive(messageLoggingIncomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep))
                                .OnReceive(jobTrackingIncomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep))
                                .OnReceive(jobCompletionIncomingStep, PipelineRelativePosition.Before, typeof(ActivateHandlersStep))
                            ;
                    });

                    x.Decorate(errorHandlerFactory);

                    //x.LogPipeline(verbose: true); // uncomment to log pipeline steps so one can decide where to insert an incoming step
                }
            )
            .Start();

        containerSetup.OnBusStarted(bus);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await containerSetup.OnServiceStopped();
    }
}
