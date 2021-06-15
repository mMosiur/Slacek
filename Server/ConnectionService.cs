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

        private bool SendDialogMessage(string resource, string payload)
        {
            const int millisecondsWaitTime = 1000;
            _tunnel.Send("alert");
            _logger.LogInformation("Alert sent to client");
            if(!_tunnel.Wait(millisecondsWaitTime)) return false;
            string reply = _tunnel.Receive();
            if(reply != "listening") return false;
            _tunnel.Send(resource);
            if(!_tunnel.Wait(millisecondsWaitTime)) return false;
            reply = _tunnel.Receive();
            if(reply != "accept") return false;
            _tunnel.Send($"{payload}");
            if(!_tunnel.Wait(millisecondsWaitTime)) return false;
            reply = _tunnel.Receive();
            return reply == "ok";
        }

        public void ExternalNewMessage(Message message)
        {
            string resource = "new message";
            ISerializer<Message> serializer = new MessageSerializer();
            string payload = serializer.Serialize(message);
            if(SendDialogMessage(resource, payload))
            {
                _logger.LogInformation("Notification about new message sent");
            }
            else
            {
                _logger.LogError("An error occurred during in new message notification");
            }
        }

        public void ExternalNewUserInGroup(int groupId, User user)
        {
            string resource = "new user";
            ISerializer<User> serializer = new UserSerializer();
            string payload = $"{groupId} {serializer.Serialize(user)}";
            if(SendDialogMessage(resource, payload))
            {
                _logger.LogInformation("Notification about new user sent");
            }
            else
            {
                _logger.LogError("An error occurred during in new user notification");
            }
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
            Headers headers = new();
            headers.Method = "pong";
            headers.StatusCode = "ok";
            headers["command"] = string.Join(' ', command.Skip(1));
            _tunnel.Send(headers.ToString());
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
                string login = command[1];
                string username = Encoding.UTF8.GetString(Convert.FromBase64String(command[2]));
                string password = Encoding.UTF8.GetString(Convert.FromBase64String(command[3]));
                _authenticatedUser = _databaseManager.RegisterNewUser(login, username, password);
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
            Headers headers = new()
            {
                Method = command[0],
                StatusCode = "ok"
            };
            headers["payload"] = payload;
            _tunnel.Send(headers.ToString());
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
                string login = command[1];
                string password = Encoding.UTF8.GetString(Convert.FromBase64String(command[2]));
                _authenticatedUser = _databaseManager.AuthenticateUser(login, password);
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

            Headers headers = new()
            {
                Method = command[0],
                StatusCode = "ok"
            };

            ISerializer<User> serializer = new UserSerializer();
            string payload = serializer.Serialize(_authenticatedUser);
            headers["payload"] = payload;
            _tunnel.Send(headers.ToString());
            _logger.LogInformation("Reply has been sent back");
        }

        private void UnauthenticateCommand(string[] command)
        {
            const int expectedLength = 1;
            _logger.LogInformation("Authenticate command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received authenticate command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            _authenticatedUser = null;
            _logger.LogInformation("User has been unauthenticated");
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

        private void JoinCommand(string[] command)
        {
            const int expectedLength = 2;
            _logger.LogInformation("Join command requested");
            if (command.Length != expectedLength)
            {
                string errorMsg = $"Received Join command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            if (_authenticatedUser is null)
            {
                string errorMsg = "Received Join command from an unauthenticated user";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            byte[] bytes = Convert.FromBase64String(command[1]);
            string name = Encoding.UTF8.GetString(bytes);
            Group group = _databaseManager.JoinGroup(_authenticatedUser.UserId, name);
            if (group is null)
            {
                string errorMsg = $"Group with name \"{command[1]}\" could not be found";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            ISerializer<Group> serializer = new GroupSerializer();
            string payload = serializer.Serialize(group);
            Headers headers = new()
            {
                Method = command[0],
                StatusCode = "ok"
            };
            headers["group"] = payload;
            _tunnel.Send(headers.ToString());
            _logger.LogInformation("Reply has been sent back");
            OnNewUserInGroup(new NewUserInGroupEventArgs(group.GroupId, _authenticatedUser));
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
                Headers headers = new()
                {
                    Method = command[0],
                    StatusCode = "ok"
                };
                headers["resource"] = command[1];
                headers["group"] = payload;
                _tunnel.Send(headers.ToString());
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
                string msg = "Received GetGroups command from an unauthenticated user";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            ICollection<Group> groups = _databaseManager.GetUserGroups(_authenticatedUser.UserId);
            ISerializer<ICollection<Group>> serializer = new CollectionSerializer<Group>();
            string payload = serializer.Serialize(groups);
            Headers headers = new()
            {
                Method = command[0],
                StatusCode = "ok"
            };
            headers["resource"] = command[1];
            headers["payload"] = payload;
            _tunnel.Send(headers.ToString());
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
            if (_authenticatedUser is null)
            {
                string msg = "Received GetUsers command from an unauthenticated user";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            if (!int.TryParse(command[2], out int groupId))
            {
                string msg = $"Given ID \"{groupId}\" could not be parsed";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            ICollection<User> users = _databaseManager.GetGroupUsers(groupId);
            if (users is null)
            {
                string msg = $"Group with ID \"{groupId}\" could not be found";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            ISerializer<ICollection<User>> serializer = new CollectionSerializer<User>();
            string payload = serializer.Serialize(users);
            Headers headers = new()
            {
                Method = command[0],
                StatusCode = "ok"
            };
            headers["resource"] = command[1];
            headers["group-id"] = command[2];
            headers["payload"] = payload;
            _tunnel.Send(headers.ToString());
            _logger.LogInformation("Reply has been sent back");
        }

        private void GetMessagesCommand(string[] command)
        {
            const int expectedLength = 3;
            _logger.LogInformation("GetMessages command requested");
            if (command.Length != expectedLength)
            {
                string msg = $"Received GetMessages command is not in a proper format. Expected {expectedLength} parts, received {command.Length}.";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            string preamble = $"{command[0]} {command[1]} {command[2]}";
            if (_authenticatedUser is null)
            {
                string msg = "Received GetMessages command from an unauthenticated user";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            if (!int.TryParse(command[2], out int groupId))
            {
                string msg = $"Given ID \"{groupId}\" could not be parsed";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            ICollection<Message> messages = _databaseManager.GetGroupMessages(groupId);
            if (messages is null)
            {
                string msg = $"Group with ID \"{groupId}\" could not be found";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            ISerializer<ICollection<Message>> serializer = new CollectionSerializer<Message>();
            string payload = serializer.Serialize(messages);
            Headers headers = new()
            {
                Method = command[0],
                StatusCode = "ok"
            };
            headers["resource"] = command[1];
            headers["group-id"] = groupId.ToString();
            headers["payload"] = payload;
            _tunnel.Send(headers.ToString());
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

                            case "unauthenticate":
                                UnauthenticateCommand(command);
                                break;

                            case "new":
                                NewCommand(command);
                                break;

                            case "join":
                                JoinCommand(command);
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

                        Headers headers = new()
                        {
                            Method = command[0],
                            StatusCode = "err"
                        };
                        _tunnel.Send(headers.ToString());
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
