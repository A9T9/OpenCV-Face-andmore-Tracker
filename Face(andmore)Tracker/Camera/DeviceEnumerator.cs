using System.Collections.Generic;
using AForge.Video.DirectShow;

namespace FaceFinderDemo.Camera
{
    /// <summary>
    /// EMGU CV doesn't support detecting available cameras, so we use directshow for it.
    /// </summary>
    public static class DeviceEnumerator
    {
        public static List<string> GetDeviceNames()
        {
            var devices = new List<string>();
            FilterInfoCollection videoDevices = new FilterInfoCollection(
                        FilterCategory.VideoInputDevice);
            for (int i = 0; i != videoDevices.Count; i++)
            {
                var dev = videoDevices[i];
                devices.Add(dev.Name);
            }
            // OpenCV seems to handle the order in the other direction
            devices.Reverse();
            return devices;
        }
    }
}
