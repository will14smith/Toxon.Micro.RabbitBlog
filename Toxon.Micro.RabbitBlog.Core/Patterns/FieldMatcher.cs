namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class FieldMatcher : IRequestMatcher
    {
        public string FieldName { get; }
        public IValueMatcher FieldValue { get; }

        public FieldMatcher(string fieldName, IValueMatcher fieldValue)
        {
            FieldName = fieldName;
            FieldValue = fieldValue;
        }

        public override string ToString()
        {
            return $"{FieldName} = {FieldValue}";
        }
    }
}