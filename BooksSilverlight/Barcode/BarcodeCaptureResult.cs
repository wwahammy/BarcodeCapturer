using System;
using System.IO;
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
using BooksSilverlight.Extensions;
using ExifLib;

namespace BooksSilverlight.Barcode {
    /// <summary>
    /// Represents the current state of the barcode capture process
    /// </summary>
    public enum CaptureState {
        Initializing,
        ImageLoaded,
        Processing,
        Success,
        ScanFailed,
        UnknownError,
        Canceled,
        ManualEntry
    }

    /// <summary>
    /// Represents the results of a barcode scan. Most recent barcode scan will be stored to WP7Manager.LastCaptureResults
    /// </summary>
    public class BarcodeCaptureResult {
        /// <summary>
        /// Indicates if the Original fullsize image should be saved to OrginalImage (used for debugging). 
        /// A VGA quality image will be used for BarcodeImage and wbBarcodeImage.
        /// NOTE: the original image will NOT be persisted across sessions, only the VGA image is stored.
        /// </summary>
        public static bool SaveOriginalImage = false; //TODO: Add user setting or trigger to enable this and save original images?

        /// <summary>
        /// Default constructor. Object will be left in the Initializing state, which should be changed to Success/Failed/Unknown when finished processing.
        /// </summary>
        public BarcodeCaptureResult() {
            BarcodeFormat = com.google.zxing.BarcodeFormat.UPC_EAN; //Set barcode type for these results
            State = CaptureState.Initializing;
        }
        
        public BarcodeCaptureResult(WriteableBitmap writeableBitmap)
        {
            var memStream = new MemoryStream();
            writeableBitmap.SaveJpeg(memStream, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 0, 95);
            SetupBarcodeImages(memStream);
        }

        /// <summary>
        /// Creates a new result object using an existing image loaded from IsolatedStorage or from the XAP file.
        /// <para>NOTE: if you have access to the original photo stream then BarcodeCaptureResult(System.IO.Stream ImageStream)
        /// is preferred as it will be able to save a High-Resolution version of the image (BitmapImage is limited to 1MP)</para>
        /// </summary>
        /// <param name="bmpBarcodeImage">BitmapImage of barcode</param>
        public BarcodeCaptureResult(BitmapImage bmpBarcodeImage)
            : this() {
            BarcodeImage = bmpBarcodeImage;
            SetupBarcodeImages();
        }

        /// <summary>
        /// Creates a new result object using a stream to a JPEG image. Most often this would be e.ChosenPhoto from the Camera PhotoResult, but could also be an Application.GetResourceStream
        /// </summary>
        /// <param name="ImageStream">e.ChosenPhoto from the Camera PhotoResult, but could also be an Application.GetResourceStream</param>
        public BarcodeCaptureResult(System.IO.Stream ImageStream)
            : this() {
            SetupBarcodeImages(ImageStream);
        }

        /// <summary>
        /// Image of the barcode. When using the camera this is initialized using BarcodeImage.SetSource(e.ChosenPhoto) and is available immediatly (Other images are queued for processing);
        /// </summary>
        public BitmapImage BarcodeImage { get; set; }

        /// <summary>
        /// VGA resolution version of BarcodeImage that will be used for processing
        /// </summary>
        public WriteableBitmap VGABarcodeImage;

        /// <summary>
        /// Cache used to store writeable bitmap version of BarcodeImage. 
        /// Note: image should be Small for good performance (640 x 480 or 1MP seems optimal)
        /// </summary>
        public WriteableBitmap wbBarcodeImage;

        /// <summary>
        /// Stores the original full size image when SaveOriginalImage == true, or null otherwise.
        /// NOTE: the original image will NOT be persisted across sessions, only the VGA image is stored.
        /// </summary>
        public WriteableBitmap OriginalImage { get; set; }

        /// <summary>
        /// Success, ScanFailed, UnknownError, Canceled, ...
        /// </summary>
        public CaptureState State { get; set; }

        /// <summary>
        /// Error message if State was not Success or "" if it was successfull
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Text results of the barcode or "" if unable to process image. If BarcodeText is edited you should call Call ProcessValue() to detect special types.
        /// </summary>
        public string BarcodeText { get; set; }

