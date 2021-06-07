namespace Slacek.Cryptography
{
    public interface ICryptographyService
    {
        public byte[] Decrypt(byte[] data);

        public byte[] Decrypt(string data);

        public string DecryptToString(byte[] data);

        public string DecryptToString(string data);

        public byte[] Encrypt(byte[] data);

        public byte[] Encrypt(string data);

        public string EncryptToBase64String(byte[] data);

        public string EncryptToBase64String(string data);
    }
}
