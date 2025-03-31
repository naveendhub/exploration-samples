using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using WcfServiceLibrary;

namespace QidoWindowsServiceHost
{
    public partial class QidoServiceHost : ServiceBase
    {
        internal static ServiceHost serviceHost = null;
        public QidoServiceHost()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (serviceHost != null)
                {
                    serviceHost.Close();
                }

                serviceHost = new ServiceHost(typeof(QidoService));
                serviceHost.Open();
            }
            catch (Exception e)
            {
                Debugger.Launch();
                Console.WriteLine(e);
                throw;
            }
            
        }

        protected override void OnStop()
        {
            try
            {
                if (serviceHost != null)
                {
                    serviceHost.Close();
                    serviceHost = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
    }
}
