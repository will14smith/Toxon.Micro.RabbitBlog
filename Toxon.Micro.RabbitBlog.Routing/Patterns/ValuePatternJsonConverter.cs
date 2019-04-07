using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Toxon.Micro.RabbitBlog.Routing.Patterns
{
    public class ValuePatternJsonConverter : JsonConverter<IValueMatcher>
    {
        public override void WriteJson(JsonWriter writer, IValueMatcher value, JsonSerializer serializer)
        {
            switch (value)
            {
                case EqualityValueMatcher equalityValueMatcher:
                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue("equal");

                    writer.WritePropertyName("value");
                    if (!(equalityValueMatcher.MatchValue is string))
                    {
                        throw new NotImplementedException();
                    }
                    serializer.Serialize(writer, equalityValueMatcher.MatchValue);

                    writer.WriteEndObject();
                    break;

                case AnyValueMatcher _:
                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue("any");

                    writer.WriteEndObject();
                    break;

                case RegexValueMatcher regexValueMatcher:
                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue("regex");

                    writer.WritePropertyName("regex");
                    serializer.Serialize(writer, regexValueMatcher.Regex.ToString());

                    writer.WriteEndObject();
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public override IValueMatcher ReadJson(JsonReader reader, Type objectType, IValueMatcher existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                case "equal":
                    if (!(obj["value"] is JValue valueProp))
                    {
                        throw new Exception("Invalid 'equal' json, missing value");
                    }
                    if (valueProp.Type != JTokenType.String)
                    {
                        throw new Exception("Invalid 'equal' json, value wasn't a string");
                    }

                    return new EqualityValueMatcher(valueProp.Value);

                case "any":
                    return new AnyValueMatcher();

                case "regex":
                    if (!(obj["regex"] is JValue regexProp))
                    {
                        throw new Exception("Invalid 'regex' json, missing value");
                    }
                    if (regexProp.Type != JTokenType.String)
                    {
                        throw new Exception("Invalid 'regex' json, regex wasn't a string");
                    }

                    return new RegexValueMatcher(regexProp.Value as string);

                default: throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}