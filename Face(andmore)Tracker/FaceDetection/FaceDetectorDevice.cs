using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Timers;
using Emgu.CV;
using FaceFinderDemo.ImageProcessing;

namespace FaceFinderDemo.FaceDetection
{
    public class FaceDetectorDevice : ImageProcessor, IDisposable
    {

        /// <summary>
        /// Determines when detection should occur: Disabled means just passthrough the image (with past detection rectangles), 
        /// Periodic will detect faces on every nth image, AllFrames detect faces on all frames received from source, Manual won't
        /// doing automatic detecting, detection can be started with ManualDetect() mthod.
        /// </summary>
        public DetectionModes DetectionMode
        {
            get { return detectionMode; }
            set
            {
                detectionMode = value;
                DetectionPeriod = detectionPeriod;
            }
        }

        /// <summary>
        /// If detection is Periodic, the minimal time which will be elapsed until the next detection, when the next frame received from the source.
        /// </summary>
        public TimeSpan DetectionPeriod
        {
            get { return detectionPeriod; }
            set
            {
                detectionPeriod = value;
                if (detectionMode == DetectionModes.Periodic)
                {
                    detectionNotifyTimer.Interval = detectionPeriod.TotalMilliseconds;
                    detectionNotifyTimer.Start();
                }
                else
                {
                    detectionNotifyTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Draw bounding boxes of detection
        /// </summary>
        public bool DrawDetection { get; set; }

        /// <summary>
        /// Draw scanned area of face features detection
        /// </summary>
        public bool DrawProbableAreas { get; set; }

        /// <summary>
        /// Get notified about detection states: started and ended with detection result
        /// </summary>
        public EventHandler<FaceDetectionEventArgs> FaceDetectorStateChanged;

        public enum DetectionModes { Disabled, Periodic, AllFrames, Manual }

        bool disposed = false;
        CascadeClassifier faceClassifier;
        CascadeClassifier eyeClassifier;
        CascadeClassifier mouthClassifier;
        CascadeClassifier noseClassifier;

        List<FaceFeatures> lastDetectedFaces = new List<FaceFeatures>();
        bool detectingInProgress;
        bool detectNextFrame;
        DetectionModes detectionMode;
        Timer detectionNotifyTimer;
        TimeSpan detectionPeriod;

        public FaceDetectorDevice()
        {
            //Read the HaarCascade objects
            CvInvoke.UseOpenCL = false;
            faceClassifier = new CascadeClassifier(@"Resources\haarcascades\frontalface_alt.xml");
            eyeClassifier = new CascadeClassifier(@"Resources\haarcascades\eye.xml");
            mouthClassifier = new CascadeClassifier(@"Resources\haarcascades\mouth.xml");
            noseClassifier = new CascadeClassifier(@"Resources\haarcascades\nose.xml");
            detectionNotifyTimer = new Timer();
            detectionNotifyTimer.Elapsed += DetectionNotifyTimerOnElapsed;
            DetectionMode = DetectionModes.Disabled;
            DetectionPeriod = TimeSpan.FromMilliseconds(500);
        }

        void DetectionNotifyTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            detectNextFrame = true;
        }

        public void ManualDetect()
        {
            detectNextFrame = true;
        }

        protected override void OnImageReceived(Mat image)
        {
            
            if (DetectionMode == DetectionModes.AllFrames)
            {
                DetectFaces(image);
            }
            else if (DetectionMode == DetectionModes.Periodic || DetectionMode == DetectionModes.Manual)
            {
                // Do it asynchronously, don't interrupt the stream of frames, the detection result will be drawn to next frame
                if (detectNextFrame && !detectingInProgress)
                {
                    detectNextFrame = false;
                    // If we don't clone the imeage, the drawing operation will modify it and face cannot be detected on that image
                    Mat imgCopy = image.Clone();
                    Task.Factory.StartNew(() => DetectFaces(imgCopy));
                }
            }
            else if (DetectionMode == DetectionModes.Disabled)
            {
                lastDetectedFaces.Clear();
            }

            if (DrawDetection)
            {
                foreach (FaceFeatures f in lastDetectedFaces)
                    f.DrawToImage(image, DrawProbableAreas);    
            }

            //CvInvoke.PutText(image, "Detection took " + detectionTime + " ms", new Point(10, 30), FontFace.HersheyComplex, 1.0, new Bgr(Color.Black).MCvScalar, 1, LineType.AntiAlias);
            OnImageAvailable(image);
        }

        private void DetectFaces(Mat image)
        {
            OnFaceDetectionStateChanged(true);
            detectingInProgress = true;
            List<FaceFeatures> detectedFaces = new List<FaceFeatures>();

            //Many opencl functions require opencl compatible gpu devices. 
            //As of opencv 3.0-alpha, opencv will crash if opencl is enable and only opencv compatible cpu device is presented
            //So we need to call CvInvoke.HaveOpenCLCompatibleGpuDevice instead of CvInvoke.HaveOpenCL (which also returns true on a system that only have cpu opencl devices).
            //CvInvoke.UseOpenCL = TryUseOpenCL && CvInvoke.HaveOpenCLCompatibleGpuDevice;

            var watch = Stopwatch.StartNew();
            using (var grayImage = new UMat())
            {
                // Convert image to grayscale
                CvInvoke.CvtColor(image, grayImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                //normalizes brightness and increases contrast of the image
                CvInvoke.EqualizeHist(grayImage, grayImage);

                //Detect the faces  from the gray scale image and store the locations as rectangle in FaceFeature each

                Rectangle[] facesDetected = faceClassifier.DetectMultiScale(grayImage, 1.1 /*the the scale factor opencv uses to increase the window each pass, default 1.1*/,
                    3 /*minNeighbors, default: 3 (the min. number of rects to group together to call it a face)*/,
                    new Size(40, 40) /*min rect check size */);

                foreach (Rectangle facerect in facesDetected)
                {
                    FaceFeatures face = new FaceFeatures(facerect, grayImage.Cols, grayImage.Rows);
                    //Get the region of interest on the faces
                    using (var probableNoseRegion = new UMat(grayImage, face.ProbableNoseLocation))
                    {
                        face.AddNose(noseClassifier.DetectMultiScale(probableNoseRegion, 1.13, 3, new Size(10, 10)));
                    }

                    using (var probableEyesRegion = new UMat(grayImage, face.ProbableEyeLocation))
                    {
                        face.AddEyes(eyeClassifier.DetectMultiScale(probableEyesRegion, 1.13, 3, new Size(10, 10)));
                    }

                    using (var probableMouthRegion = new UMat(grayImage, face.ProbableMouthLocation))
                    {
                        face.AddMouth(mouthClassifier.DetectMultiScale(probableMouthRegion, 1.13, 3, new Size(10, 20)));
                    }

                    detectedFaces.Add(face);
                }
            }

            watch.Stop();
            lastDetectedFaces = detectedFaces;
            OnFaceDetectionStateChanged(false, detectedFaces, (int) watch.ElapsedMilliseconds);
            detectingInProgress = false;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            faceClassifier.Dispose();
            eyeClassifier.Dispose();
            mouthClassifier.Dispose();
            noseClassifier.Dispose();

            disposed = true;
        }

        private void OnFaceDetectionStateChanged(bool starting, List<FaceFeatures> faces = null, int detectionTime = 0)
        {
            EventHandler<FaceDetectionEventArgs> handler = FaceDetectorStateChanged;
            if (handler != null)
            {
                handler(this, new FaceDetectionEventArgs(starting, faces, detectionTime));
            }
        }

        public void ResetDetections()
        {
            lastDetectedFaces.Clear();
        }
    }
}
