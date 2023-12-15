using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// https://github.com/NKnusperer/libwebp-sharp

namespace ImgBrowser.AdditionalImageFormats.Webp
{
    public class WebPDecoder
    {
        private enum DecodeType
        {
            RGB,
            RGBA,
            BGR,
            BGRA,
            YUV
        };

        /// <summary>
        /// The decoder's version number
        /// </summary>
        /// <returns>The version as major.minor.revision</returns>
        public string GetDecoderVersion()
        {
            var version = NativeWebPDecoder.WebPGetDecoderVersion();
            return $"{(version >> 16) & 0xff}.{(version >> 8) & 0xff}.{version & 0xff}";
        }

        /// <summary>
        /// Validate the WebP image header and retrieve the image height and width
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="imgWidth">Returns the width of the WebP image</param>
        /// <param name="imgHeight">Returnsthe height of the WebP image</param>
        /// <returns>True if the WebP image header is valid, otherwise false</returns>
        public bool GetInfo(string path, out int imgWidth, out int imgHeight)
        {
            var retValue = false;
            var width = 0;
            var height = 0;
            var pnt = IntPtr.Zero;

            try
            {
                var data = Utilities.CopyFileToManagedArray(path);
                pnt = Utilities.CopyDataToUnmanagedMemory(data);
                var ret = NativeWebPDecoder.WebPGetInfo(pnt, (uint) data.Length, ref width, ref height);
                if (ret == 1)
                {
                    retValue = true;
                }
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pnt);
            }

            imgWidth = width;
            imgHeight = height;
            return retValue;
        }

        /// <summary>
        /// Decode the WebP image into a RGB Bitmap
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <returns>A Bitmap object with the decoded WebP image.
        /// Note that a Bitmap object use the BGR format, so if you display the Bitmap in a picturebox red and blue are mixed up</returns>
        public static Bitmap DecodeRGB(string path)
        {
            return Decode(path, DecodeType.RGB, PixelFormat.Format24bppRgb);
        }

