using System;
using Emgu.CV;

namespace FaceFinderDemo.ImageProcessing
{
    public class ImageAvailableEventArgs : EventArgs
    {
        public Mat Image { get; private set; }

        public ImageAvailableEventArgs(Mat image)
        {
            Image = image;
        }
    }
}
