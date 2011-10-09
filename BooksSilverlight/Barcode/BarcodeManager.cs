using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using com.google.zxing;

namespace BooksSilverlight.Barcode {
    /*
    public static class BarcodeManager {
        /// <summary>
        /// Initializes static variables
        /// </summary>
        static BarcodeManager() {
            LastCaptureResults = new BarcodeCaptureResult(); //Load blank results when initialized
            ScanMode = com.google.zxing.BarcodeFormat.UPC_EAN; //Set default scan mode
        }

        /// <summary>
        /// Sets the type of barcode that should be scanned. Defaults to UPC_EAN.
        /// </summary>
        public static com.google.zxing.BarcodeFormat ScanMode { get; set; }

        /// <summary>
        /// Used to send Try_Harder hint to ZXing multi-readers. See http://code.google.com/p/zxing/wiki/DeveloperNotes
        /// </summary>
        public static System.Collections.Generic.Dictionary<object, object> zxingHints = new System.Collections.Generic.Dictionary<object, object>() { { DecodeHintType.TRY_HARDER, true } };

        /// <summary>
        /// Stores the last barcode capture results for cross-thread access
        /// </summary>
        public static BarcodeCaptureResult LastCaptureResults { get; set; }

        /// <summary>
        /// Delegate called to make sure progress bar is started. Better to use PhoneApplicationService.Current.State["ReturnFromSampleChooser"] and "ReturnFromCameraCapture" instead of this.
        /// </summary>
        public static Action aStartProgress;

        /// <summary>
        /// Used to invoke StartProgress delegate
        /// </summary>
        private static void StartProgress() {
            if (aStartProgress != null) {
                //WP7Utilities.UIThreadInvoke(aStartProgress);
            }
        }

        /// <summary>
        /// Stores the callback delegate that is invoked when processing is finished
        /// </summary>
        private static Action<BarcodeCaptureResult> aFinished;

        /// <summary>
        /// Starts the phone camera so that the user can capture a photo.
        /// WP7Manager.LastCaptureResults will be set to the captured image and text results.
        /// Sets PhoneApplicationService.Current.State["ReturnFromCameraCapture"] flag when application is navigated to after loading photo from camera.
        /// Flag should be removed once data has been processed by main thread callback (Ex: PhoneApplicationService.Current.State.Remove("ReturnFromCameraCapture");)
        /// </summary>
        /// <param name="Finished_Processing">Action method to call when processing is finished</param>
        public static void ScanBarcode(Action<BarcodeCaptureResult> Finished_Processing) {
            StartProgress();
            //aFinished = Finished_Processing; //Store return callback

            LastCaptureResults = new BarcodeCaptureResult(); //Create new result object

            //Microsoft.Phone.Tasks.CameraCaptureTask aTask = new Microsoft.Phone.Tasks.CameraCaptureTask();
           // aTask.Completed += new EventHandler<Microsoft.Phone.Tasks.PhotoResult>(PhotoTask_Completed);
            //aTask.Show(); //Load camera task
        }

        /// <summary>
        /// Overloaded method that processes an existing Uri instead of using the phone's camera
        /// WP7Manager.LastCaptureResults will be set to the captured image and text results.
        /// </summary>
        /// <param name="Finished_Processing">Action method to call when processing is finished</param>
        /// <param name="UriBarcode">URI to barcode image</param>
        public static void ScanBarcode(Uri UriBarcode, Action<BarcodeCaptureResult> Finished_Processing) {
            StartProgress();
            aFinished = Finished_Processing; //Store return callback
            var bi = new BitmapImage(UriBarcode);
            bi.CreateOptions = BitmapCreateOptions.None; //Fixes issue with null pointer reference
            if (bi.PixelHeight == 0 || bi.PixelWidth == 0) {
                LastCaptureResults.ExceptionThrown = new ArgumentException(String.Format("Cannot load selected image. Please make sure URI path is correct.\r\nURI '{0}' cannot be loaded. Try using overloaded ScanBarcode with BitmapImage parameter instead.", UriBarcode.OriginalString));
                throw LastCaptureResults.ExceptionThrown;
            }

            LastCaptureResults = new BarcodeCaptureResult(bi);
            ThreadPool.QueueUserWorkItem(func => ProcessImage());
        }

        /// <summary>
        /// Overloaded method for processing existing BitmatImage.
        /// NOTE: only use this for existing images. If you have access to the URI where the photo came from (camera, resource, or content file) use other methods instead.
        /// </summary>
        /// <param name="imgBarcode">Barcode Image for processing</param>
        /// <param name="Finished_Processing">Callback</param>
        public static void ScanBarcode(BitmapImage imgBarcode, Action<BarcodeCaptureResult> Finished_Processing) {
            aFinished = Finished_Processing; //Store return callback
            LastCaptureResults = new BarcodeCaptureResult(imgBarcode);
            ThreadPool.QueueUserWorkItem(func => ProcessImage());
        }

        /// <summary>
        /// Callback method for processing camera results.
        /// NOTE: This method will be called before the Main Page OnNavigatedTo method.
        /// Sets PhoneApplicationService.Current.State["ReturnFromCameraCapture"] flag to track loading from camera.
        /// Flag should be removed once data has been processed by main thread callback (Ex: PhoneApplicationService.Current.State.Remove("ReturnFromCameraCapture");)
        /// </summary>
        private static void PhotoTask_Completed(object sender, Microsoft.Phone.Tasks.PhotoResult e) {
            StartProgress();
            try {
                PhoneApplicationService.Current.State["ReturnFromCameraCapture"] = true; //Set flag to indicate we are returning from camera capture
                if (e != null && e.TaskResult == Microsoft.Phone.Tasks.TaskResult.OK)//Code from:http://blogs.msdn.com/b/coding4fun/archive/2010/08/09/10048007.aspx
                {
                    LastCaptureResults = new BarcodeCaptureResult(e.ChosenPhoto);
                    ThreadPool.QueueUserWorkItem(func => ProcessImage()); //Process image for barcode on background thread
                }
                else {
                    LastCaptureResults.State = CaptureState.Canceled;
                    LastCaptureResults.ErrorMessage = "Error: Photo capture canceled";
                    ExecuteCallback();
                }
            }
            catch (Exception ex) {
                LastCaptureResults.ExceptionThrown = ex;
                if (ex is com.google.zxing.ReaderException)//Common error thrown when image cannot be recognized
                {
                    LastCaptureResults.ErrorMessage = "Error: Cannot scan barcode. Please try again or enter manually in textbox.";
                    LastCaptureResults.State = CaptureState.ScanFailed;
                }
                else {
                    LastCaptureResults.ErrorMessage = String.Format("PhotoTask_Completed Error: {0}\r\n{1}", ex.GetType(), ex.Message);
                    LastCaptureResults.State = CaptureState.UnknownError;
                }
                ExecuteCallback();
            }
        }

        /// <summary>
        /// This code should be run on a background thread to prevent UI lockup issues.
        /// For information about background threads see http://msdn.microsoft.com/en-us/library/ff967560(VS.92).aspx#BKMK_Background
        /// </summary>
        private static void ProcessImage() {
            LastCaptureResults.State = CaptureState.Processing;
            try {
                bool bIsReady = LastCaptureResults.isReadyForProcessing.WaitOne(3000);
                if (!bIsReady) {
                    LastCaptureResults.State = CaptureState.UnknownError;
                    LastCaptureResults.ErrorMessage = "Error: Timeout waiting for images to be processed. Please try again or send issue report using app bar.";
                    return;
                }

                StartProgress();
                if (LastCaptureResults.BarcodeImage != null) {
                    var wb = LastCaptureResults.VGABarcodeImage;

                    //Code from: btnReadTag_Click in "SLZXingQRSample\SLZXingQRSample\SLZXingSample\MainPage.xaml.vb"
                    var qrRead = GetReader(); // new com.google.zxing.qrcode.QRCodeReader();
                    var luminiance = new RGBLuminanceSource(wb, wb.PixelWidth, wb.PixelHeight);
                    var binarizer = new com.google.zxing.common.HybridBinarizer(luminiance);
                    var binBitmap = new com.google.zxing.BinaryBitmap(binarizer);
                    var results = qrRead.decode(binBitmap, zxingHints); //NOTE: will throw exception if cannot decode image.
                    LastCaptureResults.BarcodeText = results.Text;
                    LastCaptureResults.State = CaptureState.Success;
                    LastCaptureResults.ProcessValue();
                }
                else {
                    LastCaptureResults.State = CaptureState.UnknownError;
                    LastCaptureResults.ErrorMessage = "Cannot process image: invalid or null BitmapImage.";
                }
            }
            catch (Exception ex) {
                LastCaptureResults.ExceptionThrown = ex;
                if (ex is com.google.zxing.ReaderException) {
                    LastCaptureResults.ErrorMessage = "Error: Cannot decode barcode image. Please make sure scan mode is correct and try again.";
                    LastCaptureResults.State = CaptureState.ScanFailed;
                }
                else {
                    Debug.WriteLine("Error: " + ex.ToString());
                    LastCaptureResults.ErrorMessage = String.Format("Barcode Library Processing Error: {0}\r\n{1}", ex.GetType(), ex.Message);
                    LastCaptureResults.State = CaptureState.UnknownError;
                }
            }
            finally {
                ExecuteCallback();
            }
        }

        /// <summary>
        /// Saves a writeable bitmap to a specified file in isolated storage.
        /// This will run on a background thread for better perfomance
        /// </summary>
        /// <param name="wbImage">Bitmap image to save</param>
        /// <param name="strFilename">Filename to save to</param>
        public static void SaveJPEGToIsolatedStorage(WriteableBitmap wbImage, string strFilename) {
            if (!WP7Utilities.isUIThread) {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication()) //http://stackoverflow.com/questions/3632326
                {
                    using (IsolatedStorageFileStream isoFileStream = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Create, isoStore)) {
                        Extensions.SaveJpeg(wbImage, isoFileStream, wbImage.PixelWidth, wbImage.PixelHeight, 0, 100);
                        isoFileStream.Close();
                    }
                }
            }
            else {
                ThreadPool.QueueUserWorkItem(func => SaveJPEGToIsolatedStorage(wbImage, strFilename)); //Run on background thread
            }
        }

        /// <summary>
        /// Returns a Memory stream that can be used to create a BitmapImage of an image from the isolated storage.
        /// BitmapImages can only be created on the UI thread, so this method will load the data into a stream so
        /// that it can be called from either the UI thread or a background thread.
        /// 
        /// Example:
        /// var ms = LoadJPEGFromIsolatedStorage("filename.jpg");
        /// var bi = new BitmapImage(); //Must be called from UI thread.
        /// bi.SetSource(ms);
        /// </summary>
        /// <param name="strFilename">Filename to save to</param>
        public static MemoryStream LoadJPEGFromIsolatedStorage(string strFilename) {
            System.IO.MemoryStream ms;
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication()) //http://stackoverflow.com/questions/3632326
            {
                using (IsolatedStorageFileStream isoFileStream = isoStore.OpenFile(strFilename, System.IO.FileMode.Open)) {
                    byte[] b = new byte[isoFileStream.Length];
                    isoFileStream.Read(b, 0, (int)isoFileStream.Length);
                    isoFileStream.Close();
                    ms = new MemoryStream(b);
                    return ms;
                }
            }
        }

        /// <summary>
        /// Used to rotate an image loaded from a stream.
        /// NOTE: this creates a bitmap image, so it must be called from the UI thread
        /// Code from http://timheuer.com/blog/archive/2010/09/23/working-with-pictures-in-camera-tasks-in-windows-phone-7-orientation-rotation.aspx
        /// </summary>
        /// <param name="stream">Stream with JPEG image</param>
        /// <param name="angle">Number of degrees to rotate image</param>
        /// <returns>New stream with rotated image</returns>
        public static Stream RotateStream(Stream stream, int angle) {
            stream.Position = 0;
            if (angle % 90 != 0 || angle < 0) throw new ArgumentException();
            if (angle % 360 == 0) return stream;

            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(stream);
            WriteableBitmap wbSource = new WriteableBitmap(bitmap);

            WriteableBitmap wbTarget = null;
            if (angle % 180 == 0) {
                wbTarget = new WriteableBitmap(wbSource.PixelWidth, wbSource.PixelHeight);
            }
            else {
                wbTarget = new WriteableBitmap(wbSource.PixelHeight, wbSource.PixelWidth);
            }

            for (int x = 0; x < wbSource.PixelWidth; x++) {
                for (int y = 0; y < wbSource.PixelHeight; y++) {
                    switch (angle % 360) {
                        case 90:
                            wbTarget.Pixels[(wbSource.PixelHeight - y - 1) + x * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 180:
                            wbTarget.Pixels[(wbSource.PixelWidth - x - 1) + (wbSource.PixelHeight - y - 1) * wbSource.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 270:
                            wbTarget.Pixels[y + (wbSource.PixelWidth - x - 1) * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                    }
                }
            }
            MemoryStream targetStream = new MemoryStream();
            wbTarget.SaveJpeg(targetStream, wbTarget.PixelWidth, wbTarget.PixelHeight, 0, 100);
            return targetStream;
        }

        private static void ExecuteCallback() {
            WP7Utilities.UIThreadInvoke(aFinished, LastCaptureResults); //Execute callback on UI thread
        }

        /// <summary>
        /// Returns the zxing reader class for the current specified ScanMode.
        /// </summary>
        /// <returns></returns>
        public static com.google.zxing.Reader GetReader() {
            com.google.zxing.Reader r;
            switch (BarcodeManager.ScanMode.Name) {
                case "CODE_128":
                    r = new com.google.zxing.oned.Code128Reader();
                    break;
                case "CODE_39":
                    r = new com.google.zxing.oned.Code39Reader();
                    break;
                case "EAN_13":
                    r = new com.google.zxing.oned.EAN13Reader();
                    break;
                case "EAN_8":
                    r = new com.google.zxing.oned.EAN8Reader();
                    break;
                case "ITF":
                    r = new com.google.zxing.oned.ITFReader();
                    break;
                case "UPC_A":
                    r = new com.google.zxing.oned.UPCAReader();
                    break;
                case "UPC_E":
                    r = new com.google.zxing.oned.UPCEReader();
                    break;
                case "QR_CODE":
                    r = new com.google.zxing.qrcode.QRCodeReader();
                    break;
                case "DATAMATRIX":
                    r = new com.google.zxing.datamatrix.DataMatrixReader();
                    break;

                //Auto-Detect:
                case "UPC_EAN":
                    r = new com.google.zxing.oned.MultiFormatUPCEANReader(zxingHints);
                    break;
                case "ALL_1D":
                    r = new com.google.zxing.oned.MultiFormatOneDReader(zxingHints);
                    break;

                default:
                    r = null;
                    if (LastCaptureResults != null) {
                        LastCaptureResults.ExceptionThrown = new ArgumentException("Error: Unknown barcode type");
                    }
                    throw new ArgumentException("Error: Unknown barcode type");
            }
            return r;
        }
    }*/
}
