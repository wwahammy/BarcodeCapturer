using System;
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
    public static class CaptureDeviceManager 
    {
        private static CaptureDevice _captureDevice;

        public static CaptureDevice CaptureDevice {
            get { return _captureDevice; }
            set {
                if (_captureDevice != null)
                    _captureDevice.Source.Stop();
                _captureDevice = value;
                _captureDevice.Source.Start();
                CaptureDeviceSet.Invoke(_captureDevice);
            }
        }

        public static event Action<CaptureDevice> CaptureDeviceSet = delegate { };
    }
}
