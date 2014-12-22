using System;
using Emgu.CV;

namespace FaceFinderDemo.ImageProcessing
{
    public abstract class ImageProcessor
    {
        public EventHandler<ImageAvailableEventArgs> ImageAvailable;
        ImageProcessor source;

        public void AttachSource(ImageProcessor source)
        {
            DetachSource();
            this.source = source;
            source.ImageAvailable += ImageAvailableFromSource;
        }

        public void DetachSource()
        {
            if (source == null)
            {
                return;
            }

            source.ImageAvailable -= ImageAvailableFromSource;
        }

        private void ImageAvailableFromSource(object sender, ImageAvailableEventArgs e)
        {
            OnImageReceived(e.Image);
        }

        protected abstract void OnImageReceived(Mat image);

        protected void OnImageAvailable(Mat image)
        {
            EventHandler<ImageAvailableEventArgs> handler = ImageAvailable;
            if (handler != null)
            {
                handler(this, new ImageAvailableEventArgs(image));
            }
        }
    }
}
