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
        private static List<ImageInfo> _imageInfoList;
        private static bool _anyFrameDirty;
        private static Thread _animationThread;
        private static readonly ReaderWriterLock RwImgListLock = new ReaderWriterLock();
        [ThreadStatic] private static int _threadWriterLockWaitCount;

        public static int AnimationDelay = 50;

        /// <summary>Advances the frame in the specified image. The new frame is drawn the next time the image is rendered. This method applies only to images with time-based frames.</summary>
        /// <param name="image">The <see cref="T:System.Drawing.Image" /> object for which to update frames.</param>
        public static void UpdateFrames(Image image)
        {
            if (!_anyFrameDirty || image == null || _imageInfoList == null || _threadWriterLockWaitCount > 0)
            {
                return;
            }
            RwImgListLock.AcquireReaderLock(-1);
            try
            {
                var flag1 = false;
                var flag2 = false;
                foreach (var imageInfo in _imageInfoList)
                {
                    if (imageInfo.Image == image)
                    {
                        if (imageInfo.FrameDirty)
                        {
                            lock (imageInfo.Image)
                            {
                                imageInfo.UpdateFrame();
                            }
                        }

                        flag2 = true;
                    }

                    if (imageInfo.FrameDirty)
                    {
                        flag1 = true;
                    }

                    if (flag1 & flag2)
                    {
                        break;
                    }
                }

                _anyFrameDirty = flag1;
            }
            finally
            {
                RwImgListLock.ReleaseReaderLock();
            }
        }

        /// <summary>Advances the frame in all images currently being animated. The new frame is drawn the next time the image is rendered.</summary>
        public static void UpdateFrames()
        {
            if (!_anyFrameDirty || _imageInfoList == null || _threadWriterLockWaitCount > 0)
            {
                return;
            }
            RwImgListLock.AcquireReaderLock(-1);
            try
            {
                foreach (var imageInfo in _imageInfoList)
                {
                    lock (imageInfo.Image)
                        imageInfo.UpdateFrame();
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

                if (_imageInfoList == null)
                {
                    _imageInfoList = new List<ImageInfo>();
                }
                imageInfo.FrameChangedHandler = onFrameChangedHandler;
                _imageInfoList.Add(imageInfo);
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
            if (image == null || _imageInfoList == null)
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
                for (var index = 0; index < _imageInfoList.Count; ++index)
                {
                    var imageInfo = _imageInfoList[index];
                    if (image != imageInfo.Image)
                    {
                        continue;
                    }

                    if (onFrameChangedHandler != imageInfo.FrameChangedHandler &&
                        (onFrameChangedHandler == null ||
                         !onFrameChangedHandler.Equals(imageInfo.FrameChangedHandler)))
                    {
                        break;
                    }
                    _imageInfoList.Remove(imageInfo);
                    break;
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
                    for (var index = 0; index < _imageInfoList.Count; ++index)
                    {
                        var imageInfo = _imageInfoList[index];
                        imageInfo.FrameTimer += 5;
                        
                        if (imageInfo.FrameTimer < imageInfo.FrameDelay(imageInfo.Frame))
                        {
                            continue;
                        }
                        
                        imageInfo.FrameTimer = 0;
                            
                        if (imageInfo.Frame + 1 < imageInfo.FrameCount)
                        {
                            ++imageInfo.Frame;
                        }
                        else
                        {
                            imageInfo.Frame = 0;
                        }
                            
                        if (imageInfo.FrameDirty)
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
                    var propertyItem = image.GetPropertyItem(20736);
                    if (propertyItem != null)
                    {
                        var numArray = propertyItem.Value;
                        frameDelay = new int[FrameCount];
                        for (var index = 0; index < FrameCount; ++index)
                        {
                            frameDelay[index] = numArray[index * 4] + 256 * numArray[index * 4 + 1] +
                                             65536 * numArray[index * 4 + 2] +
                                             16777216 * numArray[index * 4 + 3];
                            
                        }
                    }
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

            protected void OnFrameChanged(EventArgs e)
            {
                FrameChangedHandler?.Invoke(Image, e);
            }
        }
    }
}