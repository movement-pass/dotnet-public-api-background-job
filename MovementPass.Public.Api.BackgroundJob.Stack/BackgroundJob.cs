﻿namespace MovementPass.Public.Api.BackgroundJob.Stack;

using System.Collections.Generic;

using Constructs;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SQS;

public sealed class BackgroundJob : BaseStack
{
    public BackgroundJob(
        Construct scope,
        string id,
        IStackProps props = null) : base(scope, id, props)
    {
        var name = $"{this.App}_public-api-background-job_{this.Version}";

        var queue = Queue.FromQueueArn(
            this,
            "Queue",
            $"arn:aws:sqs:{this.Region}:{this.Account}:{this.App}_passes_load_{this.Version}.fifo");

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

        lambda.AddEventSource(new SqsEventSource(queue));

        queue.GrantConsumeMessages(lambda);

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
    }
}