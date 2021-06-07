using Slacek.Cryptography;
using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Slacek.Client.Core
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

        public bool IsOpen => _isOpen;

        public bool Available => IsOpen && _socket.Available > 0;

        public bool Open()
        {
            if (_isOpen)
            {
                return true;
            }
            // Receive the length of the server public key
            byte[] lengthBuffer = new byte[sizeof(int)];
            int received = _socket.Receive(lengthBuffer);
            if (received != sizeof(int))
            {
                return false;
            }
            int length = BitConverter.ToInt32(lengthBuffer);
            // Receive the public key
            byte[] pubKeyBuffer = new byte[length];
            received = _socket.Receive(pubKeyBuffer);
            if (received != length)
            {
                return false;
            }
            // Use public key to encrypt hello message"
            AsymmetricCryptographyService asc = new AsymmetricCryptographyService(pubKeyBuffer);
            byte[] data = asc.Encrypt(HelloMessage);
            // Send hello message
            _socket.Send(BitConverter.GetBytes(data.Length));
            _socket.Send(data);
            // Create symmetric key
            SymmetricCryptographyService scs = new SymmetricCryptographyService();
            // Send IV
            data = asc.Encrypt(scs.IVBytes);
            _socket.Send(BitConverter.GetBytes(data.Length));
            _socket.Send(data);
            // Send Key
            data = asc.Encrypt(scs.KeyBytes);
            _socket.Send(BitConverter.GetBytes(data.Length));
            _socket.Send(data);
            // Receive hello response
            received = _socket.Receive(lengthBuffer);
            if (received != sizeof(int))
            {
                return false;
            }
            length = BitConverter.ToInt32(lengthBuffer);
            byte[] helloBuffer = new byte[length];
            received = _socket.Receive(helloBuffer);
            if (received != length)
            {
                return false;
            }
            string reply = scs.DecryptToString(helloBuffer);
            if (reply != "hello")
            {
                return false;
            }
            cryptographyService = scs;
            _isOpen = true;
            return true;
        }

        public void Wait(int milliseconds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!Available && stopwatch.ElapsedMilliseconds < milliseconds) { }
            return;
        }

        public bool Send(string message)
        {
            if (!IsOpen)
            {
                return false;
            }
            byte[] data = cryptographyService.Encrypt(message);
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

        public byte[] ReceiveEncrypted()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!Available && stopwatch.ElapsedMilliseconds < 2000) { }
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
