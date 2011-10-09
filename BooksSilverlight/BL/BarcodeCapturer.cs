using System;

using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BooksSilverlight.Barcode;
using com.google.zxing;
using System.Threading;

namespace BooksSilverlight.BL
{
    public class BarcodeCapturer
    {
        public static Hashtable zxingHints = new Hashtable() { { DecodeHintType.TRY_HARDER, true } };
        private Task _resultTask;
        private object _resultTaskLock = new object();
        Timer timer = null;
        public BarcodeCapturer()
        {
            CaptureDeviceManager.CaptureDeviceSet += BindToNewCaptureDevice;
        }

        void BindToNewCaptureDevice(CaptureDevice obj) {
            
            obj.Source.CaptureImageCompleted += ImageCaptured;

            var dispatch = obj.Source.Dispatcher;
            timer = new Timer((input) => dispatch.BeginInvoke(() => obj.Source.CaptureImageAsync()), null, 0, 250);
           
        }

        void ImageCaptured(object sender, System.Windows.Media.CaptureImageCompletedEventArgs e)
        {

            if (_resultTask != null )
                return;
            lock (_resultTaskLock)
            {

                var initialTask = new Task<BarcodeCaptureResult>(() => CreateBarcodeCapture(e.Result));
                _resultTask = initialTask.
                    ContinueWith((taskIn) =>
                                     {
                                         if (taskIn.IsCompleted)
                                         {
                                             ProcessImage(taskIn.Result);
                                         }
                                     });

                initialTask.Start();
                
            }
        }

        private BarcodeCaptureResult CreateBarcodeCapture(WriteableBitmap bitmapIn)
        {
            BarcodeCaptureResult ret = null;
            using (var are = new System.Threading.AutoResetEvent(false)) //Use AutoResetEvent to wait for results from dispatcher
            {
                WP7Utilities.UIThreadInvoke(() =>
                                                {
                                                    ret = new BarcodeCaptureResult(bitmapIn);
                                                    are.Set();
                                                });
                are.WaitOne();
            }

            return ret;
        }


       

        private void ProcessImage(BarcodeCaptureResult captureResults) {
            captureResults.State = CaptureState.Processing;
            try {
                bool bIsReady = captureResults.isReadyForProcessing.WaitOne(3000);
                if (!bIsReady) {
                    captureResults.State = CaptureState.UnknownError;
                    captureResults.ErrorMessage = "Error: Timeout waiting for images to be processed. Please try again or send issue report using app bar.";
                    return;
                }


                if (captureResults.VGABarcodeImage != null)
                {
                    var wb = captureResults.VGABarcodeImage;

                    //Code from: btnReadTag_Click in "SLZXingQRSample\SLZXingQRSample\SLZXingSample\MainPage.xaml.vb"
                    var qrRead = new com.google.zxing.oned.MultiFormatUPCEANReader(zxingHints); ; // new com.google.zxing.qrcode.QRCodeReader();
                    var luminiance = new RGBLuminanceSource(wb, 640, 480);
                    var binarizer = new com.google.zxing.common.HybridBinarizer(luminiance);
                    var binBitmap = new com.google.zxing.BinaryBitmap(binarizer);
                    var results = qrRead.decode(binBitmap, zxingHints); //NOTE: will throw exception if cannot decode image.
                    captureResults.BarcodeText = results.Text;
                    captureResults.State = CaptureState.Success;

                    BarcodeCaptured.Invoke(captureResults);
                    //captureResults.ProcessValue();
                }
                else {
                    captureResults.State = CaptureState.UnknownError;
                    captureResults.ErrorMessage = "Cannot process image: invalid or null BitmapImage.";
                }
            }
            catch (Exception ex) {
                captureResults.ExceptionThrown = ex;
                if (ex is com.google.zxing.ReaderException) {
                    captureResults.ErrorMessage = "Error: Cannot decode barcode image. Please make sure scan mode is correct and try again.";
                    captureResults.State = CaptureState.ScanFailed;
                }
                else {
                    Debug.WriteLine("Error: " + ex.ToString());
                    captureResults.ErrorMessage = String.Format("Barcode Library Processing Error: {0}\r\n{1}", ex.GetType(), ex.Message);
                    captureResults.State = CaptureState.UnknownError;
                }
            }
            finally
            {
                lock (_resultTaskLock)
                {
                    _resultTask = null;
                }
            }
            
        }


        public event Action<BarcodeCaptureResult> BarcodeCaptured = delegate { };



    }
}