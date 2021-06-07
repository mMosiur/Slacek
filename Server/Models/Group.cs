using System.Collections.Generic;

namespace Slacek.Server.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