        /// <summary>
        /// Filename where barcode is stored in isolated storage
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Stores information about the JPEG image generated using the ExifLib
        /// </summary>
        public ExifLib.JpegInfo ExifInfo { get; set; }

        /// <summary>
        /// Type of barcode user indicated should be scanned. NOTE this might be a multi-reader like "UPC or EAN"
        /// </summary>
        public com.google.zxing.BarcodeFormat BarcodeFormat { get; set; }

        /// <summary>
        /// Used to make sure images are loaded before we start processing
        /// </summary>
        internal System.Threading.AutoResetEvent isReadyForProcessing = new System.Threading.AutoResetEvent(false);

        /// <summary>
        /// Last exception that was thrown. Usually this is a reader exception from zxing indicating the barcode could not be detected.
        /// </summary>
        public Exception ExceptionThrown;
        /*
        /// <summary>
        /// Saves the current Barcode Image to IsolatedStorage using the given filename. 
        /// Also the Filename property for this object will be updated with the new value.
        /// </summary>
        /// <param name="strFilename">Path and filename for where to put the image in IsolatedStorage</param>
        public void SaveJPEGToIsolatedStorage(string strFilename) {
            try {
                if (BarcodeImage == null) {
                    throw new NullReferenceException("BarcodeImage must be set before you can call SaveJPEGToIsolatedStorage.");
                }

                var wb = this.VGABarcodeImage;  //NOTE: save VGA Image for quicker loading
                if (wb.PixelWidth == 0) {
                    wb = this.wbBarcodeImage;
                }
                BarcodeManager.SaveJPEGToIsolatedStorage(wb, strFilename);
                Filename = strFilename;
            }
            catch (Exception ex) {
                ExceptionThrown = new Exception("Cannot save image to isolated storage.", ex);
                throw ExceptionThrown;
            }
        }*/
/*
        /// <summary>
        /// Loads BarcodeImage from IsolatedStorage using the given filename. 
        /// Also the Filename property will be updated with the new value.
        /// NOTE: BarcodeText, result and ErrorMessage will NOT be set. Also this must be called from the UI thread.
        /// </summary>
        /// <param name="strFilename">Path and filename for where to load the image from IsolatedStorage</param>
        public void LoadJPEGFromIsolatedStorage(string strFilename) {
            try {
                System.IO.Stream s = BarcodeManager.LoadJPEGFromIsolatedStorage(strFilename);
                BarcodeImage = new BitmapImage();
                BarcodeImage.CreateOptions = BitmapCreateOptions.None; //Prevent issues with delayed creation
                BarcodeImage.SetSource(s);
                Filename = strFilename;

                //TODO: Add call to Setup Images?
            }
            catch (Exception ex) {
                ExceptionThrown = new Exception("Cannot load image from isolated storage.", ex);
                throw ExceptionThrown;
            }*
        }
        */
        /// <summary>
        /// This method will setup the OriginalImage, VGABarcodeImage, and wbBarcodeImage values. It will also rotate the image if it was taken in portrait mode.
        /// </summary>
        /// <param name="ImageStream">Optional Stream to the original image (Use Camera PhotoResult or Application.GetResourceStream). Will use BarcodeImage.UriSource if omitted.</param>
        private void SetupBarcodeImages(System.IO.Stream ImageStream = null) {
            
            if (BarcodeImage == null || BarcodeImage.PixelWidth == 0) {
                if (ImageStream != null) {
                    //Check image orientation. See http://timheuer.com/blog/archive/2010/09/23/working-with-pictures-in-camera-tasks-in-windows-phone-7-orientation-rotation.aspx
                    this.ExifInfo = ExifReader.ReadJpeg(ImageStream, "");
                    int _angle = 0;

                    switch (ExifInfo.Orientation) {
                        case ExifOrientation.TopLeft:
                        case ExifOrientation.Undefined:
                            _angle = 0;
                            break;
                        case ExifOrientation.TopRight:
                            _angle = 90;
                            break;
                        case ExifOrientation.BottomRight:
                            _angle = 180;
                            break;
                        case ExifOrientation.BottomLeft:
                            _angle = 270;
                            break;
                    }

                    if (_angle > 0d) {
                        ImageStream = RotateStream(ImageStream, _angle);
                        ExifInfo = ExifReader.ReadJpeg(ImageStream, ""); //reload info
                    }

                    WP7Utilities.UIThreadInvoke(() => new BitmapImage());
                    
                    BarcodeImage.CreateOptions = BitmapCreateOptions.None;//Don't delay creation

                    BarcodeImage.SetSource(ImageStream);
                   
                }
                else {
                    ExceptionThrown = new NullReferenceException("BarcodeImage must be set before you can call SetupBarcodeImages.");
                    throw ExceptionThrown;
                }
            }

            State = CaptureState.ImageLoaded;
            //Resize images to VGA or 1MP resolution
            if (BarcodeImage.PixelWidth <= 1280 && BarcodeImage.PixelHeight <= 960) //Don't resize if image is below 1MP
            {
                VGABarcodeImage = GetWriteableBitmap(BarcodeImage.PixelWidth, BarcodeImage.PixelHeight, ImageStream);
                if (BarcodeCaptureResult.SaveOriginalImage) {
                    OriginalImage = VGABarcodeImage; //Input images is already the correct size
                }
            }
            else //Resize images that are over 1MP down to VGA size
            {
                VGABarcodeImage = GetWriteableBitmap(640, 0, ImageStream);
                if (BarcodeCaptureResult.SaveOriginalImage) {
                    this.OriginalImage = GetWriteableBitmap(2592, 0, ImageStream); //TODO: use  EXIF info to get full size. For now always use 5MP Width
                }
            }

            this.isReadyForProcessing.Set();//Signal that object is ready for processing
           // SaveJPEGToIsolatedStorage("LastCapture.jpg"); //Save image to local storage.
        }

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

