using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using ZeroFormatter;
using ZipKeyStreamingClient.Interface.Payload;

namespace ZipKeyStreamingClient.Interface
{
    public class Client : IDisposable
    {
        public delegate void OnImageReceivedDelegate(Client sender, Bitmap image);
        
        private readonly byte[] headerBuffer = new byte[8];
        private readonly object sendLock = new object();

        private TcpClient socket;
        private bool isDisposed;

        public event OnImageReceivedDelegate OnImageReceived;
        
        public async void Connect(IPAddress address, int port, CancellationToken cancel)
        {
            if (!cancel.IsCancellationRequested)
            {
                socket = socket ?? new TcpClient();
                try
                {
                    socket.Connect(address, port);
                    Console.WriteLine("Connected to server {0}:{1}", address, port);
                    new Thread(ListenerThread).Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to connect to server, retrying in 5 seconds.");
                    await Task.Delay(5000);
                    Connect(address, port, cancel);
                }
            }
        }

        private void ListenerThread()
        {
            while (!isDisposed)
            {
                try
                {
                    if (socket.Available > 0)
                    {
                        ReceiveBuffer(headerBuffer);
                        var code = BitConverter.ToInt32(headerBuffer, 0);
                        var length = BitConverter.ToInt32(headerBuffer, 4);

                        var payload = new byte[length];
                        ReceiveBuffer(payload);
                        ProcessMessage(code, payload);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Dispose();
                }
            }

            if (socket.Connected)
            {
                socket.Dispose();
            }
        }

        private void ReceiveBuffer(byte[] buffer)
        {
            var stream = socket.GetStream();
            int totalBytesRead = 0;
            int bytesRead;
            while (totalBytesRead < buffer.Length)
            {
                bytesRead = stream.Read(buffer, totalBytesRead, buffer.Length - totalBytesRead);
                totalBytesRead += bytesRead;
            }
        }

        private void ProcessMessage(int code, byte[] buffer)
        {
            switch (PayloadId.GetType(code)?.Name)
            {
                case nameof(CameraDataPayload):
                    var cameraData = ZeroFormatterSerializer.Deserialize<CameraDataPayload>(buffer);
                    ProcessData(cameraData);
                    break;
            }
        }

        private void ProcessData(CameraDataPayload payload)
        {
            using (var memStream = new MemoryStream(payload.JpegData))
            {
                var cameraImage = new Bitmap(memStream);
                OnImageReceived?.Invoke(this, cameraImage);
            }
        }

        public void Send<T>(T obj)
        {
            int code = PayloadId.GetId(obj.GetType());
            byte[] payload = ZeroFormatterSerializer.Serialize(obj);
            lock (sendLock)
            {
                try
                {
                    socket.GetStream().Write(BitConverter.GetBytes(code), 0, 4);
                    socket.GetStream().Write(BitConverter.GetBytes(payload.Length), 0, 4);
                    socket.GetStream().Write(payload, 0, payload.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to send packet to client: {0}", e);
                }
            }
        }

        public void Dispose()
        {
            this.isDisposed = true;
        }
    }
}