using Loadbalancer.Loadbalancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseAlgorithms
{
    public class RoundRobinAlgorithm : IAlgorithm
    {
        private int currentIndex = 0;

        public virtual Server GetServer(List<Server> servers)
        {
            if (currentIndex == servers.Count() - 1) currentIndex = 0;
            else currentIndex++;
            return servers[currentIndex];
        }
    }
}