        /// <summary>
        /// Converts the current Image into a WriteableBitmap.
        /// This method will be dispatched to the UI thread if called from a background thread.
        /// </summary>
        /// <param name="Width">Width of desired return image (Required)</param>
        /// <param name="Height">Optional height of return image. Will maintain original aspect ratio if Height == 0.</param>
        /// <param name="ImageStream">Optional stream to original image (Use Camera PhotoResult or Application.GetResourceStream). Will use BarcodeImage.UriSource if omitted.</param>
        public WriteableBitmap GetWriteableBitmap(int Width, int Height = 0, System.IO.Stream ImageStream = null) {
            //WARNING: I kept getting Null Pointer Exceptions "Invalid pointer" in "MS.Internal.XcpImports.CheckHResult" when creating WriteableBitmap from resource or content files
            //Seems like it works so long as the BitmapImage object was created in calling page and is set to the Source of an Image Control
            //Errors also occur when URI does not have a leading slash (ie: background.png instead of /background.png
            //Code from: http://msdn.microsoft.com/en-us/library/ff967560(v=VS.92).aspx#CodeSpippet1 
            //Question: why does LoadOriginalImage() require Application.GetResourceStream(new Uri (UriImage.OriginalString.TrimStart('/'), UriKind.Relative)); //TODO: Figure out why this works!!!

            //NOTE issues with accessing image from background thread. http://stackoverflow.com/questions/1924408/invalid-cross-thread-access-issue
            //Cannot create a WriteableBitmap from background thread or access BarcodeImage.PixelWidth/Height properties.
            if (!WP7Utilities.isUIThread) //Invoke on dispatcher if method called from background thread
            {
                WriteableBitmap wbReturn = new WriteableBitmap(0, 0);
                using (var are = new System.Threading.AutoResetEvent(false)) //Use AutoResetEvent to wait for results from dispatcher
                {
                    WP7Utilities.UIThreadInvoke(() => //Invoke method on UI Thread
                    {
                        wbReturn = GetWriteableBitmap(Width, Height, ImageStream);
                        are.Set(); //Signal background thread
                    });
                    are.WaitOne(); //Wait for signal from dispatch thread;
                }
                return wbReturn;
            }
            else //called from UI Thread so process image
            {
                try {
                    if (BarcodeImage == null) {
                        ExceptionThrown = new NullReferenceException("BarcodeImage must be set before you can call SetupBarcodeImages.");
                        throw ExceptionThrown;
                    }

                    if (Height == 0)//Calculate new height using original aspect ratio
                    {
                        Height = (int)(Width * ((Double)BarcodeImage.PixelHeight) / (double)BarcodeImage.PixelWidth);
                    }

                    wbBarcodeImage = new WriteableBitmap(Width, Height);//set height and width of new image. Use wbBarcodeImage to cache results.

                    if (ImageStream == null)//Use BarcodeImage.UriSource if stream is not provided
                    {
                        System.Windows.Resources.StreamResourceInfo sri = null;
                        if (BarcodeImage.UriSource.OriginalString == "")//Special code if image comes from camera
                        {
                            //Can't access source image, so just return a WriteableBitmap of current Bitmap
                            System.Diagnostics.Debug.WriteLine("Warning: GetWriteableBitmap Could not locate image UriSource. Original image cannot be loaded.");
                            return new WriteableBitmap(BarcodeImage); //TODO: use new EXIF information to get original size information.
                        }
                        sri = Application.GetResourceStream(BarcodeImage.UriSource);
                        if (sri == null) //Check if object was found
                        {
                            sri = Application.GetResourceStream(new Uri(BarcodeImage.UriSource.OriginalString.TrimStart('/'), UriKind.Relative)); //try without leading / TODO: Figure out why this works!!
                        }
                        if (sri != null) {
                            ImageStream = sri.Stream; //Update imagestream that will be used for processing
                        }
                        else //GetResourceStream can't load all types, so fall back on standard load.
                        {
                            //wbBarcodeImage = new WriteableBitmap(BarcodeImage);//TODO: Make sure image is 640x480
                            throw new Exception("GetWriteableBitmap cannot open image stream.");
                        }
                    }
                    else //ImageStream was provided by caller
                    {
                        ImageStream.Seek(0, System.IO.SeekOrigin.Begin); // Seek to the beginning of the stream
                    }

                    wbBarcodeImage.LoadJpeg(ImageStream);//Load JPEG from stream into our re-sized writeable bitmap

                    return wbBarcodeImage;
                }
                catch (Exception ex) {
                    ExceptionThrown = new InvalidOperationException("Error: Cannot create WriteableBitmap.\r\n", ex); //store exception
                    throw ExceptionThrown;
                }
            }
        }

