using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace FaceFinderDemo.FaceDetection
{
    public class FaceFeatures
    {
        public FaceFeatures(Rectangle face, int frameWidth, int frameHeight)
        {
            this.FaceLocation = face;

            CalcProbableEyeLocation(frameWidth, frameHeight);
            CalcProbableNoseLocation(frameWidth, frameHeight);
            CalcProbableMouthLocation(frameWidth, frameHeight);
        }

        private void CalcProbableEyeLocation(int width, int height)
        {
            var probableEyeLocation = FaceLocation;
            int originalHeight = probableEyeLocation.Height;
            probableEyeLocation.Height = (int) (probableEyeLocation.Height / 2.7f); //only search a part of the face

            //shif the frame a little bit down.
            int shiftOverY = (int) ((originalHeight / 1.7) - probableEyeLocation.Height);

            //now shift the window a little bit down
            probableEyeLocation.Y += shiftOverY;
            ProbableEyeLocation = probableEyeLocation;

            ProbableEyeLocation = FixBoundings(probableEyeLocation, width, height);
        }

        private void CalcProbableMouthLocation(int width, int height)
        {
            //these values are based on tests
            var probableMouthLocation = FaceLocation;
            probableMouthLocation.Width /= 2;
            probableMouthLocation.Height /= 3;

            //shif the frame a little bit down.
            int shiftOverX = ((FaceLocation.Width - probableMouthLocation.Width) / 2);
            int shiftOverY = probableMouthLocation.Height * 2;

            //now shift the window a little bit down
            probableMouthLocation.X += shiftOverX;
            probableMouthLocation.Y += shiftOverY;

            ProbableMouthLocation = FixBoundings(probableMouthLocation, width, height);
        }

        private void CalcProbableNoseLocation(int width, int height)
        {
            Rectangle probableNoseLocation = FaceLocation;

            //these values are based on tests
            probableNoseLocation.Width = (int)(0.43 * probableNoseLocation.Width);
            probableNoseLocation.Height = (int)(0.43 * probableNoseLocation.Height);

            //shif the frame a little bit down.
            int shiftOverX = ((FaceLocation.Width - probableNoseLocation.Width) / 2);
            int shiftOverY = ((FaceLocation.Height - probableNoseLocation.Height) / 2);
            probableNoseLocation.X += shiftOverX;
            probableNoseLocation.Y += shiftOverY;
            ProbableNoseLocation = probableNoseLocation;

            ProbableNoseLocation = FixBoundings(probableNoseLocation, width, height);
        }

        Rectangle FixBoundings(Rectangle rect, int width, int height)
        {
            if (rect.Left < 0)
            {
                rect.X = 0;
            }

            if (rect.Top < 0)
            {
                rect.Y = 0;
            }

            if (rect.Bottom > height)
            {
                rect.Height = rect.Height - (rect.Bottom - height);
            }

            if (rect.Right > width)
            {
                rect.Width = rect.Width - (rect.Right - width);
            }

            return rect;
        }

        public bool IsValid
        {
            get { return !LeftEyeLocation.IsEmpty && !RightEyeLocation.IsEmpty && !NoseLocation.IsEmpty && !MouthLocation.IsEmpty; }
        }

        public Rectangle ProbableEyeLocation { get; private set; }
        public Rectangle ProbableNoseLocation { get; private set; }
        public Rectangle ProbableMouthLocation { get; private set; }

        public Rectangle FaceLocation { get; private set; }
        public Rectangle LeftEyeLocation { get; private set; }
        public Rectangle RightEyeLocation { get; private set; }
        public Rectangle NoseLocation { get; private set; }
        public Rectangle MouthLocation { get; private set; }

        public void AddEyes(Rectangle[] eyes)
        {
            if (eyes.Length > 0)
            {
                Rectangle eyeRect = eyes[0];
                eyeRect.Offset(ProbableEyeLocation.X, ProbableEyeLocation.Y);
                LeftEyeLocation = eyeRect;
            }

            if (eyes.Length > 1)
            {
                Rectangle eyeRect = eyes[1];
                eyeRect.Offset(ProbableEyeLocation.X, ProbableEyeLocation.Y);
                RightEyeLocation = eyeRect;
            }
        }

        public void AddNose(Rectangle[] nose)
        {
            if (nose.Length > 0)
            {
                Rectangle noseRect = nose[0];
                noseRect.Offset(ProbableNoseLocation.X, ProbableNoseLocation.Y);
                NoseLocation = noseRect;
            }
        }

        public void AddMouth(Rectangle[] mouth)
        {
            if (mouth.Length > 0)
            {
                Rectangle mouthRect = mouth[0];
                mouthRect.Offset(ProbableMouthLocation.X, ProbableMouthLocation.Y);
                MouthLocation = mouthRect;
            }
        }

        public void DrawToImage(Mat image, bool includeInterestAreas)
        {
            int thickness = 2;
            CvInvoke.Rectangle(image, FaceLocation, new Bgr(Color.Red).MCvScalar, thickness);

            if(!LeftEyeLocation.IsEmpty)
                CvInvoke.Rectangle(image, LeftEyeLocation, new Bgr(Color.Yellow).MCvScalar, thickness);

            if (!RightEyeLocation.IsEmpty)
                CvInvoke.Rectangle(image, RightEyeLocation, new Bgr(Color.Yellow).MCvScalar, thickness);

            if (!NoseLocation.IsEmpty)
                CvInvoke.Rectangle(image, NoseLocation, new Bgr(Color.Green).MCvScalar, thickness);

            if (!MouthLocation.IsEmpty)
                CvInvoke.Rectangle(image, MouthLocation, new Bgr(Color.Blue).MCvScalar, thickness);

            if (includeInterestAreas)
            {
                CvInvoke.Rectangle(image, ProbableEyeLocation, new Bgr(Color.Magenta).MCvScalar, thickness);
                CvInvoke.Rectangle(image, ProbableNoseLocation, new Bgr(Color.Magenta).MCvScalar, thickness);
                CvInvoke.Rectangle(image, ProbableMouthLocation, new Bgr(Color.Magenta).MCvScalar, thickness);    
            }
        }
    }
}
