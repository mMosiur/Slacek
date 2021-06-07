using System;
using System.Collections.Generic;
using System.IO;

namespace Slacek.Server.Serializers
{
    internal class CollectionSerializer<T> : ISerializer<ICollection<T>>
    {
        public string Serialize(ICollection<T> item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(item.Count);
            ISerializer<T> serializer = SerializerProvider.Get<T>();
            foreach (T element in item)
            {
                writer.Write(serializer.Serialize(element));
            }
            byte[] bytes = stream.ToArray();
            return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
        }

        public ICollection<T> Deserialize(string serialized)
        {
            if (serialized is null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }
            byte[] bytes = Convert.FromBase64String(serialized);
            using MemoryStream stream = new MemoryStream(bytes, false);
            using BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            ICollection<T> collection = new List<T>(count);
            ISerializer<T> serializer = SerializerProvider.Get<T>();
            for (int i = 0; i < count; ++i)
            {
                string serializedElement = reader.ReadString();
                T element = serializer.Deserialize(serializedElement);
                collection.Add(element);
            }
            return collection;
        }
    }
}
