using System;

namespace Toxon.Micro.RabbitBlog.Post
{
    class Program
    {
        static void Main(string[] args)
        {
            var logic = new BusinessLogic();

            // TODO post:entry => PostEntryRequest => logic.HandlePostEntryAsync

            Console.WriteLine("Running Post... press enter to exit!");
            Console.ReadLine();
        }
    }
}
