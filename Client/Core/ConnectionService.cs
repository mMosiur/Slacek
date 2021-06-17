using Slacek.Client.Core.Models;
using Slacek.Client.Core.Serializers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Slacek.Client.Core
{
    public class ConnectionService : IDisposable
    {
        private readonly CommunicationTunnel _tunnel = null;
        private Thread _listeningThread = null;
        private bool _closeThread = false;

        public User AuthenticatedUser { get; private set; }
        public bool IsUserAuthenticated => !(AuthenticatedUser is null);

        public event EventHandler<NewMessageReceivedEventArgs> NewMessageReceived;

        protected virtual void OnNewMessageReceived(NewMessageReceivedEventArgs e) => NewMessageReceived?.Invoke(this, e);

        public event EventHandler<NewUserInGroupReceivedEventArgs> NewUserInGroupReceived;

        protected virtual void OnNewUserInGroupReceived(NewUserInGroupReceivedEventArgs e) => NewUserInGroupReceived?.Invoke(this, e);

        public event EventHandler<UserAuthenticationEventArgs> UserAuthentication;

        protected virtual void OnUserAuthentication(UserAuthenticationEventArgs e) => UserAuthentication?.Invoke(this, e);

        public event EventHandler<UserRegistrationEventArgs> UserRegistration;

        protected virtual void OnUserRegistration(UserRegistrationEventArgs e) => UserRegistration?.Invoke(this, e);

        public event EventHandler<GetGroupsReceivedEventArgs> GetGroupsReceived;

        protected virtual void OnGetGroupsReceived(GetGroupsReceivedEventArgs e) => GetGroupsReceived?.Invoke(this, e);

        public event EventHandler<GetUsersReceivedEventArgs> GetUsersReceived;

        protected virtual void OnGetUsersReceived(GetUsersReceivedEventArgs e) => GetUsersReceived?.Invoke(this, e);

        public event EventHandler<GetMessagesReceivedEventArgs> GetMessagesReceived;

        protected virtual void OnGetMessagesReceived(GetMessagesReceivedEventArgs e) => GetMessagesReceived?.Invoke(this, e);

        public event EventHandler<NewGroupReceivedEventArgs> NewGroupReceived;

        protected virtual void OnNewGroupReceived(NewGroupReceivedEventArgs e) => NewGroupReceived?.Invoke(this, e);

        public ConnectionService(string host, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(host, port);
                _tunnel = new CommunicationTunnel(socket);
                _tunnel.Open();
                _listeningThread = new Thread(Listen);
                _listeningThread.Start();
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e.Message}");
            }
        }

        private void HandleNotificationUser(string json)
        {
            NewUserNotification notification = JsonSerializer.Deserialize<NewUserNotification>(json);
            int groupId = notification.GroupId;
            ISerializer<User> serializer = new UserSerializer();
            User user = serializer.Deserialize(notification.SerializedUser);
            OnNewUserInGroupReceived(new NewUserInGroupReceivedEventArgs(groupId, user));
        }

        private void HandleNotificationMessage(string json)
        {
            NewMessageNotification notification = JsonSerializer.Deserialize<NewMessageNotification>(json);
            ISerializer<Message> serializer = new MessageSerializer();
            Message message = serializer.Deserialize(notification.SerializedMessage);
            OnNewMessageReceived(new NewMessageReceivedEventArgs(message));
        }

        private void HandleNewGroup(Headers headers)
        {
            ISerializer<Group> serializer = new GroupSerializer();
            Group group = serializer.Deserialize(headers["group"]);
            OnNewGroupReceived(new NewGroupReceivedEventArgs(group));
        }

        private void HandleJoinGroup(Headers headers)
        {
            ISerializer<Group> serializer = new GroupSerializer();
            Group group = serializer.Deserialize(headers["group"]);
            OnNewGroupReceived(new NewGroupReceivedEventArgs(group));
        }

        private void HandleNew(Headers headers)
        {
            string resource = headers["resource"];
            switch (resource)
            {
                case "group":
                    HandleNewGroup(headers);
                    break;

                default:
                    throw new Exception($"Unrecognized resource: \"{resource}\"");
            }
        }

        private void HandlePingReply(Headers headers)
        {
            bool hasPayload = headers.TryGetValue("payload", out string payload);
            if(hasPayload)
            {
                Console.WriteLine($"{headers.Method} {payload}");
            }
            else
            {
                Console.WriteLine($"{headers.Method}");
            }
        }

        private void HandleAuthenticateReply(Headers headers)
        {
            switch (headers.StatusCode)
            {
                case "ok":
                    ISerializer<User> serializer = new UserSerializer();
                    User user = serializer.Deserialize(headers["payload"]);
                    AuthenticatedUser = user;
                    OnUserAuthentication(new UserAuthenticationEventArgs(user));
                    break;

                case "err":
                    AuthenticatedUser = null;
                    OnUserAuthentication(UserAuthenticationEventArgs.Failed);
                    break;

                default:
                    throw new Exception($"Unexpected server reply status: \"{headers.StatusCode}\"");
            }
        }

        private void HandleRegisterReply(Headers header)
        {
            if (header.StatusCode == "ok")
            {
                ISerializer<User> serializer = new UserSerializer();
                User user = serializer.Deserialize(header["payload"]);
                AuthenticatedUser = user;
                OnUserRegistration(new UserRegistrationEventArgs(user));
            }
            else if (header.StatusCode == "err")
            {
                AuthenticatedUser = null;
                OnUserRegistration(UserRegistrationEventArgs.Failed);
            }
            else
            {
                throw new Exception($"Unexpected server reply status: \"{header.StatusCode}\"");
            }
        }

        private void HandleGetGroups(Headers headers)
        {
            if (headers["payload"] == "err")
            {
                OnGetGroupsReceived(GetGroupsReceivedEventArgs.Failed);
                return;
            }
            ISerializer<ICollection<Group>> serializer = new CollectionSerializer<Group>();
            ICollection<Group> groups = serializer.Deserialize(headers["payload"]);
            OnGetGroupsReceived(new GetGroupsReceivedEventArgs(groups));
        }

        private void HandleGetUsers(Headers headers)
        {
            string rawGroupId = headers["group-id"];
            if (!int.TryParse(headers["group-id"], out int groupId))
            {
                throw new Exception($"Could not parse group ID: \"{rawGroupId}\"");
            }
            if (headers["payload"] == "err")
            {
                OnGetUsersReceived(GetUsersReceivedEventArgs.Failed(groupId));
                return;
            }
            ISerializer<ICollection<User>> serializer = new CollectionSerializer<User>();
            ICollection<User> users = serializer.Deserialize(headers["payload"]);
            OnGetUsersReceived(new GetUsersReceivedEventArgs(groupId, users));
        }

        private void HandleGetMessages(Headers headers)
        {
            string rawGroupId = headers["group-id"];
            if (!int.TryParse(headers["group-id"], out int groupId))
            {
                throw new Exception($"Could not parse group ID: \"{rawGroupId}\"");
            }
            if (headers["payload"] == "err")
            {
                OnGetMessagesReceived(GetMessagesReceivedEventArgs.Failed(groupId));
                return;
            }
            ISerializer<ICollection<Message>> serializer = new CollectionSerializer<Message>();
            ICollection<Message> messages = serializer.Deserialize(headers["payload"]);
            OnGetMessagesReceived(new GetMessagesReceivedEventArgs(groupId, messages));
        }

        private void HandleGetReply(Headers headers)
        {
            string resource = headers["resource"];
            switch (resource)
            {
                case "groups":
                    HandleGetGroups(headers);
                    break;

                case "users":
                    HandleGetUsers(headers);
                    break;

                case "messages":
                    HandleGetMessages(headers);
                    break;

                default:
                    throw new Exception($"Unknown resource returned by server: \"{resource}\"");
            }
        }

        private void HandleReply(Headers headers)
        {
            switch (headers.Method)
            {
                case "pong":
                    HandlePingReply(headers);
                    break;

                case "new":
                    HandleNew(headers);
                    break;

                case "authenticate":
                    HandleAuthenticateReply(headers);
                    break;

                case "register":
                    HandleRegisterReply(headers);
                    break;

                case "get":
                    HandleGetReply(headers);
                    break;

                case "join":
                    HandleJoinGroup(headers);
                    break;

                default:
                    throw new Exception($"Unrecognized command \"{headers.Method}\"");
            }
        }

        private void HandleNotification(string notification)
        {
            string[] parts = notification.Split("\r\n", 3);
            if(parts.Length < 3)
                return;
            if(parts[0] != "notification")
                return;
            switch(parts[1])
            {
                case "message":
                    HandleNotificationMessage(parts[2]);
                    break;
                case "user":
                    HandleNotificationUser(parts[2]);
                    break;
                default:
                    return;
            }
        }

        private void ReceiveAndHandleIfAvailable()
        {
            if (!_tunnel.Available)
            {
                return;
            }
            string message = _tunnel.Receive();
            if(message.StartsWith("notification"))
            {
                HandleNotification(message);
            }
            else
            {
                Headers headers = Headers.Parse(message);
                HandleReply(headers);
            }
        }

        private void Listen()
        {
            while (!_closeThread)
            {
                ReceiveAndHandleIfAvailable();
                Thread.Sleep(100);
            }
        }

        public void Authenticate(string login, string password)
        {
            string encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            _tunnel?.Send($"authenticate {login} {encodedPassword}");
        }

        public void Unauthenticate()
        {
            _tunnel?.Send("unauthenticate");
            AuthenticatedUser = null;
        }

        public void Register(string login, string username, string password)
        {
            string encodedUsername = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
            string encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            _tunnel?.Send($"register {login} {encodedUsername} {encodedPassword}");
        }

        public bool SendNewMessage(Message message)
        {
            if (message.UserId != AuthenticatedUser.UserId)
            {
                return false;
            }
            return SendNewMessage(message.GroupId, message.Content);
        }

        public bool SendNewMessage(int groupId, string content)
        {
            if (!IsUserAuthenticated)
            {
                return false;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            string payload = Convert.ToBase64String(bytes);
            _tunnel?.Send($"new message {groupId} {payload}");
            return true;
        }

        public bool CreateNewGroup(string name)
        {
            if (!IsUserAuthenticated)
            {
                return false;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(name);
            string payload = Convert.ToBase64String(bytes);
            _tunnel?.Send($"new group {payload}");
            return true;
        }

        public bool JoinGroup(string name)
        {
            if (!IsUserAuthenticated)
            {
                return false;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(name);
            string payload = Convert.ToBase64String(bytes);
            _tunnel?.Send($"join {payload}");
            return true;
        }

        public void Ping(string payload = null)
        {
            if (payload is null)
            {
                _tunnel?.Send("ping");
            }
            else
            {
                _tunnel?.Send($"ping {payload}");
            }
        }

        public void GetGroups()
        {
            _tunnel?.Send($"get groups");
        }

        public void GetUsers(int groupId)
        {
            _tunnel?.Send($"get users {groupId}");
        }

        public void GetMessages(int groupId)
        {
            _tunnel?.Send($"get messages {groupId}");
        }

        protected void Dispose(bool disposing)
        {
            _closeThread = true;
            if (!(_listeningThread is null || _listeningThread.Join(1000)))
            {
                throw new Exception("Listening thread did not close properly");
            }
            _listeningThread = null;
            if (disposing)
            {
                _tunnel?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConnectionService()
        {
            Dispose(false);
        }
    }
}
