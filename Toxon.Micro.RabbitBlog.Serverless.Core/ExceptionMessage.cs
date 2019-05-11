using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Toxon.Micro.RabbitBlog.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.Core
{
    public class ExceptionMessage
    {
        private const string ExceptionHeader = "__exception__";

        public static bool TryGetException(Message message, out Exception exception)
        {
            if (!message.Headers.ContainsKey(ExceptionHeader))
            {
                exception = default;
                return false;
            }

            var body = Encoding.UTF8.GetString(message.Body);

            exception = new RemoteException(body);
            return true;
        }

        public static Message Build(Exception exception)
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(exception));
            var headers = new Dictionary<string, byte[]>
            {
                { ExceptionHeader, new byte[0] },
            };

            return new Message(body, headers);
        }
    }

    internal class RemoteException : Exception
    {
        public string Exception { get; }

        public RemoteException(string exception)
        {
            Exception = exception;
        }
    }
}