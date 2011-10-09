using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BooksSilverlight.BL {
    public class VideoCaptureDeviceManager 
    {
        
        /// <summary>
        /// Whether we can access capture devices
        /// </summary>
        public static bool CanCapture {
            get { return CaptureDeviceConfiguration.AllowedDeviceAccess; }
        }

        public static IEnumerable<CaptureDevice> CaptureDevices
        {
            get
            {
                return
                    CaptureDeviceConfiguration.GetAvailableVideoCaptureDevices().Select(
                        device => new CaptureDevice(device));
            }
        }

        public static CaptureDevice DefaultDevice
        {
            get { 
                var devs = CaptureDevices.ToArray();
                if (devs.Length == 1)
                    return devs[0];
                return devs.First((c) => c.Source.VideoCaptureDevice.IsDefaultDevice); }
        }


    }
}
