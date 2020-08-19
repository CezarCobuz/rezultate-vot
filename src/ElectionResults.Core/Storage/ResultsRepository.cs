using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Models;
using ElectionResults.Core.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ElectionResults.Core.Storage
{
    public class ResultsRepository : IResultsRepository
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly AppConfig _config;

        public ResultsRepository(IOptions<AppConfig> config, IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
            _config = config.Value;
        }

        public async Task InsertResults(ElectionStatistics electionStatistics)
        {
            try
            {
                Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
                item["id"] = new AttributeValue { S = electionStatistics.Id };
                item["fileType"] = new AttributeValue { S = electionStatistics.Type };
                item["fileSource"] = new AttributeValue { S = electionStatistics.Source };
                item["electionId"] = new AttributeValue { S = electionStatistics.ElectionId };
                item["statisticsJson"] = new AttributeValue { S = electionStatistics.StatisticsJson };
                item["csvTimestamp"] = new AttributeValue { N = electionStatistics.Timestamp.ToString() };
                List<WriteRequest> items = new List<WriteRequest>
                {
                    new WriteRequest
                    {
                        PutRequest = new PutRequest
                        {
                            Item = item
                        }
                    }
                };
                Dictionary<string, List<WriteRequest>> requestItems = new Dictionary<string, List<WriteRequest>>();
                requestItems[_config.TableName] = items;

                await _dynamoDb.BatchWriteItemAsync(requestItems);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Couldn't insert results");
            }
        }

        public virtual async Task InitializeDb()
        {
            var tableExists = await TableExists();
            if (!tableExists)
            {
                Log.LogInformation($"Creating table {_config.TableName}");
                await CreateTable();
            }
        }

        public virtual async Task InsertCurrentVoterTurnout(VoterTurnout voterTurnout)
        {
            var electionStatistics = new ElectionStatistics
            {
                Id = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}",
                Type = FileType.VoterTurnout.ConvertEnumToString(),
                Source = Consts.VoteTurnoutKey,
                Timestamp = voterTurnout.Timestamp,
                ElectionId = voterTurnout.ElectionId,
                StatisticsJson = JsonConvert.SerializeObject(voterTurnout)
            };
            await InsertResults(electionStatistics);
        }

        public async Task InsertVoteMonitoringStats(VoteMonitoringStats voteMonitoringInfo)
        {
            var electionStatistics = new ElectionStatistics
            {
                Id = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}",
                Type = FileType.VoteMonitoring.ConvertEnumToString(),
                Source = Consts.VoteMonitoringKey,
                Timestamp = voteMonitoringInfo.Timestamp,
                ElectionId = voteMonitoringInfo.ElectionId,
                StatisticsJson = JsonConvert.SerializeObject(voteMonitoringInfo)
            };
            await InsertResults(electionStatistics);
        }

        public async Task<Result<ElectionStatistics>> Get(string electionId, string source, string type)
        {
            var queryRequest = new QueryRequest(_config.TableName)
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":fileSource", new AttributeValue(source)},
                    {":electionId", new AttributeValue(electionId)}
                },
                IndexName = "latest-result",
                KeyConditionExpression = "fileSource = :fileSource and electionId = :electionId"
            };
            var queryResponse = await _dynamoDb.QueryAsync(queryRequest);

            var results = GetResults(queryResponse.Items);
            var latest = results.Where(r => r.Type == type).OrderByDescending(r => r.Timestamp).FirstOrDefault();
            if (latest != null)
                return Result.Success(latest);
            return Result.Success(new ElectionStatistics
            {
                StatisticsJson = ""
            });
        }

        private async Task WaitUntilTableReady(string tableName)
        {
            string status = null;

            do
            {
                await Task.Delay(2000);
                try
                {
                    var res = _dynamoDb.DescribeTableAsync(new DescribeTableRequest
                    {
                        TableName = tableName
                    });

                    status = res.Result.Table.TableStatus;
                }
                catch (ResourceNotFoundException)
                {

                }

            } while (status != "ACTIVE");
        }

        private async Task<bool> TableExists()
        {
            try
            {
                var res = await _dynamoDb.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = _config.TableName
                });
                return res.Table.TableStatus == "ACTIVE";
            }
            catch (Exception e)
            {
                Log.LogError(e, $"Table {_config.TableName} isn't ready'");
                return false;
            }
        }

        private List<ElectionStatistics> GetResults(List<Dictionary<string, AttributeValue>> foundItems)
        {
            return foundItems.Select(item => new ElectionStatistics
            {
                Id = item["id"].S,
                Timestamp = Convert.ToInt64(item["csvTimestamp"].N),
                StatisticsJson = item["statisticsJson"].S,
                Source = item["fileSource"].S,
                Type = item["fileType"].S

            }).ToList();
        }

        private async Task CreateTable()
        {
            Log.LogInformation("Creating Table");

            try
            {
                var request = new CreateTableRequest
                {
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition
                        {
                            AttributeName = "id",
                            AttributeType = ScalarAttributeType.S
                        },
                        new AttributeDefinition
                        {
                            AttributeName = "fileSource",
                            AttributeType = ScalarAttributeType.S
                        },
                        new AttributeDefinition
                        {
                            AttributeName = "electionId",
                            AttributeType = ScalarAttributeType.S
                        }
                    },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "id",
                            KeyType = KeyType.HASH
                        },
                        new KeySchemaElement
                        {
                            AttributeName = "electionId",
                            KeyType = KeyType.RANGE
                        }
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 5
                    },
                    TableName = _config.TableName,
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                    {
                        new GlobalSecondaryIndex
                        {
                            IndexName = "latest-result",
                            KeySchema = new List<KeySchemaElement>
                            {
                                new KeySchemaElement("fileSource", KeyType.HASH),
                                new KeySchemaElement("electionId", KeyType.RANGE)
                            },
                            Projection = new Projection{ProjectionType = ProjectionType.ALL},
                            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                        }
                    }
                };

                var response = await _dynamoDb.CreateTableAsync(request);
                await WaitUntilTableReady(_config.TableName);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Create table error");
            }
        }
    }
}