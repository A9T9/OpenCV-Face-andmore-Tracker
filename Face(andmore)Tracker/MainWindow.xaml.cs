using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceFinderDemo.Camera;
using FaceFinderDemo.FaceDetection;
using FaceFinderDemo.ImageProcessing;
using Microsoft.Win32;
using Point = System.Drawing.Point;


namespace FaceFinderDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FaceDetectorDevice faceDetection;
        MainWindowViewModel model;
        CameraDevice camera;
        ImageDevice image;

        public MainWindow()
        {
            model = new MainWindowViewModel();
            DataContext = model;

            // Connect the image processor chain
            faceDetection = new FaceDetectorDevice();
            camera = new CameraDevice();
            image = new ImageDevice();
            faceDetection.ImageAvailable += ImageAvailable;
            faceDetection.FaceDetectorStateChanged += FaceDetectorStateChanged;
            model.SelectedDetectionMode = FaceDetectorDevice.DetectionModes.Periodic;
            model.DrawDetection = true;
            model.DetectionPeriod = 500;
            model.LastDetection = "None";

            InitializeComponent();
            SizeToContent = SizeToContent.WidthAndHeight;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            model.AvailableCameras = DeviceEnumerator.GetDeviceNames();
            SelectedCamera.SelectedIndex = 0;
        }

        void OnClosed(object sender, EventArgs eventArgs)
        {
            camera.StopCamera();
            image.StopSending();
        }

        void ImageAvailable(object sender, ImageAvailableEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (model.IsCapturing)
                {
                    DetectedImage.Source = EmguWpfHelper.ToBitmapSource(e.Image);    
                }
                else
                {
                    DetectedImage.Source = new BitmapImage(new Uri(@"/Resources/camera_image_placeholder.png", UriKind.Relative));
                }
            }));
        }

        void FaceDetectorStateChanged(object sender, FaceDetectionEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            model.CurrentlyDetecting = e.Starting;
            if (!e.Starting)
            {
                message.AppendFormat("Detection took {0} ms, ", e.DetectionTime);
                if (e.Faces.Count == 0)
                {
                    model.LastDetection = "None";
                    message.AppendFormat("no face found");
                }
                else if (e.Faces.Count == 1)
                {
                    model.LastDetection = e.Faces[0].IsValid ? "Full face" : "Partial face";
                    message.AppendFormat("one {0} face found", e.Faces[0].IsValid ? "full" : "partial");
                }
                else
                {
                    bool isPartial = e.Faces.All((f) => f.IsValid);
                    model.LastDetection = isPartial ? e.Faces.Count + " full faces" : e.Faces.Count + " partial faces";
                    message.AppendFormat("{0} {1} face found", e.Faces.Count, isPartial ? "full" : "partial");
                }
            }
            else
            {
                message.Append("Starting detection");
            }

            LogMessage(message.ToString());
        }

        void LogMessage(string message)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                LogBox.AppendText(message + "\n");
                LogBox.ScrollToEnd();
            }));
        }

        void Cameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (camera.IsCapturing)
            {
                LogMessage("Camera changed");
                camera.StopCamera();
                camera.StartCamera(SelectedCamera.SelectedIndex);
            }
        }

        void StopCapturing_OnClick(object sender, RoutedEventArgs e)
        {
            LogMessage("Capturing stopped");
            model.IsCapturing = false;
            model.ImagePath = "";
            camera.StopCamera();
            image.StopSending();
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                Dispatcher.Invoke(new Action(() =>
                {
                    DetectedImage.Source = new BitmapImage(new Uri(@"/Resources/camera_image_placeholder.png", UriKind.Relative));
                }));
            });
        }

        void StartCapturing_OnClick(object sender, RoutedEventArgs e)
        {
            if (model.AvailableCameras.Count == 0)
            {
                MessageBox.Show("No camera available!");
                return;
            }

            if (SelectedCamera.SelectedIndex < 0)
            {
                MessageBox.Show("No camera selected!");
                return;
            }
            LogMessage("Capturing started from camera");
            model.IsCapturing = true;
            model.ImagePath = "Source: camera";
            faceDetection.DetectionMode = model.SelectedDetectionMode;
            faceDetection.AttachSource(camera);
            faceDetection.ResetDetections();
            camera.StartCamera(SelectedCamera.SelectedIndex);
        }

        void LoadImage_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            dlg.DefaultExt = ".mp3";
            dlg.Filter = "Image files(*.png, *.jpg, *jpeg, *.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            var result = dlg.ShowDialog();
            if (result == true)
            {
                faceDetection.AttachSource(image);
                if (image.LoadFromFile(dlg.FileName))
                {
                    model.IsCapturing = true;
                    model.ImagePath = "Source: " +  Path.GetFileName(dlg.FileName);
                    faceDetection.ResetDetections();
                    image.StartSending();
                    LogMessage("Image loaded: " + dlg.FileName);
                }
                else
                {
                    MessageBox.Show("Invalid image format, failed to open file", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        void DetectionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            faceDetection.DetectionMode = model.SelectedDetectionMode;
            model.UpdateState();
        }

        void DrawDetectionCheckboxChanged(object sender, RoutedEventArgs e)
        {
            faceDetection.DrawDetection = model.DrawDetection;
            faceDetection.DrawProbableAreas = model.DrawProbableAreas;
        }

        void DetectFace_OnClick(object sender, RoutedEventArgs e)
        {
            faceDetection.ManualDetect();
        }

        void DetectionPeriod_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            faceDetection.DetectionPeriod = TimeSpan.FromMilliseconds(e.NewValue);
        }

        void ShowAdvancedSettings_OnChecked(object sender, RoutedEventArgs e)
        {
            AdvancedSettingsPanel.Visibility = Visibility.Visible;
        }

        void ShowAdvancedSettings_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AdvancedSettingsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
