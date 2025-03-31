using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfClientApp
{
    internal class Program
    {
        private static WcfServiceLibrary.IQidoService clientProxy;
        static void Main(string[] args)
        {
            NetTcpBinding binding = new NetTcpBinding();
            EndpointAddress addr = new EndpointAddress("net.tcp://localhost:9081/wcfServiceDemo");
            ChannelFactory<WcfServiceLibrary.IQidoService> chn = new ChannelFactory<WcfServiceLibrary.IQidoService>(binding, addr);
            clientProxy = chn.CreateChannel();

            var result = clientProxy.GetData();
            Console.WriteLine(result);
        }
    }
}
