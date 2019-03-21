using System;
using Toxon.Micro.RabbitBlog.Greetings.Messages;

namespace Toxon.Micro.RabbitBlog.Greetings
{
    public class GreetingHandler
    {
        public GreetingResponse Handle(GreetingRequest request)
        {
            return new GreetingResponse
            {
                Greeting = $"Hello {request.Name}!"
            };
        }

        public void Handle(GreetingEvent request)
        {
            Console.WriteLine($"Say hello to {request.Name}!");
        }
    }
}
