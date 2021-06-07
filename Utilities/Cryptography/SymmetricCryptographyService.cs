using System;
using System.Security.Cryptography;
using System.Text;

namespace Slacek.Cryptography
{
    public class SymmetricCryptographyService : ICryptographyService
    {
        private readonly Aes _aes;

        private readonly Encoding _encoder = new UTF8Encoding();

        public SymmetricCryptographyService()
        {
            _aes = Aes.Create();
        }

        public SymmetricCryptographyService(byte[] iv, byte[] key) : this()
        {
            if (!_aes.ValidKeySize(key.Length * 8))
            {
                throw new CryptographicException("Unsupported key size");
            }
            _aes.BlockSize = iv.Length * 8;
            _aes.KeySize = key.Length * 8;
            _aes.IV = iv;
            _aes.Key = key;
        }

        public SymmetricCryptographyService(string iv, string key)
            : this(Convert.FromBase64String(iv), Convert.FromBase64String(key)) { }

        public string IV => Convert.ToBase64String(IVBytes);

        public byte[] IVBytes => _aes.IV;

        public string Key => Convert.ToBase64String(KeyBytes);

        public byte[] KeyBytes => _aes.Key;

        private byte[] StringToBytes(string value)
        {
            return _encoder.GetBytes(value);
        }

        private static byte[] Base64StringToBytes(string value)
        {
            return Convert.FromBase64String(value);
        }

        private string BytesToString(byte[] bytes)
        {
            return _encoder.GetString(bytes);
        }

        private static string BytesToBase64String(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public byte[] Decrypt(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ICryptoTransform decryptor = _aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public byte[] Decrypt(string data) => Decrypt(Base64StringToBytes(data));

        public string DecryptToString(byte[] data) => BytesToString(Decrypt(data));

        public string DecryptToString(string data) => BytesToString(Decrypt(Base64StringToBytes(data)));

        public byte[] Encrypt(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using ICryptoTransform encryptor = _aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public byte[] Encrypt(string data) => Encrypt(StringToBytes(data));

        public string EncryptToBase64String(byte[] data) => BytesToBase64String(Encrypt(data));

        public string EncryptToBase64String(string data) => BytesToBase64String(Encrypt(StringToBytes(data)));
    }
}
