namespace Slacek.Client.Core.Serializers
{
    internal interface ISerializer<T>
    {
        public string Serialize(T item);

        public T Deserialize(string serialized);
    }
}
