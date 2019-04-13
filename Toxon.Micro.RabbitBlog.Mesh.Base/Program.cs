using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Toxon.Swim;
using Toxon.Swim.Models;

namespace Toxon.Micro.RabbitBlog.Mesh.Base
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var swim = new SwimClient(
                new SwimHost(new IPEndPoint(IPAddress.Loopback, 17999)),
                new SwimMeta(new Dictionary<string, string>()),
                new SwimClientOptions()
            );

            await swim.StartAsync();

            Console.WriteLine($"Running Base on {swim.Local}... press enter to exit!");

            while (true)
            {
                var cmd = Console.ReadLine()?.Trim()?.ToLower();

                if (cmd == "m")
                {
                    foreach (var member in swim.Members.GetAll(true, true))
                    {
                        Console.WriteLine($"{member.Host} - {member.State}");
                        foreach (var (key, value) in member.Meta.Fields)
                        {
                            Console.WriteLine($"  {key}: {value}");
                        }
                    }
                }
                else if (cmd == "s")
                {
                    await swim.JoinAsync(swim.Members.GetAll().Select(x => x.Host).ToList());
                }
                else if (string.IsNullOrEmpty(cmd))
                {
                    return;
                }
            }

        }
    }
}
