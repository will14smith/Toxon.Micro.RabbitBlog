using System;

namespace Toxon.Micro.RabbitBlog.EntryStore
{
    class Program
    {
        static void Main(string[] args)
        {
            var logic = new BusinessLogic();

            // TODO store:*,kind:entry => StoreRequest => logic.HandleStoreAsync
            
            Console.WriteLine("Running EntryStore... press enter to exit!");
            Console.ReadLine();
        }
    }
}
