using Slacek.Server.Models;
using System;
using System.IO;

namespace Slacek.Server.Serializers
{
    internal class GroupSerializer : ISerializer<Group>
    {
        public string Serialize(Group item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(item.GroupId);
            writer.Write(item.Name);
            byte[] bytes = stream.ToArray();
            return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
        }

        public Group Deserialize(string serialized)
        {
            if (serialized is null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }
            byte[] bytes = Convert.FromBase64String(serialized);
            using MemoryStream stream = new MemoryStream(bytes, false);
            using BinaryReader reader = new BinaryReader(stream);
            return new Group()
            {
                GroupId = reader.ReadInt32(),
                Name = reader.ReadString()
            };
        }
    }
}
