using Microsoft.Extensions.Logging;
using Slacek.Server.Models;
using Slacek.Server.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Slacek.Server
{
    public class NewMessageEventArgs : EventArgs
    {
        public Message Message { get; }

        public NewMessageEventArgs(Message message)
        {
            Message = message;
        }
    }

    public class NewUserInGroupEventArgs : EventArgs
    {
        public int GroupId { get; }
        public User User { get; }

        public NewUserInGroupEventArgs(int groupId, User user)
        {
            GroupId = groupId;
            User = user;
        }
    }

    public class ConnectionService : IDisposable
    {
        private readonly CommunicationTunnel _tunnel;
        private readonly DatabaseManager _databaseManager;
        private readonly ILogger _logger;
        private User _authenticatedUser;
        private bool _disposed;

        public User AuthenticatedUser => _authenticatedUser;

        public event EventHandler<NewMessageEventArgs> NewMessage;

        protected virtual void OnNewMessage(NewMessageEventArgs e) => NewMessage?.Invoke(this, e);

        public event EventHandler<NewUserInGroupEventArgs> NewUserInGroup;

        protected virtual void OnNewUserInGroup(NewUserInGroupEventArgs e) => NewUserInGroup?.Invoke(this, e);

        public void ExternalNewMessage(Message message)
        {
            _logger.LogInformation("External message invoked");
            _logger.LogInformation("Sending external message");
            ISerializer<Message> serializer = new MessageSerializer();
            string payload = serializer.Serialize(message);
            _tunnel.Send($"new message {payload}");
        }

        public void ExternalNewUserInGroup(int groupId, User user)
        {
            _logger.LogInformation("External user in group invoked");
            _logger.LogInformation("Sending external user in group");
            ISerializer<User> serializer = new UserSerializer();
            string payload = serializer.Serialize(user);
            _tunnel.Send($"new user {groupId} {payload}");
        }

        public ConnectionService(CommunicationTunnel tunnel, DatabaseManager databaseManager, ILogger logger)
        {
            _tunnel = tunnel;
            _databaseManager = databaseManager;
            _logger = logger;
            _authenticatedUser = null;
        }

        private void PingCommand(string[] command)
        {
            _logger.LogInformation("Ping command requested");
            _tunnel.Send($"pong {string.Join(' ', string.Join(' ', command, 1, command.Length - 1))}");
            _logger.LogInformation("Reply has been sent back");
        }

        private void RegisterCommand(string[] command)
        {
            const int expectedLength = 4;
            _logger.LogInformation("Register command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received register command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            try
            {
                _authenticatedUser = _databaseManager.RegisterNewUser(command[1], command[2], command[3]);
            }
            catch (Exception e)
            {
                _logger.LogError($"Database error: {e.Message}");
                throw;
            }
            if (_authenticatedUser == null)
            {
                string errorMsg = "New user registration was not successful.";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            _logger.LogInformation("New user registration was successful");
            ISerializer<User> serializer = new UserSerializer();
            string payload = serializer.Serialize(_authenticatedUser);
            _tunnel.Send($"{command[0]} ok {payload}");
            _logger.LogInformation("Reply has been sent back");
            OnNewUserInGroup(new NewUserInGroupEventArgs(AuthenticatedUser.Groups.First().GroupId, AuthenticatedUser));
        }

        private void AuthenticateCommand(string[] command)
        {
            const int expectedLength = 3;
            _logger.LogInformation("Authenticate command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received authenticate command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            try
            {
                _authenticatedUser = _databaseManager.AuthenticateUser(command[1], command[2]);
            }
            catch (Exception e)
            {
                _logger.LogError($"Database error: {e.Message}");
                throw;
            }
            if (_authenticatedUser == null)
            {
                string errorMsg = "User authentication was not successful";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            _logger.LogInformation("User authentication was successful");

            ISerializer<User> serializer = new UserSerializer();
            string payload = serializer.Serialize(_authenticatedUser);
            _tunnel.Send($"{command[0]} ok {payload}");
            _logger.LogInformation("Reply has been sent back");
        }

        private void NewCommand(string[] command)
        {
            switch (command[1])
            {
                case "message":
                    NewMessageCommand(command);
                    break;

                case "group":
                    NewGroupCommand(command);
                    break;
            }
        }

        private void NewMessageCommand(string[] command)
        {
            const int expectedLength = 4;
            _logger.LogInformation("New message command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received NewMessage command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
                return;
            }
            if (!int.TryParse(command[2], out int groupId))
            {
                string errorMsg = $"Could not parse group ID: \"{groupId}\"";
                _logger.LogError(errorMsg);
                return;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(command[3]);
                string content = Encoding.UTF8.GetString(bytes);
                Message message = _databaseManager.NewMessage(_authenticatedUser.UserId, groupId, content);
                OnNewMessage(new NewMessageEventArgs(message));
            }
            catch (Exception e)
            {
                _logger.LogError($"Database error: {e.Message}");
                return;
            }
        }

        private void NewGroupCommand(string[] command)
        {
            const int expectedLength = 3;
            _logger.LogInformation("New group command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received NewMessage command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
                return;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(command[2]);
                string name = Encoding.UTF8.GetString(bytes);
                Group group = _databaseManager.CreateNewGroup(_authenticatedUser.UserId, name);

                string preamble = $"{command[0]} {command[1]}";
                ISerializer<Group> serializer = new GroupSerializer();
                string payload = serializer.Serialize(group);
                _tunnel.Send($"{preamble} {payload}");
                _logger.LogInformation("Reply has been sent back");
            }
            catch (Exception e)
            {
                _logger.LogError($"Database error: {e.Message}");
                return;
            }
        }

        private void GetGroupsCommand(string[] command)
        {
            const int expectedLength = 2;
            _logger.LogInformation("GetGroups command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received GetGroups command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
            }
            string preamble = $"{command[0]} {command[1]}";
            if (_authenticatedUser is null)
            {
                _logger.LogError("Received GetGroups command from an unauthenticated user");
                _tunnel.Send($"{preamble} err");
                return;
            }
            ICollection<Group> groups = _databaseManager.GetUserGroups(_authenticatedUser.UserId);
            ISerializer<ICollection<Group>> serializer = new CollectionSerializer<Group>();
            string payload = serializer.Serialize(groups);
            _tunnel.Send($"{preamble} {payload}");
            _logger.LogInformation("Reply has been sent back");
        }

        private void GetUsersCommand(string[] command)
        {
            const int expectedLength = 3;
            _logger.LogInformation("GetUsers command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received GetUsers command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
            }
            string preamble = $"{command[0]} {command[1]} {command[2]}";
            if (_authenticatedUser is null)
            {
                _logger.LogError("Received GetUsers command from an unauthenticated user");
                _tunnel.Send($"{preamble} err");
                return;
            }
            if (!int.TryParse(command[2], out int groupId))
            {
                _logger.LogError($"Given ID \"{groupId}\" could not be parsed");
                _tunnel.Send($"{preamble} err");
                return;
            }
            ICollection<User> users = _databaseManager.GetGroupUsers(groupId);
            if (users is null)
            {
                _logger.LogError($"Group with ID \"{groupId}\" could not be found");
                _tunnel.Send($"{preamble} err");
                return;
            }
            ISerializer<ICollection<User>> serializer = new CollectionSerializer<User>();
            string payload = serializer.Serialize(users);
            _tunnel.Send($"{preamble} {payload}");
            _logger.LogInformation("Reply has been sent back");
        }

        private void GetMessagesCommand(string[] command)
        {
            const int expectedLength = 3;
            _logger.LogInformation("GetMessages command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received GetMessages command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
            }
            string preamble = $"{command[0]} {command[1]} {command[2]}";
            if (_authenticatedUser is null)
            {
                _logger.LogError("Received GetMessages command from an unauthenticated user");
                _tunnel.Send($"{preamble} err");
                return;
            }
            if (!int.TryParse(command[2], out int groupId))
            {
                _logger.LogError($"Given ID \"{groupId}\" could not be parsed");
                _tunnel.Send($"{preamble} err");
                return;
            }
            ICollection<Message> messages = _databaseManager.GetGroupMessages(groupId);
            if (messages is null)
            {
                _logger.LogError($"Group with ID \"{groupId}\" could not be found");
                _tunnel.Send($"{preamble} err");
                return;
            }
            ISerializer<ICollection<Message>> serializer = new CollectionSerializer<Message>();
            string payload = serializer.Serialize(messages);
            _tunnel.Send($"{preamble} {payload}");
            _logger.LogInformation("Reply has been sent back");
        }

        private void GetCommand(string[] command)
        {
            switch (command[1])
            {
                case "groups":
                    GetGroupsCommand(command);
                    break;

                case "users":
                    GetUsersCommand(command);
                    break;

                case "messages":
                    GetMessagesCommand(command);
                    break;

                default:
                    _logger.LogError($"Unknown get command: \"{command[1]}\"");
                    break;
            }
        }

        public void Serve()
        {
            while (_tunnel.IsSocketLive)
            {
                if (_tunnel.Available)
                {
                    string message = _tunnel.Receive();
                    _logger.LogInformation($"Received: \"{message}\"");
                    string[] command = message.Split(' ');
                    try
                    {
                        switch (command[0])
                        {
                            case "ping":
                                PingCommand(command);
                                break;

                            case "register":
                                RegisterCommand(command);
                                break;

                            case "authenticate":
                                AuthenticateCommand(command);
                                break;

                            case "new":
                                NewCommand(command);
                                break;

                            case "get":
                                GetCommand(command);
                                break;

                            default:
                                _logger.LogError($"Unknown command: \"{command[0]}\"");
                                throw new Exception("Unknown command");
                        }
                    }
                    catch (Exception)
                    {
                        _tunnel.Send($"{command[0]} err");
                    }
                }
                Thread.Sleep(100);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _tunnel.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
