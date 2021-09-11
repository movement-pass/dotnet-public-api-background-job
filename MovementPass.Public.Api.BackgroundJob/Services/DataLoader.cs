namespace MovementPass.Public.Api.BackgroundJob.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Options;

    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.Model;

    using ExtensionMethods;
    using Infrastructure;

    public interface IDataLoader
    {
        Task Load(
            IEnumerable<Pass> applications,
            CancellationToken cancellationToken);
    }

    public class DataLoader : IDataLoader
    {
        private readonly IAmazonDynamoDB _dynamodb;
        private readonly DynamoDBTablesOptions _tableOptions;

        public DataLoader(
            IAmazonDynamoDB dynamodb,
            IOptions<DynamoDBTablesOptions> tableOptions)
        {
            if (tableOptions == null)
            {
                throw new ArgumentNullException(nameof(tableOptions));
            }

            this._dynamodb = dynamodb;
            this._tableOptions = tableOptions.Value;
        }

        public async Task Load(
            IEnumerable<Pass> applications,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tasks = applications.Chunk(12)
                .Select(group =>
                {
                    var writeItems = new List<TransactWriteItem>();

                    foreach (var application in group)
                    {
                        writeItems.Add(new TransactWriteItem
                        {
                            Put = new Put
                            {
                                TableName = this._tableOptions.Passes,
                                Item = application.ToDynamoDBAttributes()
                            }
                        });

                        writeItems.Add(new TransactWriteItem
                        {
                            Update = new Update
                            {
                                TableName = this._tableOptions.Applicants,
                                Key =
                                    new Dictionary<string, AttributeValue>
                                    {
                                        {
                                            "id",
                                            new AttributeValue
                                            {
                                                S = application.ApplicantId
                                            }
                                        }
                                    },
                                UpdateExpression = "SET #ac = #ac + :inc",
                                ExpressionAttributeNames =
                                    new Dictionary<string, string>
                                    {
                                        { "#ac", "appliedCount" }
                                    },
                                ExpressionAttributeValues =
                                    new Dictionary<string, AttributeValue>
                                    {
                                        {
                                            ":inc",
                                            new AttributeValue { N = "1" }
                                        }
                                    }
                            }
                        });
                    }

                    var req = new TransactWriteItemsRequest
                    {
                        TransactItems = writeItems
                    };

                    return this._dynamodb.TransactWriteItemsAsync(
                        req,
                        cancellationToken);
                });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}