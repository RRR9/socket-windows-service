using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace SocketClientServices
{
    class SocketClient
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(SocketClient));
        Socket _client;
        int _port;
        string _address;

        public SocketClient(string address, int port)
        {
            _client = null;
            _port = port;
            _address = address;
        }

        public void ReceiveMessage()
        {
            using(_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                byte[] data = new byte[10 * 1000];
                string msg;
                IPEndPoint _ipPoint = new IPEndPoint(IPAddress.Parse(_address), _port);

                _client.Connect(_ipPoint);

                while (true)
                {
                    _log.Info("Ожидание собщения...");

                    int bytes = _client.Receive(data, SocketFlags.None);
                    msg = Encoding.UTF8.GetString(data, 0, bytes);

                    _log.Info($"Сервер {_client.RemoteEndPoint} отправил сообщение: {msg}\n");

                    new Thread(() => ParseMessage(msg)).Start();
                }
            }
        }

        public void Clear()
        {
            _client.Close();
        }

        void SendMessage(string msg)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                _client.Send(data);
            }
            catch(Exception ex)
            {
                _log.Error("Method _SendMessage_", ex);
            }
        }

        void ParseMessage(string msg)
        {
            dynamic jResponse = JsonConvert.DeserializeObject(msg);
            dynamic t = jResponse.identifier;

            JObject type = new JObject();
            JObject args = new JObject();

            if (t != null)
            {
                int idSocket = t.id_socket;
                string nameTerminal = GetNameTerminal();

                args["id_socket"] = idSocket;
                args["name_terminal"] = nameTerminal; // $"terminal {_client.Handle.ToInt32()}";

                type["identifier"] = args;

                SendMessage(type.ToString());
            }
        }

        string GetNameTerminal()
        {
            const string fileName = "terminalinfo.xml";
            const string processName = "nterm";
            string nameTerminal = "-";
            Process[] processArr = null;

            try
            {
                processArr = Process.GetProcessesByName(processName);
            }
            catch(Exception ex)
            {
                _log.Error("Method _GetNameTerminal_", ex);
                return nameTerminal;
            }

            if (processArr.Length > 1)
            {
                nameTerminal = $"Невозможно определить названия терминала. Запущенно несколько процессов с одинаковым именем {processName}";
            }
            else if (processArr.Length < 1)
            {
                nameTerminal = $"Невозможно определить названия терминала. Процесс {processName} не запущен";
            }
            else
            {
                Process process = null;
                string pathNTerm = null;
                try
                {
                    process = Process.GetProcessesByName(processName)[0];
                    pathNTerm = Path.GetDirectoryName(process.MainModule.FileName);
                }
                catch(Exception ex)
                {
                    _log.Error("Method _GetNameTerminal_", ex);
                    return nameTerminal;
                }
                foreach (var file in Directory.GetFiles(pathNTerm))
                {
                    if (Path.GetFileName(file) == fileName)
                    {
                        _log.Info($"{fileName} found!");
                        using (StreamReader sr = new StreamReader(Path.Combine(pathNTerm, fileName)))
                        {
                            try
                            {
                                string s = sr.ReadToEnd();
                                XmlDocument xmlDocument = new XmlDocument();
                                xmlDocument.LoadXml(s);
                                nameTerminal = xmlDocument.GetElementsByTagName("terminalID")[0].InnerText;
                            }
                            catch(Exception ex)
                            {
                                _log.Error("Method _GetNameTerminal_", ex);
                                nameTerminal = $"Файл {fileName} настроен неверно";
                            }
                        }
                        break;
                    }
                }
            }
            _log.Info($"Terminal name: {nameTerminal}");
            return nameTerminal;
        }
    }
}
