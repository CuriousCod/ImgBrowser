using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImgBrowser.Helpers.WebpSupport
{
    public static class Utilities
    {
        /// <summary>
        /// Copy data from managed to unmanaged memory
        /// </summary>
        /// <param name="data">The data you want to copy</param>
        /// <returns>Pointer to the location of the unmanaged data</returns>
        public static IntPtr CopyDataToUnmanagedMemory(byte[] data)
        {
            // Initialize unmanaged memory to hold the array
            var size = Marshal.SizeOf(data[0]) * data.Length;
            var pnt = Marshal.AllocHGlobal(size);
            // Copy the array to unmanaged memory
            Marshal.Copy(data, 0, pnt, data.Length);
            return pnt;
        }

        /// <summary>
        /// Get data from unmanaged memory back to managed memory
        /// </summary>
        /// <param name="source">A Pointer where the data lifes</param>
        /// <param name="length">How many bytes you want to copy</param>
        /// <returns></returns>
        public static byte[] GetDataFromUnmanagedMemory(IntPtr source, int length)
        {
            // Initialize managed memory to hold the array
            var data = new byte[length];
            // Copy the array back to managed memory
            Marshal.Copy(source, data, 0, length);
            return data;
        }

        /// <summary>
        /// Get a file from the disk and copy it into a managed byte array
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns>A byte array containing the file</returns>
        public static byte[] CopyFileToManagedArray(string path)
        {
            // Load file from disk and copy it into a byte array
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var data = new byte[fs.Length];
            fs.Read(data, 0, (int) fs.Length);
            fs.Close();
            return data;
        }

        /// <summary>
        /// Convert raw image data into a Bitmap object
        /// </summary>
        /// <param name="data">The byte array containing the image data</param>
        /// <param name="imgWidth">The width of your image</param>
        /// <param name="imgHeight">The height of your image</param>
        /// <param name="format">The PixelFormat the Bitmap should use</param>
        /// <returns>The Bitmap object conating you image</returns>
        public static Bitmap ConvertDataToBitmap(byte[] data, int imgWidth, int imgHeight, PixelFormat format)
        {
            var bmp = new Bitmap(imgWidth, imgHeight, format);
            
            // Create a BitmapData and Lock all pixels to be written
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly,
                bmp.PixelFormat);
            
            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            
            bmp.UnlockBits(bmpData);
            
            return bmp;
        }

        /// <summary>
        /// Calculate the needed size for a bitmap
        /// </summary>
        /// <param name="imgWidth">The image width</param>
        /// <param name="imgHeight">The image height</param>
        /// <param name="format">The pixel format you want to use</param>
        /// <returns>The bitmap size in bytes</returns>
        public static int CalculateBitmapSize(int imgWidth, int imgHeight, PixelFormat format)
        {
            return imgWidth * imgHeight * Image.GetPixelFormatSize(format) / 8;
        }
    }
}