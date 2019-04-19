using System.Collections.Generic;
using System.Linq;
using System.Net;
using Toxon.Swim.Models;

namespace Toxon.Micro.RabbitBlog.Mesh.Host
{
    internal class WellKnownBases : ISwimBootstrapper
    {
        private readonly IReadOnlyCollection<SwimHost> _baseAddresses;

        public WellKnownBases(IEnumerable<string> baseAddresses)
        {
            _baseAddresses = baseAddresses
                .Select(x =>
                {
                    var parts = x.Split(":");
                    var ip = IPAddress.Parse((string) parts[0]);
                    var port = int.Parse((string) parts[1]);
                    return new IPEndPoint(ip, port);
                })
                .Select(x => new SwimHost(x))
                .ToList();
        }

        public IReadOnlyCollection<SwimHost> GetWellKnownHosts()
        {
            return _baseAddresses;
        }
    }
}