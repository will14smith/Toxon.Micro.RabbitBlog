using System;
using Newtonsoft.Json;

namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class PatternJsonConverter : JsonConverter<IRequestMatcher>
    {
        public override void WriteJson(JsonWriter writer, IRequestMatcher value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override IRequestMatcher ReadJson(JsonReader reader, Type objectType, IRequestMatcher existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class ValuePatternJsonConverter : JsonConverter<IValueMatcher>
    {
        public override void WriteJson(JsonWriter writer, IValueMatcher value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override IValueMatcher ReadJson(JsonReader reader, Type objectType, IValueMatcher existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
