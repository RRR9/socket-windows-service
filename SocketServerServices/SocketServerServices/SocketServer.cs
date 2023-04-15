using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using log4net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SocketServerServices
{
    sealed class SocketServer : IDisposable
    {
        Dictionary<int, string> _idName;
        Dictionary<string, Socket> _nameDevice;
        Dictionary<int, Socket> _handlers;

        List<Socket> _sockets;
        bool _disposed;

        HttpListener _httpListener;
        Socket _listener;

        static readonly ILog _log = LogManager.GetLogger(typeof(SocketServer));
        int _port;
        string _address;
        string _localAddress;

        int _countingTerminal;

        public SocketServer(string address, int port, string localAddress)
        {
            _disposed = false;

            _countingTerminal = 0;
            _localAddress = localAddress;
            _port = port;
            _address = address;

            _handlers = new Dictionary<int, Socket>();
            _nameDevice = new Dictionary<string, Socket>();
            _idName = new Dictionary<int, string>();

            _sockets = new List<Socket>();
            _httpListener = new HttpListener();
            _listener = null;
        }

        void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            foreach(var handler in _handlers)
            {
                _sockets.Add(handler.Value);
            }

            if (disposing)
            {
                foreach (var socket in _sockets)
                {
                    socket.Dispose();
                }

                _listener?.Dispose();
            }

            _httpListener?.Close();

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SocketServer()
        {
            Dispose(false);
        }

        void IdentifySocket(Socket s)
        {
            JObject args = new JObject();
            JObject type = new JObject();

            args["id_socket"] = s.Handle.ToInt32();
            type["identifier"] = args;

            string request = type.ToString();

            SendMessage(s, request);
        }

        public void AcceptConnections()
        {
            using (_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                _httpListener.Prefixes.Add(_localAddress);
                _httpListener.Start();

                new Thread(() => ReceiveHttpMessage()).Start();

                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(_address), _port);
                _listener.Bind(ipEndPoint);
                _listener.Listen(100);
                _log.Info($"Start listen TCP:  {_address}:{_port}");

                while (true)
                {
                    Socket handler = _listener.Accept();

                    _handlers.Add(handler.Handle.ToInt32(), handler);

                    new Thread(() => ReceiveMessage(handler)).Start();
                    new Thread(() => IdentifySocket(handler)).Start();
                }
            }
        }

        void ReceiveHttpMessage()
        {
            _log.Info($"Start listen http:  {_localAddress}...");
            while (true)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = _httpListener.GetContext();
                }
                catch(Exception ex)
                {
                    _log.Error("Method _SendMessage_", ex);
                    Dispose();
                    break;
                }
                
                string body;
                using (StreamReader sr = new StreamReader(ctx?.Request.InputStream))
                {
                    body = sr.ReadToEnd();
                }
                _log.Info($"Request:\n {body}");

                ParseMessageFromHttp(ctx, body);
            }
        }

        void DeleteSocket(int idSocket)
        {
            try
            {
                _handlers.TryGetValue(idSocket, out Socket s);
                s.Dispose();

                _handlers.Remove(idSocket);

                if (_idName.TryGetValue(idSocket, out string nameTerminal))
                {
                    _idName.Remove(idSocket);
                    _nameDevice.Remove(nameTerminal);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Method _ReceiveMessage_", ex);
            }
        }

        void ReceiveMessage(Socket handler)
        {
            _log.Info($"Ready to receive message from {handler.RemoteEndPoint}");
            byte[] buffer = new byte[10 * 1000];
            int received = 0;
            while (true)
            {
                try
                {
                    received = handler.Receive(buffer, SocketFlags.None);
                    
                    string response = Encoding.UTF8.GetString(buffer, 0, received);
                    _log.Info($"Клиент {handler.RemoteEndPoint} отправил сообщение: {response}");
                    ParseMessageFromTerminal(response, handler);
                }
                catch (Exception ex)
                {
                    DeleteSocket(handler.Handle.ToInt32());

                    _log.Error("Method _ReceiveMessage_", ex);
                    break;
                }
            }
        }

        void SendMessage(Socket s, string msg)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                s.Send(data);
            }
            catch(Exception ex)
            {
                _log.Error("Method _SendMessage_", ex);
            }
        }

        void FillResponseMessage(string rspMsg, HttpStatusCode statusCode, string cntType, HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = (int)statusCode;
            ctx.Response.ContentType = cntType;

            byte[] d = Encoding.UTF8.GetBytes(rspMsg);

            ctx.Response.ContentLength64 = d.Length;
            ctx.Response.OutputStream.Write(d, 0, d.Length);
        }

        void ParseMessageFromHttp(HttpListenerContext ctx, string msg)
        {
            dynamic jResponse = JsonConvert.DeserializeObject(msg);
            dynamic t = jResponse.show;
            JObject type = new JObject();

            if (t != null)
            {
                JArray terminals = new JArray();

                foreach (var item in _nameDevice)
                {
                    terminals.Add(item.Key);
                }

                type["show"] = terminals;

                FillResponseMessage(type.ToString(), HttpStatusCode.OK, "json/application; charset=utf-8", ctx);
            }
            else
            {
                type["error"] = "Error message. Undefined command!";

                FillResponseMessage(type.ToString(), HttpStatusCode.BadRequest, "json/application; charset=utf-8", ctx);
            }
        }

        void ParseMessageFromTerminal(string msg, Socket socket)
        {
            dynamic jResponse = JsonConvert.DeserializeObject(msg);
            dynamic t = jResponse.identifier;

            if (t != null)
            {
                try
                {
                    int idSocket = socket.Handle.ToInt32();
                    string nameTerminal = t.name_terminal;

                    try
                    {
                        int d = Convert.ToInt32(nameTerminal);

                        _nameDevice.Add(nameTerminal, socket);
                        _idName.Add(idSocket, nameTerminal);
                    }
                    catch
                    {
                        _countingTerminal = (_countingTerminal + 1) % 10000;
                        string c = Convert.ToString(_countingTerminal);

                        _nameDevice.Add(nameTerminal + " " + c, socket);
                        _idName.Add(idSocket, nameTerminal + " " + c);
                    }
                }
                catch(Exception ex)
                {
                    _log.Error("Method _ParseMessageFromTerminal_", ex);
                }
            }
        }
    }
}
