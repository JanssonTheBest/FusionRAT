using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Common.Plugin
{
    public class DynamicAssemblyLoader : IDisposable
    {
        private CollectibleAssemblyLoadContext _loadContext;
        private Assembly _loadedAssembly;

        public Assembly LoadAssembly(byte[] assemblyBytes)
        {
            _loadContext = new CollectibleAssemblyLoadContext();
            using (MemoryStream ms = new MemoryStream(assemblyBytes))
            {
                _loadedAssembly = _loadContext.LoadFromStream(ms);
            }
            return _loadedAssembly;
        }

        public Assembly LoadAssembly(string assemblyPath)
        {
            _loadContext = new CollectibleAssemblyLoadContext();
            using (FileStream fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
            {
                _loadedAssembly = _loadContext.LoadFromStream(fs);
            }
            return _loadedAssembly;
        }

        public void Dispose()
        {
            if (_loadContext != null)
            {
                _loadContext.Unload();
                _loadContext = null;
            }
            _loadedAssembly = null;
        }

        private class CollectibleAssemblyLoadContext : AssemblyLoadContext
        {
            public CollectibleAssemblyLoadContext() : base(isCollectible: true) { }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return null;
            }
        }
    }
}
