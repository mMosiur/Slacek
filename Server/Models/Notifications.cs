namespace Slacek.Server
{
    public class NewMessageNotification
    {
        public string SerializedMessage { get; set; }
    }
    public class NewUserNotification
    {
        public int GroupId { get; set; }
        public string SerializedUser { get; set; }
    }
}
