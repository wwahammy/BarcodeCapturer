using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BooksSilverlight.ViewModels;

namespace BooksSilverlight {
    public partial class MainPage : UserControl {
        public MainPage() {
            InitializeComponent();
        }

        private CaptureSource _cam = new CaptureSource();

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            //check if we have access to video capture device
            if (CaptureDeviceConfiguration.AllowedDeviceAccess)
            {
                
            }

            else
            {
                
            }
        }

        private void ConnectWebcamToDevice()
        {
            if (!CaptureDeviceConfiguration.AllowedDeviceAccess)
            {
                if(!CaptureDeviceConfiguration.RequestDeviceAccess())
                    return;
            }

            _cam.VideoCaptureDevice = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();
            var brush = new VideoBrush {Stretch = Stretch.Uniform};
            brush.SetSource(_cam);

            CamDisplay.Fill = brush;

            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CaptureDeviceConfiguration.RequestDeviceAccess();
            (this.DataContext as MainPageViewModel).SetToDefaultDevice();
        }
    }
}
