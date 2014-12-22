using System;
using System.Timers;
using Emgu.CV;
using Emgu.CV.CvEnum;
using FaceFinderDemo.ImageProcessing;

namespace FaceFinderDemo.Camera
{
    public class ImageDevice : ImageProcessor, IDisposable
    {
        public bool IsSending { get { return isSending; } }

        public int FrameRate { get; set; }

        bool disposed;
        Mat image;
        bool isSending;
        Timer sendTimer;
        object sync = new object();

        public ImageDevice()
        {
            sendTimer = new Timer();
            sendTimer.Interval = 100;
            sendTimer.Elapsed += SendTimerOnElapsed;
        }

        void SendTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (image == null)
                return;

            sendTimer.Stop();
            lock (elapsedEventArgs)
            {
                OnImageAvailable(image.Clone());    
            }
            sendTimer.Start();
        }

        public void StartSending()
        {
            if (isSending)
                return;

            sendTimer.Start();

            isSending = true;
        }

        public void StopSending()
        {
            if (!isSending)
                return;

            sendTimer.Stop();

            isSending = false;
        }

        public bool LoadFromFile(string imageFile)
        {
            lock (sync)
            {
                if (image != null)
                {
                    image.Dispose();
                }
                try
                {
                    var imageLoaded = new Mat(imageFile, LoadImageType.Color);
                    if (imageLoaded.IsEmpty)
                        return false;

                    image = imageLoaded;
                    return true;
                }
                catch
                {
                    return false;
                }    
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (image != null)
            {
                image.Dispose();
            }

            disposed = true;
        }

        protected override void OnImageReceived(Mat image)
        {
            
        }
    }
}
