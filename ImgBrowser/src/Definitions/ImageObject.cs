using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ImgBrowser.Helpers;
using ImgBrowser.Helpers.WebpSupport;

namespace ImgBrowser
{
    public class ImageObject
    {
        public string FullFilename;
        public Bitmap Image => image ?? (image = LoadFileAsBitmap(FullFilename));
        public string Name => FullFilename == "" ? "" : System.IO.Path.GetFileName(FullFilename);
        public string Path => FullFilename == "" ? "" : System.IO.Path.GetDirectoryName(FullFilename)?.TrimEnd('\\');
        public bool IsFile => File.Exists(FullFilename);

        private Bitmap image;
        private bool imageValidated;
        private IntPtr imagePtr;

        private Bitmap[] bitmapFrames = { };
        private int frameIndex;
        
        public int FrameCount;
        
        public ImageObject(string file)
        {
            FullFilename = file;
        }

        private Bitmap LoadFileAsBitmap(string file)
        {
            try
            {
                if (!file.EndsWith(".webp"))
                {
                    return (Bitmap) GdiApi.GetImage(file, ref imagePtr);    
                }

                return !NativeWebPDecoder.IsDllAvailable() ? null : WebPDecoder.DecodeBGRA(file);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            // This actually doesn't catch these errors, since it also requires HandleProcessCorruptedStateExceptions 
            // Got this one while trying to access corrupt image
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/4de25cc0-9235-4e40-9cd7-d7c934d78cc6/sehexception-is-not-caught-in-managed-code-windows-just-kills-the-process?forum=clr
            catch (SEHException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Copies current image to RAM
        /// Will speed up image rendering (redraws faster) in picture box autosize mode
        /// </summary>
        public void CopyImageToMemory()
        {
            if (imageValidated)
            {
                return;
            }
            
            if (imagePtr == IntPtr.Zero)
            {
                return;
            }
            
            GdiApi.ValidateImage(imagePtr);
            
            imageValidated = true;
        }
        
        public bool IsAnimated()
        {
            if (!Name.EndsWith(".gif"))
            {
                return false;
            }

            if (FrameCount != 0)
            {
                return true;
            }
            
            FrameCount = Image.GetFrameCount(FrameDimension.Time);
            bitmapFrames = new Bitmap[FrameCount];

            return true;
        }
        
        public Task<bool> GenerateBitmapsFromFrames(CancellationToken token)
        {
            if (imagePtr == IntPtr.Zero)
            {
                return Task.FromResult(false);
            }
            
            GdiApi.CloneImage(imagePtr, out var cloneImagePtr);
            var clonedImage = (Bitmap) GdiApi.CreateImageObject(cloneImagePtr);
            
            return Task.Run(() =>
            {
                if (clonedImage == null)
                {
                    return false;
                }

                var frames = new Bitmap[FrameCount];
                var cloningTasks = new Task[FrameCount];

                for (var i = 0; i < FrameCount; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }
                    
                    GdiApi.CloneImage(cloneImagePtr, out var iterationClonePtr);
                    var iterationClone = (Bitmap) GdiApi.CreateImageObject(iterationClonePtr);
                    cloningTasks[i] = CloneBitmapFrameToBitmapArrayAsync(ref frames, i, iterationClone, iterationClonePtr, token);
                }
                
                Task.WaitAll(cloningTasks);
                
                bitmapFrames = frames;
                return true;

            }, token);
        }
        
        private static Task CloneBitmapFrameToBitmapArrayAsync(ref Bitmap[] targetArray, int arrayIndex, Image sourceImage, IntPtr sourcePtr, CancellationToken token)
        {
            var bitmaps = targetArray;
            return Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                
                sourceImage.SelectActiveFrame(FrameDimension.Time, arrayIndex);

                GdiApi.CloneImage(sourcePtr, out var clonePtr2);
                bitmaps[arrayIndex] = new Bitmap(GdiApi.CreateImageObject(clonePtr2));

            }, token);
        }

        public Bitmap GetNextFrame()
        {
            if (FrameCount == 0)
            {
                return Image;
            }
            
            var nextFrame = IncrementFrame();

            if (bitmapFrames[nextFrame] != null)
            {
                return bitmapFrames[nextFrame];
            }
            
            bitmapFrames[nextFrame] = LoadFrameAtIndex(nextFrame);

            return bitmapFrames[nextFrame];
        }
        
        public Bitmap LoadFrameAtIndex(int index)
        {
            if (imagePtr == IntPtr.Zero)
            {
                return null;
            }
            
            if (FrameCount == 0)
            {
                return null;
            }
            
            if (index < 0 || index >= FrameCount)
            {
                return null;
            }
            
            GdiApi.CloneImage(imagePtr, out var clonePtr);
            var clonedBitmap = (Bitmap) GdiApi.CreateImageObject(clonePtr);
            
            clonedBitmap.SelectActiveFrame(FrameDimension.Time, index);
            
            return new Bitmap(GdiApi.CreateImageObject(clonePtr));
        }

        private int IncrementFrame()
        {
            if (FrameCount == 0)
            {
                return 0;
            }
            
            frameIndex++;
            
            if (frameIndex >= FrameCount)
            {
                frameIndex = 0;
            }
            
            return frameIndex;
        }
        
        /// <summary>
        /// Gets the delay between frames in a gif
        /// </summary>
        /// <returns> Delay in milliseconds </returns>
        public int GetFrameDelay()
        {
            var frameDelay = Image.GetPropertyItem (0x5100);
            return (frameDelay.Value [0] + frameDelay.Value [1] * 256) * 10;
        }
    }
}
