using Slacek.Server.Models;
using System;
using System.IO;

namespace Slacek.Server.Serializers
{
    internal class MessageSerializer : ISerializer<Message>
    {
        public string Serialize(Message item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(item.GroupId);
            writer.Write(item.UserId);
            writer.Write(item.Content);
            byte[] bytes = stream.ToArray();
            return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
        }

        public Message Deserialize(string serialized)
        {
            if (serialized is null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }
            byte[] bytes = Convert.FromBase64String(serialized);
            using MemoryStream stream = new MemoryStream(bytes, false);
            using BinaryReader reader = new BinaryReader(stream);
            return new Message()
            {
                GroupId = reader.ReadInt32(),
                UserId = reader.ReadInt32(),
                Content = reader.ReadString()
            };
        }
    }
}
