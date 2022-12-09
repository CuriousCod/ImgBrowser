using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace ImgBrowser.Helpers
{
    /// <summary>Animates an image that has time-based frames.</summary>
    public static class GifAnimator
    {
        private static ImageInfo _currentImageInfo;
        private static bool _anyFrameDirty;
        private static Thread _animationThread;
        private static readonly ReaderWriterLock RwImgListLock = new ReaderWriterLock();
        [ThreadStatic] private static int _threadWriterLockWaitCount;

        public static int AnimationDelay = 50;
        
        public static bool IsAnimated => _currentImageInfo?.Animated ?? false; 

        /// <summary>Advances the frame in the specified image. The new frame is drawn the next time the image is rendered. This method applies only to images with time-based frames.</summary>
        /// <param name="image">The <see cref="T:System.Drawing.Image" /> object for which to update frames.</param>
        public static void UpdateFrames(Image image)
        {
            if (!_anyFrameDirty || image == null || _currentImageInfo == null || _threadWriterLockWaitCount > 0)
            {
                return;
            }
            
            RwImgListLock.AcquireReaderLock(-1);
            try
            {
                lock (_currentImageInfo.Image)
                {
                    if (_currentImageInfo.Image == image)
                    {
                        if (_currentImageInfo.FrameDirty)
                        {
                            _currentImageInfo.UpdateFrame();
                        }
                    }
                    
                    _anyFrameDirty = _currentImageInfo.FrameDirty;
                }
            }
            finally
            {
                RwImgListLock.ReleaseReaderLock();
            }
        }

        /// <summary>Advances the frame in all images currently being animated. The new frame is drawn the next time the image is rendered.</summary>
        public static void UpdateFrames()
        {
            if (!_anyFrameDirty || _currentImageInfo == null || _threadWriterLockWaitCount > 0)
            {
                return;
            }
            RwImgListLock.AcquireReaderLock(-1);
            try
            {
                lock (_currentImageInfo)
                {
                    _currentImageInfo.UpdateFrame();
                }

                _anyFrameDirty = false;
            }
            finally
            {
                RwImgListLock.ReleaseReaderLock();
            }
        }

        /// <summary>Displays a multiple-frame image as an animation.</summary>
        /// <param name="image">The <see cref="T:System.Drawing.Image" /> object to animate.</param>
        /// <param name="onFrameChangedHandler">An <see langword="EventHandler" /> object that specifies the method that is called when the animation frame changes.</param>
        public static void Animate(Image image, EventHandler onFrameChangedHandler)
        {
            if (image == null)
            {
                return;
            }
            ImageInfo imageInfo;
            
            lock (image)
            {
                imageInfo = new ImageInfo(image);
            }
            
            StopAnimate(image, onFrameChangedHandler);
            var isReaderLockHeld = RwImgListLock.IsReaderLockHeld;
            var lockCookie = new LockCookie();
            ++_threadWriterLockWaitCount;
            
            try
            {
                if (isReaderLockHeld)
                {
                    lockCookie = RwImgListLock.UpgradeToWriterLock(-1);
                }
                else
                {
                    RwImgListLock.AcquireWriterLock(-1);
                }
            }
            finally
            {
                --_threadWriterLockWaitCount;
            }

            try
            {
                if (!imageInfo.Animated)
                {
                    return;
                }

                imageInfo.FrameChangedHandler = onFrameChangedHandler;
                
                if (_currentImageInfo == null)
                {
                    _currentImageInfo = imageInfo;
                }

                if (_animationThread != null)
                {
                    return;
                }
                
                _animationThread = new Thread(AnimateImages)
                {
                    Name = nameof(GifAnimator),
                    IsBackground = true
                };
                
                _animationThread.Start();
            }
            finally
            {
                if (isReaderLockHeld)
                {
                    RwImgListLock.DowngradeFromWriterLock(ref lockCookie);
                }
                else
                {
                    RwImgListLock.ReleaseWriterLock();
                }
            }
        }

        /// <summary>Returns a Boolean value indicating whether the specified image contains time-based frames.</summary>
        /// <param name="image">The <see cref="T:System.Drawing.Image" /> object to test.</param>
        /// <returns>This method returns <see langword="true" /> if the specified image contains time-based frames; otherwise, <see langword="false" />.</returns>
        public static bool CanAnimate(Image image)
        {
            if (image == null)
            {
                return false;
            }
            
            lock (image)
            {
                foreach (var frameDimensions in image.FrameDimensionsList)
                {
                    if (new FrameDimension(frameDimensions).Equals(FrameDimension.Time))
                    {
                        return image.GetFrameCount(FrameDimension.Time) > 1;
                    }
                }
            }

            return false;
        }

        /// <summary>Terminates a running animation.</summary>
        /// <param name="image">The <see cref="T:System.Drawing.Image" /> object to stop animating.</param>
        /// <param name="onFrameChangedHandler">An <see langword="EventHandler" /> object that specifies the method that is called when the animation frame changes.</param>
        public static void StopAnimate(Image image, EventHandler onFrameChangedHandler)
        {
            if (image == null || _currentImageInfo == null)
            {
                return;
            }
            
            var isReaderLockHeld = RwImgListLock.IsReaderLockHeld;
            var lockCookie = new LockCookie();
            ++_threadWriterLockWaitCount;
            
            try
            {
                if (isReaderLockHeld)
                {
                    lockCookie = RwImgListLock.UpgradeToWriterLock(-1);
                }
                else
                {
                    RwImgListLock.AcquireWriterLock(-1);
                }
            }
            finally
            {
                --_threadWriterLockWaitCount;
            }

            try
            {
                if (_currentImageInfo.Image == null)
                {
                    return;
                }

                if (_currentImageInfo.Image != image)
                {
                    return;
                }
                
                if (onFrameChangedHandler == _currentImageInfo.FrameChangedHandler && (onFrameChangedHandler != null || onFrameChangedHandler.Equals(_currentImageInfo.FrameChangedHandler)))
                {
                    _currentImageInfo = null;
                }
            }
            finally
            {
                if (isReaderLockHeld)
                {
                    RwImgListLock.DowngradeFromWriterLock(ref lockCookie);
                }
                else
                {
                    RwImgListLock.ReleaseWriterLock();
                }
            }
        }

        private static void AnimateImages()
        {
            while (true)
            {
                RwImgListLock.AcquireReaderLock(-1);
                try
                {
                    if (_currentImageInfo?.Image == null)
                    {
                        continue;
                    }

                    _currentImageInfo.FrameTimer += 5;
                    
                    if (_currentImageInfo.FrameTimer > _currentImageInfo.FrameDelay(_currentImageInfo.Frame))
                    {
                        _currentImageInfo.FrameTimer = 0;
                        
                        if (_currentImageInfo.Frame + 1 < _currentImageInfo.FrameCount)
                        {
                            ++_currentImageInfo.Frame;
                        }
                        else
                        {
                            _currentImageInfo.Frame = 0;
                        }
                        
                        if (_currentImageInfo.FrameDirty)
                        {
                            _anyFrameDirty = true;
                        }
                    }
                    
                }
                finally
                {
                    RwImgListLock.ReleaseReaderLock();
                }

                Thread.Sleep(AnimationDelay);
            }
        }

        private class ImageInfo
        {
            private const int PropertyTagFrameDelay = 20736;
            private int frame;
            private readonly int[] frameDelay;

            public ImageInfo(Image image)
            {
                Image = image;
                Animated = CanAnimate(image);
                
                if (Animated)
                {
                    FrameCount = image.GetFrameCount(FrameDimension.Time);
                    frameDelay = GetImageFrameDelays(image, FrameCount);
                }
                else
                {
                    FrameCount = 1;
                }

                if (frameDelay != null)
                {
                    return;
                }
                frameDelay = new int[FrameCount];
            }

            public bool Animated { get; }

            public int Frame
            {
                get => frame;
                set
                {
                    if (frame == value)
                    {
                        return;
                    }

                    if (value < 0 || value >= FrameCount)
                    {
                        throw new ArgumentException(@"InvalidFrame", nameof(value));
                    }

                    if (!Animated)
                    {
                        return;
                    }
                    
                    frame = value;
                    FrameDirty = true;
                    OnFrameChanged(EventArgs.Empty);
                }
            }

            public bool FrameDirty { get; private set; }

            public EventHandler FrameChangedHandler { get; set; }

            public int FrameCount { get; }

            public int FrameDelay(int thisFrame) => frameDelay[thisFrame];

            internal int FrameTimer { get; set; }

            internal Image Image { get; }

            internal void UpdateFrame()
            {
                if (!FrameDirty)
                {
                    return;
                }
                
                Image.SelectActiveFrame(FrameDimension.Time, Frame);
                FrameDirty = false;
            }

            private void OnFrameChanged(EventArgs e)
            {
                FrameChangedHandler?.Invoke(Image, e);
            }

            /// <summary>
            /// Gets the frame delays for an animated image.
            /// </summary>
            /// <param name="image"> Image to get frame delays from </param>
            /// <param name="frameCount"> Number of frames in the image</param>
            /// <returns> An array of integers that contains the delay, in milliseconds, for each frame in the image.</returns>
            private static int[] GetImageFrameDelays(Image image, int frameCount)
            {
                var frameDelay = new int[frameCount];
                
                var propertyItem = image.GetPropertyItem(PropertyTagFrameDelay);
                
                if (propertyItem == null)
                {
                    return frameDelay;
                }
                
                var numArray = propertyItem.Value;
                frameDelay = new int[frameCount];
                
                for (var index = 0; index < frameCount; ++index)
                {
                    frameDelay[index] = numArray[index * 4] + 256 * numArray[index * 4 + 1] +
                                        65536 * numArray[index * 4 + 2] +
                                        16777216 * numArray[index * 4 + 3];
                }

                return frameDelay;
            }
        }
    }
}