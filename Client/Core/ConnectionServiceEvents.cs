using Slacek.Client.Core.Models;
using System;
using System.Collections.Generic;

namespace Slacek.Client.Core
{
    public class NewMessageReceivedEventArgs : EventArgs
    {
        public Message Message { get; }

        public NewMessageReceivedEventArgs(Message message) => Message = message;
    }

    public class NewUserInGroupReceivedEventArgs : EventArgs
    {
        public int GroupId { get; }
        public User User { get; }

        public NewUserInGroupReceivedEventArgs(int groupId, User user)
        {
            GroupId = groupId;
            User = user;
        }
    }

    public class UserAuthenticationEventArgs : EventArgs
    {
        public bool Successful => !(User is null);
        public User User { get; }

        public UserAuthenticationEventArgs(User user) => User = user;

        public static UserAuthenticationEventArgs Failed { get; } = new UserAuthenticationEventArgs(null);
    }

    public class UserRegistrationEventArgs : EventArgs
    {
        public bool Successful => !(User is null);
        public User User { get; }

        public UserRegistrationEventArgs(User user) => User = user;

        public static UserRegistrationEventArgs Failed { get; } = new UserRegistrationEventArgs(null);
    }

    public class GetGroupsReceivedEventArgs : EventArgs
    {
        public bool Successful => !(Groups is null);
        public ICollection<Group> Groups { get; }

        public GetGroupsReceivedEventArgs(ICollection<Group> groups) => Groups = groups;

        public static GetGroupsReceivedEventArgs Failed { get; } = new GetGroupsReceivedEventArgs(null);
    }

    public class GetUsersReceivedEventArgs : EventArgs
    {
        public bool Successful => !(Users is null);
        public int GroupId { get; }
        public ICollection<User> Users { get; }

        public GetUsersReceivedEventArgs(int groupId, ICollection<User> users)
        {
            GroupId = groupId;
            Users = users;
        }

        public static GetUsersReceivedEventArgs Failed(int groupId) => new GetUsersReceivedEventArgs(groupId, null);
    }

    public class GetMessagesReceivedEventArgs : EventArgs
    {
        public bool Successful => !(Messages is null);
        public int GroupId { get; }
        public ICollection<Message> Messages { get; }

        public GetMessagesReceivedEventArgs(int groupId, ICollection<Message> messages)
        {
            GroupId = groupId;
            Messages = messages;
        }

        public static GetMessagesReceivedEventArgs Failed(int groupId) => new GetMessagesReceivedEventArgs(groupId, null);
    }

    public class NewGroupReceivedEventArgs : EventArgs
    {
        public Group Group { get; }

        public NewGroupReceivedEventArgs(Group group)
        {
            Group = group;
        }
    }
}
