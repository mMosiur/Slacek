using Slacek.Client.Core;
using Slacek.Client.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Slacek.Client.Desktop
{
    public class DataManager
    {
        private readonly ConnectionService _connectionService;

        public User LoggedInUser => _connectionService.AuthenticatedUser;

        public bool IsUserAuthenticated => _connectionService.IsUserAuthenticated;

        public ObservableCollection<User> Users { get; }

        public ObservableCollection<Group> Groups { get; }

        public Dictionary<int, ObservableCollection<Message>> Messages { get; }

        public DataManager(ConnectionService connectionService)
        {
            _connectionService = connectionService;
            Users = new ObservableCollection<User>();
            Groups = new ObservableCollection<Group>();
            Messages = new Dictionary<int, ObservableCollection<Message>>();
            _connectionService.GetGroupsReceived += ConnectionManager_GetGroupsReceived;
            _connectionService.GetUsersReceived += ConnectionManager_GetUsersReceived;
            _connectionService.GetMessagesReceived += ConnectionManager_GetMessagesReceived;
            _connectionService.NewMessageReceived += ConnectionManager_NewMessageReceived;
            _connectionService.NewUserInGroupReceived += ConnectionService_NewUserInGroupReceived;
            _connectionService.NewGroupReceived += ConnectionService_NewGroupReceived;
        }

        private void ConnectionService_NewGroupReceived(object sender, NewGroupReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                Groups.Add(e.Group);
                Messages[e.Group.GroupId] = new ObservableCollection<Message>();
            });
        }

        private void ConnectionManager_GetGroupsReceived(object sender, GetGroupsReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                Groups.Clear();
                if (e.Successful)
                {
                    foreach (Group group in e.Groups)
                    {
                        Groups.Add(group);
                        if (!Messages.ContainsKey(group.GroupId))
                        {
                            Messages.Add(group.GroupId, new ObservableCollection<Message>());
                        }
                    }
                }
            });
        }

        private void ConnectionManager_GetUsersReceived(object sender, GetUsersReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                Users.Clear();
                if (e.Successful)
                {
                    foreach (User user in e.Users)
                    {
                        Users.Add(user);
                    }
                }
            });
        }

        private void ConnectionManager_GetMessagesReceived(object sender, GetMessagesReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                int groupId = e.GroupId;
                Messages[groupId].Clear();
                if (e.Successful)
                {
                    foreach (Message message in e.Messages)
                    {
                        Messages[groupId].Add(message);
                        message.User = Users.FirstOrDefault(u => u.UserId == message.UserId);
                    }
                }
            });
        }

        private void ConnectionManager_NewMessageReceived(object sender, NewMessageReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                Message message = e.Message;
                int groupId = message.GroupId;
                ObservableCollection<Message> messages = Messages.GetValueOrDefault(groupId);
                if (messages is null)
                {
                    return;
                }
                message.User = Users.FirstOrDefault(u => u.UserId == message.UserId);
                messages.Add(message);
            });
        }

        private void ConnectionService_NewUserInGroupReceived(object sender, NewUserInGroupReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                int groupId = e.GroupId;
                User user = e.User;
                if(Groups.Any(g => g.GroupId == groupId))
                {
                    Users.Add(user);
                }
            });
        }

        public bool SendNewMessage(Group group, string content)
        {
            Message message = new Message()
            {
                GroupId = group.GroupId,
                Group = group,
                UserId = LoggedInUser.UserId,
                User = LoggedInUser,
                Content = content
            };
            bool sent = _connectionService.SendNewMessage(message);
            if (sent)
            {
                Messages[group.GroupId].Add(message);
            }
            return sent;
        }

        public void Logout()
        {
            _connectionService.Unauthenticate();
            Users.Clear();
            Groups.Clear();
            Messages.Clear();
        }

        public void GetGroups()
        {
            _connectionService.GetGroups();
        }

        public void GetUsers(int groupId)
        {
            _connectionService.GetUsers(groupId);
        }

        public void GetMessages(int groupId)
        {
            _connectionService.GetMessages(groupId);
        }
    }
}
