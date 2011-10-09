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
using BooksSilverlight.BL;
using CaptureDevice = System.Windows.Media.CaptureDevice;
using System.Collections.ObjectModel;

namespace BooksSilverlight.ViewModels {
    public class MainPageViewModel : ViewModelBase
    {
        private BL.CaptureDevice _captureDevice;
        private BarcodeCapturer _barcodeCapturer;
        private string _barCode;
        /*
        public ObservableCollection<Textbook> Textbooks { get;
            private set;
        }*/
        public MainPageViewModel()
        {
            CaptureDeviceManager.CaptureDeviceSet += (d) => CaptureDevice = d;
            _barcodeCapturer = new BarcodeCapturer();
            _barcodeCapturer.BarcodeCaptured += new Action<Barcode.BarcodeCaptureResult>(_barcodeCapturer_BarcodeCaptured);
            CaptureDeviceConfiguration.RequestDeviceAccess();
            
        }

        public void SetToDefaultDevice()
        {
            if (VideoCaptureDeviceManager.CanCapture)
                CaptureDeviceManager.CaptureDevice = VideoCaptureDeviceManager.DefaultDevice;
        }

        public string BarCode
        {
            get { return _barCode; }
            set
            {
                _barCode = value;
                base.OnPropertyChanged("BarCode");
                base.OnPropertyChanged("UPCString");
            }
        }

        void  _barcodeCapturer_BarcodeCaptured(Barcode.BarcodeCaptureResult obj)
        {
            BarCode = obj.BarcodeText;
        }


        public BL.CaptureDevice CaptureDevice {
            get { return _captureDevice; }
            private set { _captureDevice = value;
                

                base.OnPropertyChanged("CaptureDevice"); 
                base.OnPropertyChanged("CaptureDeviceSource");
                base.OnPropertyChanged("CaptureBrush");
            }
        }

        public CaptureSource CaptureDeviceSource
        {
            get { if(CaptureDevice != null)
                return CaptureDevice.Source;
            return null;
            }
        }

        public Brush CaptureBrush
        {
            get
            {
                if (CaptureDevice == null)
                {
                    return new SolidColorBrush(Colors.Black);
                }
                else
                {
                    var vidBrush = new VideoBrush { Stretch = Stretch.Uniform };
                    vidBrush.SetSource(CaptureDeviceSource);
                    return vidBrush;
                }
            }
        }

        public string UPCString
        {
            get { return "The UPC is " + BarCode; }
        }
    }
}
