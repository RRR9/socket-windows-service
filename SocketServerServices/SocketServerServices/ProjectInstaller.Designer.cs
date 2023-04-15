
namespace SocketServerServices
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            // 
            // serviceInstaller
            // 
            this.serviceInstaller.Description = "Служба позволяющая поддерживать соединение терминалов с сервером, а также отправл" +
    "ять и получать сообщения";

            this.serviceInstaller.DisplayName = "SocketServerServices";
            this.serviceInstaller.ServiceName = "SocketServerServices";

            _displayName = this.serviceInstaller.DisplayName;
            _serviceName = this.serviceInstaller.ServiceName;
            //this.serviceInstaller.DisplayName = ConfigurationManager.AppSettings["appName"];
            //this.serviceInstaller.ServiceName = ConfigurationManager.AppSettings["appName"];

            this.serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.serviceInstaller});

        }

        #endregion

        private static string _displayName;
        private static string _serviceName;

        public static string DisplayNameStr 
        { 
            get
            {
                return _displayName;
            }
        }

        public static string ServiceNameStr
        { 
            get
            {
                return _serviceName;
            }
        }

        private System.ServiceProcess.ServiceInstaller serviceInstaller;
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
    }
}