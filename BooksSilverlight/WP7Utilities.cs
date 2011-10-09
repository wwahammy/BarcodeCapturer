using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BooksSilverlight {
    public static class WP7Utilities {
        /// <summary>
        /// Returns true if application is running on an Emulator instead of actual device
        /// </summary>
        //public static bool isEmulatorMode { get { return Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator; } }


        private static Visibility v;
        private static AutoResetEvent areSignal;

        /// <summary>
        /// Returns true if the application is running under the Light theme instead of default Dark theme.
        /// </summary>
        public static bool isLightTheme //TODO: find a better way to do this
        {
            get {
                if (IsDesignTime)//Throws null exception at design time
                {
                    return false;
                }
                else if (WP7Utilities.isUIThread) {
                    v = (Visibility)Application.Current.Resources["PhoneLightThemeVisibility"];
                }
                else {
                    areSignal = new AutoResetEvent(false);
                    WP7Utilities.UIThreadInvoke(() => { v = (Visibility)Application.Current.Resources["PhoneLightThemeVisibility"]; areSignal.Set(); });
                    areSignal.WaitOne(3000);
                }
                return v == System.Windows.Visibility.Visible;
            }
        }


        private static bool _bDesignFlag = false;//flag for if _bDesignTime has been set yet
        private static bool _bDesignTime = false;//private variable that caches the results.
        /// <summary>
        /// Returns true when project is being rendered using Visual Studio or Blend designer
        /// Useful to omit code that could cause the designer to crash.
        /// </summary>
        public static bool IsDesignTime {
            get {
                try {
                    if (!_bDesignFlag) {
                        //Took a while to figure out (see below) how to do this on VS 2010 Cider designer AND Blend, but this seems to work
                        //sri gets files from the ZAP, which won't exist at design time so it always returns null.
                        //we just have to specify a file that always exists, like the dll for this assembly or WMAppManifest.xml. TODO: Test this using marketplace zap file
                        System.Windows.Resources.StreamResourceInfo sri = Application.GetResourceStream(new Uri("WMAppManifest.xml", UriKind.Relative));
                        _bDesignTime = sri == null;
                    }
                    return _bDesignTime;
                }
                catch (Exception ex) {
                    Debug.WriteLine("Error calling WP7Utilities.IsDesignTime: " + ex);
                    return false; //default to not design time
                }
            }
        }

        /*{
            get
            {
                /*
                //From http://bryantlikes.com/DetectingDesignModeInSilverlight.aspx
                //Doesn't work in WP7.
                try
                {
                    var host = Application.Current.Host.Source;
                    return false;
                }
                catch
                {
                    return true;
                }
                * /
                //return System.ComponentModel.DesignerProperties.IsInDesignTool; //from http://geekswithblogs.net/lbugnion/archive/2009/09/05/detecting-design-time-mode-in-wpf-and-silverlight.aspx

                var prop = System.ComponentModel.DesignerProperties.IsInDesignModeProperty;
                return = (bool) System.ComponentModel.DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
            }
        }

        private static bool? _isInDesignMode;
        /// <summary>
        /// Gets a value indicating whether the control is in design mode (running in Blend
        /// or Visual Studio).
        /// NOTE: DOES NOT SEEM TO WORK IN VISUAL STUDIO CIDER DESIGNER
        /// </summary>
        public static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                {
#if SILVERLIGHT
                    _isInDesignMode = System.ComponentModel.DesignerProperties.IsInDesignTool;
#else
            var prop = DesignerProperties.IsInDesignModeProperty;
            _isInDesignMode
                = (bool)DependencyPropertyDescriptor
                .FromProperty(prop, typeof(FrameworkElement))
                .Metadata.DefaultValue;
#endif
                }

                return _isInDesignMode.Value;
            }
        }
        */

        /// <summary>
        /// Runs an action on the UI thread.
        /// Example: WP7Utilities.UIThreadInvoke(new Action(() => {BitmapImage x = new BitmapImage();}) 
        /// </summary>
        public static DispatcherOperation UIThreadInvoke(Action a) {

            if (isUIThread)
            {
                a.Invoke();
                return null;
            }
            else
            { return Deployment.Current.Dispatcher.BeginInvoke(a); }
                
        }

        /// <summary>
        /// Runs an action on the UI thread.
        /// Example: WP7Utilities.UIThreadInvoke(new Action(() => {BitmapImage x = new BitmapImage();}) 
        /// </summary>
        public static DispatcherOperation UIThreadInvoke(Delegate d, params object[] args) {

                return Deployment.Current.Dispatcher.BeginInvoke(d, args);
        }

        /// <summary>
        /// Runs an action on the UI thread after the specified amount of time using a DispatchTimer
        /// Example: WP7Utilities.UIThreadInvoke(new Action(() => {BitmapImage x = new BitmapImage();}) 
        /// </summary>
        /// <param name="tsDelay">Delay before invoke will begin. Ex: TimeSpan.FromSeconds(2)</param>
        /// <param name="a">Code to execute</param>
        public static void UIThreadDelayInvoke(TimeSpan tsDelay, Action a) {
            /* Use Threadpool with sleep instead as it is easier than the dispatch timer
            DispatcherTimer timer = new DispatcherTimer();
            timer = new DispatcherTimer();
            timer.Interval = tsDelay;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            */
            ThreadPool.QueueUserWorkItem(func => { System.Threading.Thread.Sleep(tsDelay); UIThreadInvoke(a); });
        }

        /// <summary>
        /// Returns true if called from UI thread, false otherwise
        /// </summary>
        public static bool isUIThread {
            get {
                if (IsDesignTime)//Dispatcher not available at design time
                {
                    return true;
                }
                else {
                    return Deployment.Current.Dispatcher.CheckAccess();
                }
            }
        }


        
        
    }
}
