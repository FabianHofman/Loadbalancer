using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loadbalancer.Loadbalancer
{
    public interface IAlgorithm
    {
        Server GetServer(List<Server> servers);
    }
}
