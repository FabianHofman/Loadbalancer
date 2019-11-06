using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Loadbalancer.Loadbalancer
{
    public class IAlgorithmFactory
    {
        private static List<Type> types = new List<Type>();

        public IAlgorithm GetAlgorithm(string algo)
        {
            if (algo == null || algo == "")
            {
                throw new ArgumentException("No such Algorithm");
            }
            object oInstance = Activator.CreateInstance(GetAlgorithmByName(algo));
            return (oInstance as IAlgorithm);
        }

        private Type GetAlgorithmByName(string name)
        {
            object oInstance = null;
            foreach (var type in types)
            {
                if (type.Name == name)
                {
                    oInstance = type;
                }
            }
            return (oInstance as Type);
        }

        public static List<string> GetAllAlgorithms()
        {
            types = new List<Type>();

            Type tAlgo = typeof(IAlgorithm);
            List<string> lstClasses = new List<string>();

            string path = @"D:\School\Lesjaar 3\NotS\WIN\Beroepsproducten\Loadbalancer\Loadbalancer-Fabian-Hofman\Loadbalancer\Algos";
            string[] dlls = Directory.GetFiles(path, "*.dll");
            List<Assembly> assemblies = new List<Assembly>();

            foreach(string dll in dlls)
            {
                assemblies.Add(Assembly.LoadFile(Path.GetFullPath(dll)));
            }

            foreach (Assembly assem in assemblies)
            {
                foreach (Type type in assem.GetTypes())
                {
                    if (tAlgo.IsAssignableFrom(type) && (type != tAlgo))
                    {
                        types.Add(type);
                        lstClasses.Add(type.Name);
                    }
                }
            }
            return lstClasses;
        }
    }
}
