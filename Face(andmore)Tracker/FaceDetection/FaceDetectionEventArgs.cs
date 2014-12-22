using System;
using System.Collections.Generic;

namespace FaceFinderDemo.FaceDetection
{
    public class FaceDetectionEventArgs : EventArgs
    {
        public bool Starting { get; private set; }
        public List<FaceFeatures> Faces { get; private set; }
        public int DetectionTime { get; private set; }

        public FaceDetectionEventArgs(bool starting, List<FaceFeatures> faces, int detectionTime)
        {
            Starting = starting;
            Faces = faces;
            DetectionTime = detectionTime;
        }
    }
}
