namespace Slacek.Client.Core.Models
{
    public class Message
    {
        public string Content { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
}
