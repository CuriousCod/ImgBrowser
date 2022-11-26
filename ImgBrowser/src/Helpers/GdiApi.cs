using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImgBrowser.Helpers
{
    /// <summary>
    /// Direct access to GDI+ functions.
    /// </summary>
    public static class GdiApi
    {
        [DllImport("gdiplus.dll", ExactSpelling=true, CharSet=CharSet.Unicode)]
        private static extern int GdipGetImageType(IntPtr image, out GdipImageTypeEnum type);

        [DllImport("gdiplus.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);
        
        [DllImport("gdiplus.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GdipImageForceValidation(HandleRef image);
        
        [DllImport("gdiplus.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GdipCloneImage(HandleRef image, out IntPtr cloneimage);

        private enum GdipImageTypeEnum 
        {
            Unknown = 0,
            Bitmap = 1,
            Metafile = 2
        }
        
        /// <summary>
        /// Loads an image using GDI+ directly
        /// <para>Skips copying image into memory which reduces image load time by around 90 %</para>
        /// </summary>
        /// <param name="filename">Full path to the image</param>
        /// <param name="imagePtr">Stores the pointer to the image</param>
        /// <returns>Image. Null if image could not be loaded</returns>
        public static Image GetImage(string filename, ref IntPtr imagePtr)
        {
            // Optional?
            // IntSecurity.DemandReadFileIO(filename);
            
            filename = Path.GetFullPath(filename);
            var bitmapFromFile = GdipCreateBitmapFromFile(filename, out var bitmap);
            
            if (bitmapFromFile != 0)
            {
                return null;
                // throw new Exception("GdipCreateBitmapFromFile failed with error code " + bitmapFromFile);
            }
            
            // Optional
            if ( GdipGetImageType(bitmap, out var imageType) != 0 ) 
            {
                return null;
            }
            
            imagePtr = bitmap;
            
            switch(imageType) 
            {
                case GdipImageTypeEnum.Bitmap:
                    return (Bitmap) typeof(Bitmap).InvokeMember("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { bitmap });
                case GdipImageTypeEnum.Metafile:
                    return (Metafile) typeof(Metafile).InvokeMember("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { bitmap });
                case GdipImageTypeEnum.Unknown:
                default:
                    return null;
                    // throw new Exception("Unknown image type");
            }
        }
        
        /// <summary>
        /// Loads image into memory
        /// <para> Optional, Images are loaded really fast without this, but image repaint speed is slower</para>
        /// <para> https://stackoverflow.com/questions/60480461/gdi-drawimage-notably-slower-in-c-win32-than-in-c-sharp-winforms </para>
        /// </summary>
        /// <param name="imagePtr">Pointer to the image</param>
        /// <returns>None</returns>
        public static void ValidateImage(IntPtr imagePtr)
        {
            var status = GdipImageForceValidation(new HandleRef(null, imagePtr)); 
            if (status != 0)
            {
                throw new Exception("GdipImageForceValidation failed with error code " + status);
            }
        }
        
        public static int CloneImage(IntPtr imagePtr, out IntPtr cloneImagePtr)
        {
            return GdipCloneImage(new HandleRef(null, imagePtr), out cloneImagePtr);
        }

        public static Image CreateImageObject(IntPtr nativeImage)
        {
            GdipGetImageType(nativeImage, out var imageTypeEnum);
            
            switch (imageTypeEnum)
            {
                case GdipImageTypeEnum.Bitmap:
                    return (Bitmap) typeof(Bitmap).InvokeMember("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { nativeImage });
                case GdipImageTypeEnum.Metafile:
                    return (Metafile) typeof(Metafile).InvokeMember("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { nativeImage });
                case GdipImageTypeEnum.Unknown:
                default:
                    return null;
            }
        }
    }
}