        /// <summary>
        /// Decode the WebP image into a RGBA Bitmap
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <returns>A Bitmap object with the decoded WebP image.
        /// Note that a Bitmap object use the ABGR format, so if you display the Bitmap in a picturebox red and blue are mixed up</returns>
        public static Bitmap DecodeRGBA(string path)
        {
            return Decode(path, DecodeType.RGBA, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Decode the WebP image into a BGR Bitmap
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <returns>A Bitmap object with the decoded WebP image</returns>
        public static Bitmap DecodeBGR(string path)
        {
            return Decode(path, DecodeType.BGR, PixelFormat.Format24bppRgb);
        }

        /// <summary>
        /// Decode the WebP image into a BGRA Bitmap
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <returns>A Bitmap object with the decoded WebP image</returns>
        public static Bitmap DecodeBGRA(string path)
        {
            return Decode(path, DecodeType.BGRA, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Decode the WebP image file into raw RGB image data
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="imgWidth">Returns the width of the WebP image</param>
        /// <param name="imgHeight">Returns the height of the WebP image</param>
        /// <returns>A byte array containing the raw decoded image data</returns>
        public static byte[] DecodeRGB(string path, out int imgWidth, out int imgHeight)
        {
            return Decode(path, DecodeType.RGB, PixelFormat.Format24bppRgb, out imgWidth, out imgHeight);
        }

        /// <summary>
        /// Decode the WebP image file into raw RGBA image data
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="imgWidth">Returns the width of the WebP image</param>
        /// <param name="imgHeight">Returns the height of the WebP image</param>
        /// <returns>A byte array containing the raw decoded image data</returns>
        public static byte[] DecodeRGBA(string path, out int imgWidth, out int imgHeight)
        {
            return Decode(path, DecodeType.RGBA, PixelFormat.Format32bppArgb, out imgWidth, out imgHeight);
        }

        /// <summary>
        /// Decode the WebP image file into raw BGR image data
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="imgWidth">Returns the width of the WebP image</param>
        /// <param name="imgHeight">Returns the height of the WebP image</param>
        /// <returns>A byte array containing the raw decoded image data</returns>
        public static byte[] DecodeBGR(string path, out int imgWidth, out int imgHeight)
        {
            return Decode(path, DecodeType.BGR, PixelFormat.Format24bppRgb, out imgWidth, out imgHeight);
        }

        /// <summary>
        /// Decode the WebP image file into raw BGRA image data
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="imgWidth">Returns the width of the WebP image</param>
        /// <param name="imgHeight">Returns the height of the WebP image</param>
        /// <returns>A byte array containing the raw decoded image data</returns>
        public static byte[] DecodeBGRA(string path, out int imgWidth, out int imgHeight)
        {
            return Decode(path, DecodeType.BGRA, PixelFormat.Format32bppArgb, out imgWidth, out imgHeight);
        }

        /// <summary>
        /// Internal convert method to get a Bitmap from a WebP image file
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="type">The color type you want to convert to</param>
        /// <param name="format">The PixelFormat the Bitmap should use</param>
        /// <returns></returns>
        private static Bitmap Decode(string path, DecodeType type, PixelFormat format)
        {
            var data = Decode(path, type, format, out var width, out var height);
            return Utilities.ConvertDataToBitmap(data, width, height, format);
        }

        /// <summary>
        /// Internal convert method to get a byte array from a WebP image file
        /// </summary>
        /// <param name="path">The path to the WebP image file</param>
        /// <param name="type">The color type you want to convert to</param>
        /// <param name="format">The PixelFormat you want to use</param>
        /// <param name="imgWidth">Returns the width of the WebP image</param>
        /// <param name="imgHeight">Returns the height of the WebP image</param>
        /// <returns></returns>
        private static byte[] Decode(string path, DecodeType type, PixelFormat format, out int imgWidth,
            out int imgHeight)
        {
            var width = 0;
            var height = 0;
            var data = IntPtr.Zero;
            var outputBuffer = IntPtr.Zero;
            var result = IntPtr.Zero;

            try
            {
                // Load data
                var managedData = Utilities.CopyFileToManagedArray(path);

                // Copy data to unmanaged memory
                data = Utilities.CopyDataToUnmanagedMemory(managedData);

                // Get image width and height
                NativeWebPDecoder.WebPGetInfo(data, (uint) managedData.Length, ref width, ref height);

                // Get image data lenght
                var dataSize = (uint) managedData.Length;

                // Calculate bitmap size for decoded WebP image
                var outputBufferSize = Utilities.CalculateBitmapSize(width, height, format);

                // Allocate unmanaged memory to decoded WebP image
                outputBuffer = Marshal.AllocHGlobal(outputBufferSize);

                // Calculate distance between scanlines
                var outputStride = width * Image.GetPixelFormatSize(format) / 8;

                // Convert image
                switch (type)
                {
                    case DecodeType.RGB:
                        result = NativeWebPDecoder.WebPDecodeRGBInto(data, dataSize,
                            outputBuffer, outputBufferSize, outputStride);
                        break;
                    case DecodeType.RGBA:
                        result = NativeWebPDecoder.WebPDecodeRGBAInto(data, dataSize,
                            outputBuffer, outputBufferSize, outputStride);
                        break;
                    case DecodeType.BGR:
                        result = NativeWebPDecoder.WebPDecodeBGRInto(data, dataSize,
                            outputBuffer, outputBufferSize, outputStride);
                        break;
                    case DecodeType.BGRA:
                        result = NativeWebPDecoder.WebPDecodeBGRAInto(data, dataSize,
                            outputBuffer, outputBufferSize, outputStride);
                        break;
                }

                // Set out values
                imgWidth = width;
                imgHeight = height;

                // Copy data back to managed memory and return
                return Utilities.GetDataFromUnmanagedMemory(result, outputBufferSize);
            }
            finally
            {
                // Free unmanaged memory
                Marshal.FreeHGlobal(data);
                Marshal.FreeHGlobal(outputBuffer);
            }
        }
    }
}