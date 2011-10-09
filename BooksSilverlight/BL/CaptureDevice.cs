using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace BooksSilverlight.BL {
    public class CaptureDevice 
    {
        
        private VideoCaptureDevice _device;
        private static Dictionary<VideoCaptureDevice, CaptureSource> sources = new Dictionary<VideoCaptureDevice, CaptureSource>(); 

        public CaptureDevice(VideoCaptureDevice device)
        {
            _device = device;
            lock(this.GetType()) 
            {
                if (!sources.ContainsKey(device))
                {
                    sources[_device] = new CaptureSource {VideoCaptureDevice = device};
                }
            }
        }

        public CaptureSource Source
        {
            get { return sources[_device]; }
        }

        
    }
}
