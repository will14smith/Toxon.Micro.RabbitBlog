using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class PatternJsonConverter : JsonConverter<IRequestMatcher>
    {
        public override void WriteJson(JsonWriter writer, IRequestMatcher value, JsonSerializer serializer)
        {
            switch (value)
            {
                case AndMatcher andMatcher:
                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue("and");

                    writer.WritePropertyName("matchers");
                    serializer.Serialize(writer, andMatcher.RequestMatchers);

                    writer.WriteEndObject();
                    break;

                case FieldMatcher fieldMatcher:
                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue("field");

                    writer.WritePropertyName("name");
                    serializer.Serialize(writer, fieldMatcher.FieldName);

                    writer.WritePropertyName("value");
                    serializer.Serialize(writer, fieldMatcher.FieldValue);

                    writer.WriteEndObject();
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public override IRequestMatcher ReadJson(JsonReader reader, Type objectType, IRequestMatcher existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            if (!(obj["type"] is JValue typeProp))
            {
                throw new Exception("Invalid json, missing type");
            }
            if (typeProp.Type != JTokenType.String)
            {
                throw new Exception("Invalid json, type wasn't a string");
            }

            switch (typeProp.Value as string)
            {
                case "and":
                    if (!(obj["matchers"] is JArray matchersProp))
                    {
                        throw new Exception("Invalid 'and' json, missing matchers");
                    }

                    var matchers = matchersProp.ToObject<IRequestMatcher[]>(serializer);

                    return new AndMatcher(matchers);

                case "field":
                    if (!(obj["name"] is JValue nameProp))
                    {
                        throw new Exception("Invalid 'field' json, missing name");
                    }
                    if (nameProp.Type != JTokenType.String)
                    {
                        throw new Exception("Invalid 'field' json, name wasn't a string");
                    }

                    if (!(obj["value"] is JToken valueProp))
                    {
                        throw new Exception("Invalid 'field' json, missing value");
                    }

                    var value = valueProp.ToObject<IValueMatcher>(serializer);

                    return new FieldMatcher(nameProp.Value as string, value);
                    
                default: throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}
