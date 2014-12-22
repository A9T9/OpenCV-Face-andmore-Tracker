using System;
using Emgu.CV;
using FaceFinderDemo.ImageProcessing;

namespace FaceFinderDemo.Camera
{
    public class CameraDevice : ImageProcessor, IDisposable
    {
        public bool IsCapturing { get { return isCapturing; }}

        Mat capturedImage;
        Capture camera;
        bool disposed;
        int frameRate;
        int frameCount;
        bool isCapturing;

        public CameraDevice()
        {
            capturedImage = new Mat();
        }

        public void StartCamera(int cameraIndex)
        {
            if (isCapturing)
            {
                return;
            }
            CvInvoke.UseOpenCL = false;
            camera = new Capture(cameraIndex);
            camera.ImageGrabbed += CapOnImageGrabbed;
            camera.Start();
            isCapturing = true;
        }

        public void StopCamera()
        {
            if (!isCapturing)
            {
                return;
            }

            camera.ImageGrabbed -= CapOnImageGrabbed;
            camera.Stop();
            camera.Dispose();
            isCapturing = false;
        }

        private void CapOnImageGrabbed(object sender, EventArgs e)
        {
            frameCount++;
            //Debug.WriteLine("Frames: " + frameCount);
            camera.Retrieve(capturedImage);
            OnImageAvailable(capturedImage);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            capturedImage.Dispose();

            if (isCapturing)
            {
                StopCamera();
            }

            disposed = true;
        }

        protected override void OnImageReceived(Mat image)
        {
            
        }
    }
}
