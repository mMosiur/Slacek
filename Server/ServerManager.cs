using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Slacek.Server
{
    public class ConnectionManagerConfiguration
    {
        public ConnectionManagerConfiguration(int port, string address)
        {
            Port = port;
            Address = IPAddress.Parse(address);
        }

        public ConnectionManagerConfiguration(int port, IPAddress address)
        {
            Port = port;
            Address = address;
        }

        public IPAddress Address { get; set; }
        public int Port { get; set; }
    }

    public class ServerManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConnectionManagerConfiguration _configuration;

        private readonly Socket _listenerSocket;

        private readonly DatabaseManager _databaseManager;

        private readonly ICollection<ConnectionService> _connections = new HashSet<ConnectionService>();

        public ServerManager(ILogger logger, ConnectionManagerConfiguration configuration, DatabaseManager databaseManager)
        {
            _logger = logger;
            _configuration = configuration;
            _listenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(_configuration.Address, _configuration.Port);
            _listenerSocket.Bind(endPoint);
            _databaseManager = databaseManager;
            _logger.LogInformation($"ConnectionManager started on {_configuration.Address}:{_configuration.Port}");
        }

        private void Connection_NewMessage(object sender, NewMessageEventArgs e)
        {
            foreach (ConnectionService connection in _connections)
            {
                if (ReferenceEquals(sender, connection)) continue;
                connection.ExternalNewMessage(e.Message);
            }
        }

        private void Connection_NewUserInGroup(object sender, NewUserInGroupEventArgs e)
        {
            foreach (ConnectionService connection in _connections)
            {
                if (ReferenceEquals(sender, connection)) continue;
                connection.ExternalNewUserInGroup(e.GroupId, e.User);
            }
        }

        private void ServeClient(Socket clientSocket)
        {
            CommunicationTunnel tunnel = new CommunicationTunnel(clientSocket);
            tunnel.Open();
            using ConnectionService connection = new(tunnel, _databaseManager, new ConsoleLogger(LogLevel.Information));
            _connections.Add(connection);
            connection.NewMessage += Connection_NewMessage;
            connection.NewUserInGroup += Connection_NewUserInGroup;
            connection.Serve();
            _connections.Remove(connection);
        }

        private void ServeClient(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (obj is not Socket socket)
            {
                throw new ArgumentException($"Argument is not an expected type", nameof(obj));
            }
            ServeClient(socket);
            _logger.LogInformation("Client disconnected");
        }

        public void Run()
        {
            _listenerSocket.Listen(4);
            while (true)
            {
                Socket clientSocket = _listenerSocket.Accept();
                IPAddress remoteAddress = ((IPEndPoint)clientSocket.RemoteEndPoint).Address;
                _logger.LogInformation($"Connected: {remoteAddress}");
                Thread thread = new Thread(ServeClient);
                thread.Start(clientSocket);
            }
        }

        public void Dispose()
        {
            _listenerSocket.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
