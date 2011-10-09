using System.IO;
using System.Windows.Media.Imaging;
using ImageTools;
using ImageTools.Filtering;
using ImageTools.IO.Jpeg;
using System.Threading;

namespace BooksSilverlight.Extensions
{
    public static class WriteableBitmapExtensions
    {
        public static void SaveJpeg(this WriteableBitmap bitmap, Stream targetStream, int targetWidth, int targetHeight, int orientation, int quality)
        {
            ExtendedImage image = null;
            var disp = bitmap.Dispatcher;
            using (var are = new AutoResetEvent(false))
            {
                disp.BeginInvoke(() =>
                {
                    image = bitmap.ToImage();
                    are.Set();
                });
                are.WaitOne();
            }
            
            var resizer = new NearestNeighborResizer();
            var imageBaseOut = new ExtendedImage(targetWidth, targetHeight);
            resizer.Resize(image, imageBaseOut, targetWidth, targetHeight);
            var encoder = new JpegEncoder {Quality = quality};
            encoder.Encode(imageBaseOut, targetStream);
        }

        public static void LoadJpeg(this WriteableBitmap bitmap, Stream sourceStream)
        {
            JpegDecoder decoder = new JpegDecoder();

            var image = new ExtendedImage();
            
            decoder.Decode(image, sourceStream);

            WP7Utilities.UIThreadInvoke(() => bitmap.SetSource(image.ToStream()));
        }
    }
}