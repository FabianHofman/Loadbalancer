using Loadbalancer.Loadbalancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdditionalAlgorithms
{
    public class RandomAlgorithm : IAlgorithm
    {
        public Server GetServer(List<Server> servers)
        {
            Random random = new Random();
            return servers[random.Next(servers.Count())];
        }
    }
}
