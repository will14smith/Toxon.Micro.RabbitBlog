using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Toxon.Micro.RabbitBlog.EntryStore.Inbound;
using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.EntryStore
{
    [ServicePlugin("entry-store.v2")]
    public class BusinessLogic
    {
        private static class Keys
        {
            public const string Id = "id";
            public const string User = "user";
            public const string Text = "text";
        }

        private readonly IAmazonDynamoDB _dynamo = new AmazonDynamoDBClient(RegionEndpoint.EUWest1);
        private readonly string _tableName = "rabbitblog-entries";

        [MessageRoute("store:*,kind:entry,cache:true")]
        [MessageRoute("store:*,kind:entry")]
        public Task<object> HandleStoreAsync(StoreRequest message)
        {
            switch (message.Store)
            {
                case "list":
                    return List(message.User);

                case "save":
                    return Save(message.User, message.Text);

                default: throw new NotImplementedException();
            }
        }

        private async Task<object> List(string userFilter = null)
        {
            var filter = new Dictionary<string, Condition>();
            if (!string.IsNullOrEmpty(userFilter))
            {
                filter.Add(Keys.User, new Condition { AttributeValueList = new List<AttributeValue> { new AttributeValue { S = userFilter } }, ComparisonOperator = ComparisonOperator.EQ });
            }

            var result = new List<EntryResponse>();
            var lastKey = new Dictionary<string, AttributeValue>();
            while (true)
            {
                var response = await _dynamo.ScanAsync(new ScanRequest
                {
                    TableName = _tableName,
                    ScanFilter = filter,
                    ExclusiveStartKey = lastKey
                });
                result.AddRange(response.Items.Select(Map));

                if (response.LastEvaluatedKey.Count == 0)
                {
                    break;
                }
                lastKey = response.LastEvaluatedKey;
            }

            return result;
        }

        private async Task<object> Save(string user, string text)
        {
            var entry = new EntryResponse
            {
                Id = Guid.NewGuid().ToString("N"),
                User = user,
                Text = text
            };

            await _dynamo.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = Map(entry)
            });

            return entry;
        }

        private static Dictionary<string, AttributeValue> Map(EntryResponse entry)
        {
            return new Dictionary<string, AttributeValue>
            {
                {Keys.Id, new AttributeValue {S = entry.Id}},
                {Keys.User, new AttributeValue {S = entry.User}},
                {Keys.Text, new AttributeValue {S = entry.Text}},
            };
        }

        private EntryResponse Map(Dictionary<string, AttributeValue> attributes)
        {
            return new EntryResponse
            {
                Id = GetOrDefault(attributes, Keys.Id).S,
                User = GetOrDefault(attributes, Keys.User).S,
                Text = GetOrDefault(attributes, Keys.Text).S,
            };
        }

        private AttributeValue GetOrDefault(Dictionary<string, AttributeValue> attributes, string text)
        {
            return attributes.TryGetValue(text, out var attribute) ? attribute : new AttributeValue { NULL = true };
        }
    }
}
