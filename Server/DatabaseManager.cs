using Microsoft.EntityFrameworkCore;
using Slacek.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slacek.Server
{
    public class DatabaseManager
    {
        private readonly DatabaseContext _databaseContext;

        public DatabaseManager(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
            if (!_databaseContext.Groups.Any())
            {
                _databaseContext.Add(new Group { Name = "Ogólny" });
                _databaseContext.SaveChanges();
            }
        }

        public User RegisterNewUser(string login, string username, string password)
        {
            if (_databaseContext.Users.Any(user => user.Login == login))
            {
                return null;
            }
            Group group = _databaseContext.Groups.First();
            User user = new User { Login = login, Username = username, Password = password, Groups = new List<Group> { group } };
            _databaseContext.Add(user);
            _databaseContext.SaveChanges();
            return _databaseContext.Users.Include(u => u.Groups).Single(u => u.Login == login);
        }

        public Group CreateNewGroup(int userId, string name)
        {
            User user = _databaseContext.Users.Single(u => u.UserId == userId);
            Group group = new Group { Name = name, Users = new List<User> { user } };
            group = _databaseContext.Add(group).Entity;
            _databaseContext.SaveChanges();
            return group;
        }

        public ICollection<Group> GetUserGroups(int userId)
        {
            try
            {
                return _databaseContext.Users.Include(u => u.Groups).Single(u => u.UserId == userId).Groups;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ICollection<User> GetGroupUsers(int groupId)
        {
            try
            {
                return _databaseContext.Groups.Include(g => g.Users).Single(g => g.GroupId == groupId).Users;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ICollection<Message> GetGroupMessages(int groupId)
        {
            try
            {
                return _databaseContext.Groups.Include(g => g.Messages).Single(g => g.GroupId == groupId).Messages;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Group JoinGroup(int userId, string groupName)
        {
            try
            {
                User user = _databaseContext.Users.Single(u => u.UserId == userId);
                Group group = _databaseContext.Groups.First(g => g.Name == groupName);
                group.Users.Add(user);
                _databaseContext.SaveChanges();
                return group;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public User AuthenticateUser(string login, string password)
        {
            try
            {
                return _databaseContext.Users.Include(u => u.Groups).Single(user => user.Login == login && user.Password == password);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public Message NewMessage(int userId, int groupId, string content)
        {
            try
            {
                User user = _databaseContext.Users.Single(u => u.UserId == userId);
                Group group = _databaseContext.Groups.Single(g => g.GroupId == groupId);
                Message message = _databaseContext.Add(new Message { User = user, UserId = userId, Group = group, GroupId = groupId, Content = content }).Entity;
                _databaseContext.SaveChanges();
                return message;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
