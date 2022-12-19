using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImgBrowser.Helpers
{
    /// <summary>
    /// Direct access to GDI and GDI+ methods.
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
        private static extern int GdipCloneImage(HandleRef image, out IntPtr cloneimage);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
            int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc,  TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", ExactSpelling=true, SetLastError=true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling=true, SetLastError=true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling=true, SetLastError=true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling=true, SetLastError=true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool StretchBlt(IntPtr hdcDest, int nXDest, int nYDest, int nDestWidth, int nDestHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int nSrcWidth, int nSrcHeight, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll")]
        public static extern bool SetStretchBltMode(IntPtr hdc, StretchMode iStretchMode);

        [DllImport("gdi32.dll", EntryPoint="GdiAlphaBlend")]
        public static extern bool AlphaBlend(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest,
            IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            BLENDFUNCTION blendFunction);

        public enum TernaryRasterOperations : uint {
            SRCCOPY     = 0x00CC0020,
            SRCPAINT    = 0x00EE0086,
            SRCAND      = 0x008800C6,
            SRCINVERT   = 0x00660046,
            SRCERASE    = 0x00440328,
            NOTSRCCOPY  = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY   = 0x00C000CA,
            MERGEPAINT  = 0x00BB0226,
            PATCOPY     = 0x00F00021,
            PATPAINT    = 0x00FB0A09,
            PATINVERT   = 0x005A0049,
            DSTINVERT   = 0x00550009,
            BLACKNESS   = 0x00000042,
            WHITENESS   = 0x00FF0062,
            CAPTUREBLT  = 0x40000000 //only if WinVer >= 5.0.0 (see wingdi.h)
        }

        public enum StretchMode : int {
            BLACKONWHITE = 1,
            WHITEONBLACK = 2,
            COLORONCOLOR = 3,
            HALFTONE     = 4,
            MAXSTRETCHBLTMODE = 4
        }

        private enum GdipImageTypeEnum 
        {
            Unknown = 0,
            Bitmap = 1,
            Metafile = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            byte BlendOp;
            byte BlendFlags;
            byte SourceConstantAlpha;
            byte AlphaFormat;

            public BLENDFUNCTION(byte op, byte flags, byte alpha, byte format)
            {
                BlendOp = op;
                BlendFlags = flags;
                SourceConstantAlpha = alpha;
                AlphaFormat = format;
            }
        }

        //
        // currently defined blend operation
        //
        const int AC_SRC_OVER = 0x00;

        //
        // currently defined alpha format
        //
        const int AC_SRC_ALPHA = 0x01;

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

        public static void DrawAndResizeBitmapToRectangle(Graphics graphics, Bitmap sourceImage, Rectangle targetRectangle)
        {
            var pTarget = graphics.GetHdc();
            var pSource = CreateCompatibleDC(pTarget);
            var pOrig = SelectObject(pSource, sourceImage.GetHbitmap());

            // GdiApi.BitBlt(pTarget, rect.X,rect.Y, rect.Width, rect.Height, pSource,0,0,TernaryRasterOperations.SRCCOPY);

            SetStretchBltMode(pTarget, StretchMode.HALFTONE);
            StretchBlt(pTarget, targetRectangle.X,targetRectangle.Y, targetRectangle.Width, targetRectangle.Height, pSource,0,0, sourceImage.Width, sourceImage.Height, GdiApi.TernaryRasterOperations.SRCCOPY);

            var pNew = SelectObject(pSource, pOrig);
            DeleteObject(pNew);
            DeleteDC(pSource);
            graphics.ReleaseHdc(pTarget);
        }

        public static void DrawTransparentBitmapToRectangle(Graphics graphics, Bitmap sourceImage, Rectangle targetRectangle)
        {
            var pTarget = graphics.GetHdc();
            var pSource = CreateCompatibleDC(pTarget);
            var pOrig = SelectObject(pSource, sourceImage.GetHbitmap());

            var blend = new BLENDFUNCTION(AC_SRC_OVER, 0, 255, AC_SRC_ALPHA);
            var size = new Size(sourceImage.Width, sourceImage.Height);
            var pointSource = new Point(0, 0);
            var pointTarget = new Point(targetRectangle.X, targetRectangle.Y);

            SetStretchBltMode(pTarget, StretchMode.HALFTONE);
            var blendResult = AlphaBlend(pTarget, pointTarget.X, pointTarget.Y, targetRectangle.Width, targetRectangle.Height, pSource, pointSource.X, pointSource.Y, size.Width, size.Height, blend);

            var pNew = SelectObject(pSource, pOrig);
            DeleteObject(pNew);
            DeleteDC(pSource);
            graphics.ReleaseHdc(pTarget);
        }
    }
}