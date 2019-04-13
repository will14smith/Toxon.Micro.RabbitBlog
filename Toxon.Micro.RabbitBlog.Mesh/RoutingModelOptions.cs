using System.Net;

namespace Toxon.Micro.RabbitBlog.Mesh
{
    public class RoutingModelOptions
    {
        // TODO use local ip instead
        public IPAddress IPAddress { get; set; } = IPAddress.Loopback;
        public (int Min, int Max) PortSelectionRange { get; set; } = (18000, 19000);
    }
}