using Slacek.Cryptography;
using System;
using System.Net.Sockets;

namespace Slacek.Server
{
    public class CommunicationTunnel : IDisposable
    {
        private const string HelloMessage = "hello";

        private readonly Socket _socket;

        private bool _isOpen = false;

        private ICryptographyService cryptographyService;

        public CommunicationTunnel(Socket socket)
        {
            _socket = socket;
        }

        public bool IsSocketLive
        {
            get
            {
                bool part1 = _socket.Poll(10000, SelectMode.SelectRead);
                bool part2 = _socket.Available == 0;
                return !(part1 && part2);
            }
        }

        public bool IsOpen => _isOpen;

        public bool Available => IsOpen && _socket.Available > 0;

        public bool Open()
        {
            if (!IsSocketLive)
            {
                return false;
            }
            if (_isOpen)
            {
                return true;
            }
            AsymmetricCryptographyService acs = new();
            byte[] publicKeyBytes = acs.PublicKeyBytes;
            _socket.Send(BitConverter.GetBytes(publicKeyBytes.Length));
            _socket.Send(publicKeyBytes);
            byte[] lengthBuffer = new byte[sizeof(int)];
            int received = _socket.Receive(lengthBuffer);
            if (received != sizeof(int))
            {
                return false;
            }
            int length = BitConverter.ToInt32(lengthBuffer);
            byte[] helloBuffer = new byte[length];
            received = _socket.Receive(helloBuffer);
            if (received != length)
            {
                return false;
            }
            string decryptedHello = acs.DecryptToString(helloBuffer);
            if (decryptedHello != HelloMessage)
            {
                _socket.Close();
                _isOpen = false;
                return false;
            }
            received = _socket.Receive(lengthBuffer);
            if (received != sizeof(int))
            {
                return false;
            }
            length = BitConverter.ToInt32(lengthBuffer);
            byte[] ivBuffer = new byte[length];
            received = _socket.Receive(ivBuffer);
            if (received != length)
            {
                return false;
            }
            ivBuffer = acs.Decrypt(ivBuffer);
            received = _socket.Receive(lengthBuffer);
            if (received != sizeof(int))
            {
                return false;
            }
            length = BitConverter.ToInt32(lengthBuffer);
            byte[] keyBuffer = new byte[length];
            received = _socket.Receive(keyBuffer);
            if (received != length)
            {
                return false;
            }
            keyBuffer = acs.Decrypt(keyBuffer);
            cryptographyService = new SymmetricCryptographyService(ivBuffer, keyBuffer);
            byte[] data = cryptographyService.Encrypt(HelloMessage);
            _socket.Send(BitConverter.GetBytes(data.Length));
            _socket.Send(data);
            _isOpen = true;
            return true;
        }

        public bool Send(string message)
        {
            byte[] data = cryptographyService.Encrypt(message);
            return SendEncrypted(data);
        }

        public bool SendBytes(byte[] bytes)
        {
            byte[] data = cryptographyService.Encrypt(bytes);
            return SendEncrypted(data);
        }

        private bool SendEncrypted(byte[] data)
        {
            if (!IsOpen)
            {
                return false;
            }
            _socket.Send(BitConverter.GetBytes(data.Length));
            _socket.Send(data);
            return true;
        }

        public string Receive()
        {
            byte[] data = ReceiveEncrypted();
            return cryptographyService.DecryptToString(data);
        }

        public byte[] ReceiveBytes()
        {
            byte[] data = ReceiveEncrypted();
            return cryptographyService.Decrypt(data);
        }

        private byte[] ReceiveEncrypted()
        {
            if (!Available)
            {
                return null;
            }
            byte[] lengthBuffer = new byte[sizeof(int)];
            int received = _socket.Receive(lengthBuffer);
            if (received != sizeof(int))
            {
                throw new Exception($"Received {received} bytes instead of two");
            }
            int length = BitConverter.ToInt32(lengthBuffer);
            byte[] buffer = new byte[length];
            received = _socket.Receive(buffer);
            if (received != length)
            {
                throw new Exception($"Received {received} bytes instead of {length}");
            }
            return buffer;
        }

        public void Dispose()
        {
            _socket.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
