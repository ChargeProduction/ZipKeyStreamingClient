using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZipKeyStreamingClient.Interface;
using ZipKeyStreamingClient.Interface.Payload;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace ZipKeyStreamingClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private Client client;
        private CancellationTokenSource  connectCancellation;
        private CameraModel cameraModel = new CameraModel();
        
        public MainWindow()
        {
            InitializeComponent();
            client = new Client();
            connectCancellation = new CancellationTokenSource();
            client.OnImageReceived += OnImageReceived;
            client.Connect(IPAddress.Parse("127.0.0.1"), 24456, connectCancellation.Token);
        }

        private void OnImageReceived(Client sender, Bitmap image)
        {
            Dispatcher.BeginInvoke((Action) (() =>
            {
                if (cameraModel.Source == null || cameraModel.Source.Width != image.Width || cameraModel.Source.Height != image.Height)
                {
                    cameraModel.Source = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr24, null);
                }

                var data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                cameraModel.Source.Lock();

                CopyMemory(cameraModel.Source.BackBuffer, data.Scan0, (uint)(cameraModel.Source.BackBufferStride * image.Height));

                cameraModel.Source.AddDirtyRect(new Int32Rect(0, 0, image.Width, image.Height));
                cameraModel.Source.Unlock();

                image.UnlockBits(data);
                image.Dispose();
                CameraSurface.Source = cameraModel.Source;
            }));
        }

        public void SendStart()
        {
            client.Send(new CommandPayload() {Command = "START"});
        }

        public void SendStop()
        {
            client.Send(new CommandPayload() {Command = "STOP"});
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            connectCancellation.Cancel();
        }

        public CameraModel CameraModel => cameraModel;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            SendStart();
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            SendStop();
        }
    }
}