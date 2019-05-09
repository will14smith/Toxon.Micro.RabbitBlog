using System;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    internal class Timing
    {
        public static string Now => DateTime.UtcNow.ToString("HH:mm:ss fffffff");
    }
}