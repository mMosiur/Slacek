using System.Collections.Generic;

namespace Slacek.Server.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public ICollection<Group> Groups { get; set; }
    }
}
