using System;
using System.Security.Cryptography;
using System.Text;

namespace Slacek.Cryptography
{
    public class AsymmetricCryptographyService : ICryptographyService
    {
        private readonly string _privateKey;
        private readonly string _publicKey;
        private readonly Encoding _encoder = new UTF8Encoding();

        public string PublicKey => _publicKey;

        public byte[] PublicKeyBytes => _encoder.GetBytes(PublicKey);

        public AsymmetricCryptographyService()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            _privateKey = rsa.ToXmlString(true);
            _publicKey = rsa.ToXmlString(false);
        }

        public AsymmetricCryptographyService(string publicKey)
        {
            try
            {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);
            }
            catch (CryptographicException)
            {
                throw;
            }
            _publicKey = publicKey;
            _privateKey = null;
        }

        public AsymmetricCryptographyService(byte[] publicKeyBytes)
        {
            string publicKey = _encoder.GetString(publicKeyBytes);
            try
            {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);
            }
            catch (CryptographicException)
            {
                throw;
            }
            _publicKey = publicKey;
            _privateKey = null;
        }

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

            if (_privateKey is null)
            {
                throw new NotSupportedException("Asymmetric cryptography from imported public key does not support data decryption.");
            }
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(_privateKey);
            return rsa.Decrypt(data, false);
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

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(_publicKey);
            return rsa.Encrypt(data, false);
        }

        public byte[] Encrypt(string data) => Encrypt(StringToBytes(data));

        public string EncryptToBase64String(byte[] data) => BytesToBase64String(Encrypt(data));

        public string EncryptToBase64String(string data) => BytesToBase64String(Encrypt(StringToBytes(data)));
    }
}