        /// <summary>
        /// Used to detect special value types such as URLs and (eventually) Phone numbers.
        /// This method should be called whenever the BarcodeText is updated (ex: manual entry)
        /// </summary>
        public void ProcessValue() {
            try {
                //Check for URLs in results.
                System.Text.RegularExpressions.Regex rxURL = new System.Text.RegularExpressions.Regex(@"(?:(?<protocol>http|ftp|https)://)?(?<![@a-zA-z0-9])(?<domain>[\w\.]{1,255}[.](?:[a-zA-Z]{2,8}|\d{1,3}))(?<path>/[a-z0-9\._/~%\-\+&\#\?!=\(\)@]*)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var match = rxURL.Match(BarcodeText);
                if (match.Success) {
                    if (match.Value.Substring(0, 10).Contains("://")) {
                        Uri.TryCreate(match.Value, UriKind.Absolute, out _URI);
                    }
                    else {
                        Uri.TryCreate("http://" + match.Value, UriKind.Absolute, out _URI);
                    }
                }
                else if (BarcodeText.ToLower().StartsWith("zune://"))//zune:// urls are used by marketplace links and other things (Camera URI)
                {
                    Uri.TryCreate(BarcodeText, UriKind.Absolute, out _URI);
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("Error in ProcessingValue: " + ex.Message);
            }
        }


        //
        //
        // Special detected types
        //
        //
        /// <summary>
        /// URI found in barcode text or null if no URI was found
        /// </summary>
        public Uri _URI;
    }
}
