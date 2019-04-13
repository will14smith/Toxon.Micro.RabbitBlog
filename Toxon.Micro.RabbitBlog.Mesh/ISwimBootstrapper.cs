using System.Collections.Generic;
using Toxon.Swim.Models;

namespace Toxon.Micro.RabbitBlog.Mesh
{
    public interface ISwimBootstrapper
    {
        IReadOnlyCollection<SwimHost> GetWellKnownHosts();
    }
}