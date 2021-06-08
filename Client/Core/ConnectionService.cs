using Slacek.Client.Core.Models;
using Slacek.Client.Core.Serializers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Slacek.Client.Core
{
    public class ConnectionService : IDisposable
    {
        private enum CommandType
        {
            Ping,
            Register,
            Authenticate,
            GetGroups,
            GetUsers,
            GetMessages
        }

        private readonly Queue<CommandType> _unhandledRequests = new Queue<CommandType>(1);
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

        private void EnqueueRequest(CommandType commandType) => _unhandledRequests.Enqueue(commandType);

        private void DequeueRequest(CommandType commandType)
        {
            if (_unhandledRequests.Count == 0 || _unhandledRequests.Peek() != commandType)
            {
                throw new Exception("Unexpected server reply");
            }
            _ = _unhandledRequests.Dequeue();
        }

        private void HandleNewMessage(string[] command)
        {
            ISerializer<Message> serializer = new MessageSerializer();
            Message newMessage = serializer.Deserialize(command[2]);
            OnNewMessageReceived(new NewMessageReceivedEventArgs(newMessage));
        }

        private void HandleNewUser(string[] command)
        {
            ISerializer<User> serializer = new UserSerializer();
            if (!int.TryParse(command[2], out int groupId))
            {
                throw new Exception($"Could not parse group ID: \"{command[2]}\"");
            }
            User user = serializer.Deserialize(command[3]);
            OnNewUserInGroupReceived(new NewUserInGroupReceivedEventArgs(groupId, user));
        }

        private void HandleNewGroup(string[] command)
        {
            ISerializer<Group> serializer = new GroupSerializer();
            Group group = serializer.Deserialize(command[2]);
            OnNewGroupReceived(new NewGroupReceivedEventArgs(group));
        }

        private void HandleJoinGroup(string[] command)
        {
            ISerializer<Group> serializer = new GroupSerializer();
            Group group = serializer.Deserialize(command[2]);
            OnNewGroupReceived(new NewGroupReceivedEventArgs(group));
        }

        private void HandleNew(string resource, string[] command)
        {
            switch (resource)
            {
                case "message":
                    HandleNewMessage(command);
                    break;

                case "user":
                    HandleNewUser(command);
                    break;

                case "group":
                    HandleNewGroup(command);
                    break;

                default:
                    throw new Exception($"Unrecognized resource: \"{resource}\"");
            }
        }

        private void HandlePingReply(string payload)
        {
            DequeueRequest(CommandType.Ping);
            Console.WriteLine($"pong {payload}");
        }

        private void HandleAuthenticateReply(string status, string payload)
        {
            DequeueRequest(CommandType.Authenticate);
            switch (status)
            {
                case "ok":
                    ISerializer<User> serializer = new UserSerializer();
                    User user = serializer.Deserialize(payload);
                    AuthenticatedUser = user;
                    OnUserAuthentication(new UserAuthenticationEventArgs(user));
                    break;

                case "err":
                    AuthenticatedUser = null;
                    OnUserAuthentication(UserAuthenticationEventArgs.Failed);
                    break;

                default:
                    throw new Exception($"Unexpected server reply status: \"{status}\"");
            }
        }

        private void HandleRegisterReply(string status, string payload)
        {
            DequeueRequest(CommandType.Register);
            if (status == "ok")
            {
                ISerializer<User> serializer = new UserSerializer();
                User user = serializer.Deserialize(payload);
                AuthenticatedUser = user;
                OnUserRegistration(new UserRegistrationEventArgs(user));
            }
            else if (status == "err")
            {
                AuthenticatedUser = null;
                OnUserRegistration(UserRegistrationEventArgs.Failed);
            }
            else
            {
                throw new Exception($"Unexpected server reply status: \"{status}\"");
            }
        }

        private void HandleGetGroups(string payload)
        {
            DequeueRequest(CommandType.GetGroups);
            if (payload == "err")
            {
                OnGetGroupsReceived(GetGroupsReceivedEventArgs.Failed);
                return;
            }
            ISerializer<ICollection<Group>> serializer = new CollectionSerializer<Group>();
            ICollection<Group> groups = serializer.Deserialize(payload);
            OnGetGroupsReceived(new GetGroupsReceivedEventArgs(groups));
        }

        private void HandleGetUsers(string groupIdStr, string payload)
        {
            DequeueRequest(CommandType.GetUsers);
            if (!int.TryParse(groupIdStr, out int groupId))
            {
                throw new Exception($"Could not parse group ID: \"{groupIdStr}\"");
            }
            if (payload == "err")
            {
                OnGetUsersReceived(GetUsersReceivedEventArgs.Failed(groupId));
                return;
            }
            ISerializer<ICollection<User>> serializer = new CollectionSerializer<User>();
            ICollection<User> users = serializer.Deserialize(payload);
            OnGetUsersReceived(new GetUsersReceivedEventArgs(groupId, users));
        }

        private void HandleGetMessages(string groupIdStr, string payload)
        {
            DequeueRequest(CommandType.GetMessages);
            if (!int.TryParse(groupIdStr, out int groupId))
            {
                throw new Exception($"Could not parse group ID: \"{groupIdStr}\"");
            }
            if (payload == "err")
            {
                OnGetMessagesReceived(GetMessagesReceivedEventArgs.Failed(groupId));
                return;
            }
            ISerializer<ICollection<Message>> serializer = new CollectionSerializer<Message>();
            ICollection<Message> messages = serializer.Deserialize(payload);
            OnGetMessagesReceived(new GetMessagesReceivedEventArgs(groupId, messages));
        }

        private void HandleGetReply(string resource, string[] reply)
        {
            switch (resource)
            {
                case "groups":
                    HandleGetGroups(reply[2]);
                    break;

                case "users":
                    HandleGetUsers(reply[2], reply[3]);
                    break;

                case "messages":
                    HandleGetMessages(reply[2], reply[3]);
                    break;

                default:
                    throw new Exception($"Unknown resource returned by server: \"{resource}\"");
            }
        }

        private void HandleReply(string[] reply)
        {
            switch (reply[0])
            {
                case "pong":
                    HandlePingReply(reply[1]);
                    break;

                case "new":
                    HandleNew(reply[1], reply);
                    break;

                case "authenticate":
                    HandleAuthenticateReply(reply[1], reply.Length > 2 ? reply[2] : null);
                    break;

                case "register":
                    HandleRegisterReply(reply[1], reply.Length > 2 ? reply[2] : null);
                    break;

                case "get":
                    HandleGetReply(reply[1], reply);
                    break;

                case "join":
                    HandleJoinGroup(reply);
                    break;

                default:
                    throw new Exception($"Unrecognized command \"{reply[0]}\"");
            }
        }

        private void ReceiveAndHandleIfAvailable()
        {
            if (!_tunnel.Available)
            {
                return;
            }
            string message = _tunnel.Receive();
            string[] reply = message.Split(' ');
            HandleReply(reply);
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
            EnqueueRequest(CommandType.Authenticate);
            _tunnel?.Send($"authenticate {login} {password}");
        }

        public void Unauthenticate()
        {
            _tunnel?.Send("unauthenticate");
            AuthenticatedUser = null;
        }

        public void Register(string login, string username, string password)
        {
            EnqueueRequest(CommandType.Register);
            _tunnel?.Send($"register {login} {username} {password}");
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

        public void Ping(string message = null)
        {
            EnqueueRequest(CommandType.Ping);
            if (message is null)
            {
                _tunnel?.Send("ping");
            }
            else
            {
                _tunnel?.Send($"ping {message}");
            }
        }

        public void GetGroups()
        {
            EnqueueRequest(CommandType.GetGroups);
            _tunnel?.Send($"get groups");
        }

        public void GetUsers(int groupId)
        {
            EnqueueRequest(CommandType.GetUsers);
            _tunnel?.Send($"get users {groupId}");
        }

        public void GetMessages(int groupId)
        {
            EnqueueRequest(CommandType.GetMessages);
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
