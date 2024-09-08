using Common.Communication;
using Common.DTOs.MessagePack;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Concurrent;
using System.Runtime.Loader;
using System;
using System.IO;
using Common.Plugin;


namespace Client.Communication
{
    internal class ClientSession : Session
    {
        ClientInfoDTO clientInfo;
        ConcurrentDictionary<object?, DynamicAssemblyLoader> activePlugins = new ConcurrentDictionary<object?, DynamicAssemblyLoader>();
        public ClientSession(IConnectionProperties connectionProperties) : base(connectionProperties)
        {
            OnPing += HandlePing;
            OnPlugin += HandlePlugin;
            OnDisposePlugin += DisposePlugin;
        }

        private void HandlePlugin(object? sender, EventArgs e)
        {
            var DTO = (PluginDTO)sender;
            var assemblyLoader = new DynamicAssemblyLoader();
            var assembly = assemblyLoader.LoadAssembly(DTO.Plugin);
            var plugin = Activator.CreateInstance(assembly.GetType(DTO.PluginFullName), this);
            activePlugins.TryAdd(plugin, assemblyLoader);
        }

        public void DisposePlugin(object? sender, EventArgs e)
        {
            bool result = activePlugins.TryRemove(sender, out var assemblyctx);
            assemblyctx.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private async void HandlePing(object? sender, EventArgs e)
        {
            SendPacketAsync(new PingDTO());
        }
    }
}
