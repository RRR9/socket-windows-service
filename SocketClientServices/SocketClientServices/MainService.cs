using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using log4net;

namespace SocketClientServices
{
    public partial class MainService : ServiceBase
    {
        Thread _thread = null;
        static ILog _log = LogManager.GetLogger(typeof(MainService));
        SocketClient _socketClient = null;

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _thread = new Thread(() => SocketService());
            _thread.Start();
        }

        void SocketService()
        {
            string ipAddress = ConfigurationManager.AppSettings["ipAddress"];
            int port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);

            _socketClient = new SocketClient(ipAddress, port);

            while (true)
            {
                try
                {
                    _socketClient.ReceiveMessage();
                }
                catch(ThreadAbortException ex)
                {
                    _log.Error("Method _SocketService_", ex);
                    _socketClient.Clear();
                    break;
                }
                catch(Exception ex)
                {
                    _log.Error("Method _SocketService_", ex);
                    _socketClient.Clear();
                }

                Thread.Sleep(5 * 1000);
            }
        }

        void CloseSocketClient()
        {
            _thread?.Abort();
            _socketClient?.Clear();
            _thread?.Join();
        }
        
        protected override void OnStop()
        {
            CloseSocketClient();
        }
    }
}
