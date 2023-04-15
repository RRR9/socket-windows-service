using System;
using System.Configuration;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using log4net.Config;
using log4net;

namespace SocketServerServices
{
    static class Program
    {
        static ILog _log;

        static void Main()
        {
            XmlConfigurator.Configure(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config")));
            _log = LogManager.GetLogger(typeof(Program));
            InstallWindowsService();
        }

        static bool CheckService(string serviceName)
        {
            foreach (var service in ServiceController.GetServices())
            {
                if (service.ServiceName.Equals(serviceName))
                {
                    return true;
                }
            }
            return false;
        }

        static void InstallWindowsService()
        {
            string appName = ConfigurationManager.AppSettings["appName"];
            _log.Info($"Start checking already installed service: {appName}");
            bool found = CheckService(appName);
            _log.Info($"Service found = {found}");
            if (!found)
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                //string installUtilPath = ConfigurationManager.AppSettings["installUtilPath"];
                string filePath = null;
                foreach (var file in Directory.GetFiles(currentPath))
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.ToLower().Equals(appName.ToLower() + ".exe"))
                    {
                        filePath = file;
                    }
                }
                if(filePath != null)
                {
                    try
                    {
                        _log.Info("Start install service");
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        _log.Info("Service installed");
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Method _InstallWindowsService_", ex);
                    }
                }
            }
            else
            {
                _log.Info("Ready to go");
                ServiceBase[] ServicesToRun = new ServiceBase[] {
                    new MainService()
                };
                try
                {
                    _log.Info("Trying to run service");
                    ServiceBase.Run(ServicesToRun);
                }
                catch(Exception ex)
                {
                    _log.Error("Method _InstallWindowsService_", ex);
                }
            }
        }
    }
}
