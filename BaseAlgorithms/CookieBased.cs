using Loadbalancer.Loadbalancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseAlgorithms
{
    class CookieBased : RoundRobinAlgorithm, IAlgorithm
    {
        public override Server GetServer(List<Server> servers)
        {
            return base.GetServer(servers);
        }
    }
}
