using Slacek.Server.Models;
using System;
using System.Collections.Generic;

namespace Slacek.Server.Serializers
{
    internal static class SerializerProvider
    {
        private static readonly Dictionary<Type, object> _serializers;

        static SerializerProvider()
        {
            _serializers = new Dictionary<Type, object>();
        }

        public static ISerializer<T> Get<T>()
        {
            if (_serializers.ContainsKey(typeof(T)))
            {
                return _serializers[typeof(T)] as ISerializer<T>;
            }
            if (typeof(T) == typeof(User))
            {
                ISerializer<User> serializer = new UserSerializer();
                _serializers.Add(typeof(T), serializer);
                return serializer as ISerializer<T>;
            }
            if (typeof(T) == typeof(Group))
            {
                ISerializer<Group> serializer = new GroupSerializer();
                _serializers.Add(typeof(T), serializer);
                return serializer as ISerializer<T>;
            }
            if (typeof(T) == typeof(Message))
            {
                ISerializer<Message> serializer = new MessageSerializer();
                _serializers.Add(typeof(T), serializer);
                return serializer as ISerializer<T>;
            }
            throw new Exception($"Serializer of type `{typeof(T)}` was not found");
        }
    }
}
