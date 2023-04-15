using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using log4net;

namespace SocketServerServices
{
    [RunInstaller(true)]
    public partial class MainService : ServiceBase
    {
        Thread _thread = null;
        static readonly ILog _log = LogManager.GetLogger(typeof(MainService));
        SocketServer _socketService;

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _thread = new Thread(() => SocketService());
                _thread.Start();
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
        }

        void SocketService()
        {
            string ipAddress = ConfigurationManager.AppSettings["ipAddress"];
            int port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
            string localAddress = ConfigurationManager.AppSettings["localAddress"];

            while(true)
            {
                try
                {
                    using(_socketService = new SocketServer(ipAddress, port, localAddress))
                    {
                        _socketService.AcceptConnections();
                    }
                }
                catch(ThreadAbortException ex)
                {
                    _log.Error(ex);
                    break;
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
                Thread.Sleep(3 * 1000);
            }
        }

        void CloseSocketServer()
        {
            try
            {
                _thread?.Abort();
                _socketService.Dispose();
                _thread?.Join();
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
        }

        protected override void OnStop()
        {
            CloseSocketServer();
        }
    }
}
