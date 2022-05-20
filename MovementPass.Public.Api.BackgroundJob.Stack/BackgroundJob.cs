namespace MovementPass.Public.Api.BackgroundJob.Stack
{
    using System.Collections.Generic;

    using Amazon.CDK;
    using Amazon.CDK.AWS.DynamoDB;
    using Amazon.CDK.AWS.IAM;
    using Amazon.CDK.AWS.Kinesis;
    using Amazon.CDK.AWS.Lambda;
    using Amazon.CDK.AWS.Lambda.EventSources;

    public sealed class BackgroundJob : BaseStack
    {
        public BackgroundJob(
            Construct scope,
            string id,
            IStackProps props = null) : base(scope, id, props)
        {
            var name = $"{this.App}_public-api-background-job_{this.Version}";

            var stream = Stream.FromStreamArn(
                this,
                "Stream",
                $"arn:aws:kinesis:{this.Region}:{this.Account}:stream/passes-load");

            var lambda = new Function(this, "Lambda",
                new FunctionProps {
                    FunctionName = name,
                    Handler =
                        "MovementPass.Public.Api.BackgroundJob::MovementPass.Public.Api.BackgroundJob.Program::Main",
                    Runtime = Runtime.DOTNET_6,
                    Timeout = Duration.Minutes(15),
                    MemorySize = 3008,
                    Code = Code.FromAsset($"dist/{name}.zip"),
                    Tracing = Tracing.ACTIVE,
                    Environment = new Dictionary<string, string> {
                        { "ASPNETCORE_ENVIRONMENT", "Production" },
                        { "CONFIG_ROOT_KEY", this.ConfigRootKey }
                    }
                });

            lambda.AddToRolePolicy(new PolicyStatement(
                new PolicyStatementProps {
                    Effect = Effect.ALLOW,
                    Actions = new[] { "ssm:GetParametersByPath" },
                    Resources = new[] {
                        $"arn:aws:ssm:{this.Region}:{this.Account}:parameter{this.ConfigRootKey}"
                    }
                }));

            foreach (var partialName in new[] { "applicants", "passes" })
            {
                var tableName =
                    this.GetParameterStoreValue(
                        $"dynamodbTables/{partialName}");

                var table = Table.FromTableArn(
                    this,
                    $"{partialName}Table",
                    $"arn:aws:dynamodb:{this.Region}:{this.Account}:table/{tableName}");

                table.GrantReadWriteData(lambda);
            }

            lambda.AddEventSource(new KinesisEventSource(stream,
                new KinesisEventSourceProps {
                    BatchSize = 1000,
                    MaxBatchingWindow = Duration.Minutes(1),
                    StartingPosition = StartingPosition.LATEST
                }));

            stream.GrantRead(lambda);
        }
    }
}