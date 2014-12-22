using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using FaceFinderDemo.FaceDetection;

namespace FaceFinderDemo
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public List<string> AvailableCameras
        {
            get { return availableCameras; }
            set
            {
                availableCameras = value;
                OnPropertyChanged("AvailableCameras");
            }
        }

        public bool IsCapturing
        {
            get { return isCapturing; }
            set
            {
                isCapturing = value;
                UpdateState();
                OnPropertyChanged("IsCapturing");
            }
        }

        public void UpdateState()
        {
            ManualDetectionEnabled = SelectedDetectionMode == FaceDetectorDevice.DetectionModes.Manual && IsCapturing;
            PeriodDetectionEnabled = SelectedDetectionMode == FaceDetectorDevice.DetectionModes.Periodic;
        }

        public Array AvailableDetectionModes
        {
            get { return Enum.GetValues(typeof(FaceDetectorDevice.DetectionModes)); }
        }

        public FaceDetectorDevice.DetectionModes SelectedDetectionMode { get; set; }
        public int DetectionPeriod { get; set; }
        public bool DrawDetection { get; set; }
        public bool DrawProbableAreas { get; set; }

        public bool ManualDetectionEnabled
        {
            get { return manualDetectionEnabled; }
            set
            {
                manualDetectionEnabled = value;
                OnPropertyChanged("ManualDetectionEnabled");
            }
        }

        public bool PeriodDetectionEnabled
        {
            get { return periodDetectionEnabled; }
            set
            {
                periodDetectionEnabled = value;
                OnPropertyChanged("PeriodDetectionEnabled");
            }
        }

        public bool CurrentlyDetecting
        {
            get { return currentlyDetecting; }
            set
            {
                currentlyDetecting = value;
                OnPropertyChanged("CurrentlyDetecting");
            }
        }

        public string LastDetection
        {
            get { return lastDetection; }
            set
            {
                lastDetection = value;
                OnPropertyChanged("LastDetection");
            }
        }

        public string ImagePath
        {
            get { return imagePath; }
            set
            {
                imagePath = value;
                OnPropertyChanged("ImagePath");
            }
        }
        
        List<string> availableCameras;
        bool isCapturing;
        bool manualDetectionEnabled;
        bool periodDetectionEnabled;
        bool currentlyDetecting;
        string lastDetection;
        bool isImageLoaded;
        string imagePath;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
