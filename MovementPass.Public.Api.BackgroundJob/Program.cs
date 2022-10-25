[assembly:
    Amazon.Lambda.Core.LambdaSerializer(
        typeof(Amazon.Lambda.Serialization.SystemTextJson.
            DefaultLambdaJsonSerializer))]

namespace MovementPass.Public.Api.BackgroundJob;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Amazon.DynamoDBv2;
using Amazon.Lambda.SQSEvents;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using ExtensionMethods;
using Infrastructure;
using Services;

public class Program
{
    private static readonly ServiceProvider Container = CreateContainer();

    public async Task Main(SQSEvent sqsEvent)
    {
        if (sqsEvent == null)
        {
            throw new ArgumentNullException(nameof(sqsEvent));
        }

        await Container.GetRequiredService<IProcessor>()
            .Process(sqsEvent.Records, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static ServiceProvider CreateContainer()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false)
            .AddSystemsManager(
                Environment.GetEnvironmentVariable("CONFIG_ROOT_KEY"))
            .Build();

        var services = new ServiceCollection();

        services.AddOptions();
        services.AddSingleton<IConfiguration>(_ => config);

        services.AddDefaultAWSOptions(config.GetAWSOptions());
        services.AddAWSService<IAmazonDynamoDB>();

        var production = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

        if (production)
        {
            AWSXRayRecorder.InitializeInstance(config);
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        services.AddLogging(options =>
            options.AddLambdaLogger(config, "logging"));

        config.Apply<DynamoDBTablesOptions>(services);
        config.Apply<JwtOptions>(services);

        services.AddSingleton<IRecordDeserializer, RecordDeserializer>();
        services.AddSingleton<ITokenValidator, TokenValidator>();
        services.AddSingleton<IDataReducer, DataReducer>();
        services.AddSingleton<IDataLoader, DataLoader>();
        services.AddSingleton<IProcessor, Processor>();

        return services.BuildServiceProvider();
    }
}