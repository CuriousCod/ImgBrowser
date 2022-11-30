using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using ImgBrowser.Helpers;

namespace ImgBrowser.CustomControls
{
    /// <summary>Represents a Windows picture box control for displaying an image.</summary>
    /// <filterpriority>1</filterpriority>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [DefaultProperty("Image")]
    [DefaultBindingProperty("Image")]
    [Docking(DockingBehavior.Ask)]
    [Designer(
        "System.Windows.Forms.Design.PictureBoxDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class PictureSquare : Control, ISupportInitialize
    {
        private BorderStyle borderStyle;
        private Image image;
        private PictureBoxSizeMode sizeMode;
        private Size savedSize;
        private bool currentlyAnimating;
        private AsyncOperation currentAsyncLoadOperation;
        private string imageLocation;
        private Image initialImage;
        private Image errorImage;
        private int contentLength;
        private int totalBytesRead;
        private MemoryStream tempDownloadStream;
        private const int readBlockSize = 4096;
        private byte[] readBuffer;
        private ImageInstallationType imageInstallationType;
        private SendOrPostCallback loadCompletedDelegate;
        private SendOrPostCallback loadProgressDelegate;
        private bool handleValid;
        private object internalSyncObject = new object();
        private Image defaultInitialImage;
        private Image defaultErrorImage;
        [ThreadStatic] private static Image defaultInitialImageForThread = (Image) null;
        [ThreadStatic] private static Image defaultErrorImageForThread = (Image) null;
        private static readonly object defaultInitialImageKey = new object();
        private static readonly object defaultErrorImageKey = new object();
        private static readonly object loadCompletedKey = new object();
        private static readonly object loadProgressChangedKey = new object();
        private const int PICTUREBOXSTATE_asyncOperationInProgress = 1;
        private const int PICTUREBOXSTATE_cancellationPending = 2;
        private const int PICTUREBOXSTATE_useDefaultInitialImage = 4;
        private const int PICTUREBOXSTATE_useDefaultErrorImage = 8;
        private const int PICTUREBOXSTATE_waitOnLoad = 16;
        private const int PICTUREBOXSTATE_needToLoadImageLocation = 32;
        private const int PICTUREBOXSTATE_inInitialization = 64;
        private BitVector32 pictureBoxState;
        private StreamReader localImageStreamReader;
        private Stream uriImageStream;
        private static readonly object EVENT_SIZEMODECHANGED = new object();

        /// <summary>Initializes a new instance of the <see cref="T:ImgBrowser.Helpers.PictureBox" /> class.</summary>
        public PictureSquare()
        {
            // SetState2(2048, true);
            pictureBoxState = new BitVector32(12);
            SetStyle(ControlStyles.Opaque | ControlStyles.Selectable, false);
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
            TabStop = false;
            savedSize = Size;
        }

        /// <summary>Overrides the <see cref="P:System.Windows.Forms.Control.AllowDrop" /> property.</summary>
        /// <returns>true if drag-and-drop operations are allowed in the control; otherwise, false. The default is false.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool AllowDrop
        {
            get => base.AllowDrop;
            set => base.AllowDrop = value;
        }

        /// <summary>Indicates the border style for the control.</summary>
        /// <returns>One of the <see cref="T:System.Windows.Forms.BorderStyle" /> enumeration values. The default is <see cref="F:System.Windows.Forms.BorderStyle.None" />.</returns>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">The value assigned is not one of the <see cref="T:System.Windows.Forms.BorderStyle" /> values. </exception>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [DefaultValue(BorderStyle.None)]
        [DispId(-504)]
        public BorderStyle BorderStyle
        {
            get => borderStyle;
            set
            {
                if (value < BorderStyle.None || value > BorderStyle.Fixed3D)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int) value, typeof(BorderStyle));
                }

                if (borderStyle == value)
                {
                    return;
                }
                
                borderStyle = value;
                RecreateHandle();
                AdjustSize();
            }
        }

        private Uri CalculateUri(string path)
        {
            try
            {
                return new Uri(path);
            }
            catch (UriFormatException ex)
            {
                path = Path.GetFullPath(path);
                return new Uri(path);
            }
        }

        /// <summary>Cancels an asynchronous image load.</summary>
        /// <filterpriority>2</filterpriority>
        public void CancelAsync() => pictureBoxState[2] = true;

        /// <summary>Overrides the <see cref="P:System.Windows.Forms.Control.CausesValidation" /> property.</summary>
        /// <returns>true if the control causes validation to be performed on any controls requiring validation when it receives focus; otherwise, false. The default is true.</returns>
        /// <filterpriority>2</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new bool CausesValidation
        {
            get => base.CausesValidation;
            set => base.CausesValidation = value;
        }

        /// <summary>Overrides the <see cref="E:System.Windows.Forms.Control.CausesValidationChanged" /> property.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler CausesValidationChanged
        {
            add => base.CausesValidationChanged += value;
            remove => base.CausesValidationChanged -= value;
        }

        /// <summary>Overrides the <see cref="P:System.Windows.Forms.Control.CreateParams" /> property.</summary>
        /// <returns>A <see cref="T:System.Windows.Forms.CreateParams" /> that contains the required creation parameters when the handle to the control is created.</returns>
        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                CreateParams createParams = base.CreateParams;
                switch (borderStyle)
                {
                    case BorderStyle.FixedSingle:
                        createParams.Style |= 8388608;
                        break;
                    case BorderStyle.Fixed3D:
                        createParams.ExStyle |= 512;
                        break;
                }

                return createParams;
            }
        }

        /// <summary>Gets a value indicating the mode for Input Method Editor (IME) for the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</summary>
        /// <returns>Always <see cref="F:System.Windows.Forms.ImeMode.Disable" />.</returns>
        protected override ImeMode DefaultImeMode => ImeMode.Disable;

        /// <returns>The default <see cref="T:System.Drawing.Size" /> of the control.</returns>
        protected override Size DefaultSize => new Size(100, 50);

        /// <summary>Gets or sets the image to display when an error occurs during the image-loading process or if the image load is canceled.</summary>
        /// <returns>An <see cref="T:System.Drawing.Image" /> to display if an error occurs during the image-loading process or if the image load is canceled.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Localizable(true)]
        [RefreshProperties(RefreshProperties.All)]
        public Image ErrorImage
        {
            get
            {
                if (errorImage != null || !pictureBoxState[8])
                {
                    return errorImage;
                }
                
                if (defaultErrorImage == null)
                {
                    if (defaultErrorImageForThread == null)
                    {
                        defaultErrorImageForThread = new Bitmap(typeof(PictureBox), "ImageInError.bmp");
                    }
                    defaultErrorImage = defaultErrorImageForThread;
                }

                errorImage = defaultErrorImage;

                return errorImage;
            }
            set
            {
                if (ErrorImage != value)
                {
                    pictureBoxState[8] = false;
                }
                errorImage = value;
            }
        }

        /// <summary>Overrides the <see cref="P:System.Windows.Forms.Control.ForeColor" /> property.</summary>
        /// <returns>The foreground <see cref="T:System.Drawing.Color" /> of the control. The default is the value of the <see cref="P:System.Windows.Forms.Control.DefaultForeColor" /> property.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Color ForeColor
        {
            get => base.ForeColor;
            set => base.ForeColor = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.ForeColor" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ForeColorChanged
        {
            add => base.ForeColorChanged += value;
            remove => base.ForeColorChanged -= value;
        }

        /// <summary>Gets or sets the font of the text displayed by the control.</summary>
        /// <returns>The <see cref="T:System.Drawing.Font" /> to apply to the text displayed by the control. The default is the value of the <see cref="P:System.Windows.Forms.Control.DefaultFont" /> property.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Font Font
        {
            get => base.Font;
            set => base.Font = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.Font" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler FontChanged
        {
            add => base.FontChanged += value;
            remove => base.FontChanged -= value;
        }

        /// <summary>Gets or sets the image that is displayed by <see cref="T:ImgBrowser.Helpers.PictureBox" />.</summary>
        /// <returns>The <see cref="T:System.Drawing.Image" /> to display.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Localizable(true)]
        [Bindable(true)]
        public Image Image
        {
            get => image;
            set => InstallNewImage(value, ImageInstallationType.DirectlySpecified);
        }

        /// <summary>Gets or sets the path or URL for the image to display in the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</summary>
        /// <returns>The path or URL for the image to display in the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Localizable(true)]
        [DefaultValue(null)]
        [RefreshProperties(RefreshProperties.All)]
        public string ImageLocation
        {
            get => imageLocation;
            set
            {
                imageLocation = value;
                pictureBoxState[32] = !string.IsNullOrEmpty(imageLocation);
                
                if (string.IsNullOrEmpty(imageLocation) && imageInstallationType != ImageInstallationType.DirectlySpecified)
                {
                    InstallNewImage(null, ImageInstallationType.DirectlySpecified);
                }

                if (WaitOnLoad && !pictureBoxState[64] && !string.IsNullOrEmpty(imageLocation))
                {
                    Load();
                }
                
                Invalidate();
            }
        }

        private Rectangle ImageRectangle => ImageRectangleFromSizeMode(sizeMode);
        
        private Rectangle ImageRectangleFromSizeMode(PictureBoxSizeMode mode)
        {
            var rectangle = ControlUtils.DeflateRect(ClientRectangle, Padding);
            
            if (image == null)
            {
                return rectangle;
            }
            
            switch (mode)
            {
                case PictureBoxSizeMode.Normal:
                case PictureBoxSizeMode.AutoSize:
                    rectangle.Size = image.Size;
                    Size = rectangle.Size; // Added
                    break;
                case PictureBoxSizeMode.CenterImage:
                    rectangle.X += (rectangle.Width - image.Width) / 2;
                    rectangle.Y += (rectangle.Height - image.Height) / 2;
                    rectangle.Size = image.Size;
                    break;
                case PictureBoxSizeMode.Zoom:
                    Size size = image.Size;
                    double val1 = (double) ClientRectangle.Width / (double) size.Width;
                    Rectangle clientRectangle = ClientRectangle;
                    double val2 = (double) clientRectangle.Height / (double) size.Height;
                    float num1 = Math.Min((float) val1, (float) val2);
                    rectangle.Width = (int) ((double) size.Width * (double) num1);
                    rectangle.Height = (int) ((double) size.Height * (double) num1);
                    ref Rectangle local1 = ref rectangle;
                    clientRectangle = ClientRectangle;
                    int num2 = (clientRectangle.Width - rectangle.Width) / 2;
                    local1.X = num2;
                    ref Rectangle local2 = ref rectangle;
                    clientRectangle = ClientRectangle;
                    int num3 = (clientRectangle.Height - rectangle.Height) / 2;
                    local2.Y = num3;
                    break;
            }

            return rectangle;
        }

        /// <summary>Gets or sets the image displayed in the <see cref="T:ImgBrowser.Helpers.PictureBox" /> control when the main image is loading.</summary>
        /// <returns>The <see cref="T:System.Drawing.Image" /> displayed in the picture box control when the main image is loading.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Localizable(true)]
        [RefreshProperties(RefreshProperties.All)]
        public Image InitialImage
        {
            get
            {
                if (initialImage != null || !pictureBoxState[4])
                {
                    return initialImage;
                }
                
                if (defaultInitialImage == null)
                {
                    if (defaultInitialImageForThread == null)
                    {
                        defaultInitialImageForThread = new Bitmap(typeof(PictureBox), "PictureBox.Loading.bmp");
                        
                    }
                    defaultInitialImage = defaultInitialImageForThread;
                }

                initialImage = defaultInitialImage;

                return initialImage;
            }
            set
            {
                if (InitialImage != value)
                {
                    pictureBoxState[4] = false;
                }
                
                initialImage = value;
            }
        }

        private void InstallNewImage(Image value, ImageInstallationType installationType)
        {
            StopAnimate();
            image = value;
            
            ControlUtils.DoLayoutIf(AutoSize,  this,  this, nameof(Image));

            Animate();
            
            if (installationType != ImageInstallationType.ErrorOrInitial)
            {
                AdjustSize();
            }
            
            imageInstallationType = installationType;
            
            Invalidate();
            
            ControlUtils.xClearPreferredSizeCache(this);
        }

        /// <summary>Gets or sets the Input Method Editor(IME) mode supported by this control.</summary>
        /// <returns>One of the <see cref="T:System.Windows.Forms.ImeMode" /> values.</returns>
        /// <filterpriority>2</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new ImeMode ImeMode
        {
            get => base.ImeMode;
            set => base.ImeMode = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.ImeMode" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ImeModeChanged
        {
            add => base.ImeModeChanged += value;
            remove => base.ImeModeChanged -= value;
        }

        /// <summary>Displays the image specified by the <see cref="P:ImgBrowser.Helpers.PictureBox.ImageLocation" /> property of the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// <see cref="P:ImgBrowser.Helpers.PictureBox.ImageLocation" /> is null or an empty string.</exception>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        public void Load()
        {
            if (string.IsNullOrEmpty(imageLocation))
            {
                throw new InvalidOperationException("PictureBoxNoImageLocation");
            }
            
            pictureBoxState[32] = false;
            
            var installationType = ImageInstallationType.FromUrl;
            
            Image fromStream;
            try
            {
                DisposeImageStream();
                var uri = CalculateUri(imageLocation);
                if (uri.IsFile)
                {
                    localImageStreamReader = new StreamReader(uri.LocalPath);
                    fromStream = Image.FromStream(localImageStreamReader.BaseStream);
                }
                else
                {
                    using (var webClient = new WebClient())
                    {
                        uriImageStream = webClient.OpenRead(uri.ToString());
                        fromStream = Image.FromStream(uriImageStream);
                    }
                }
            }
            catch
            {
                if (!DesignMode)
                {
                    throw;
                }

                fromStream = ErrorImage;
                installationType = ImageInstallationType.ErrorOrInitial;
            }

            InstallNewImage(fromStream, installationType);
        }

        /// <summary>Sets the <see cref="P:ImgBrowser.Helpers.PictureBox.ImageLocation" /> to the specified URL and displays the image indicated.</summary>
        /// <param name="url">The path for the image to display in the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</param>
        /// <exception cref="T:System.InvalidOperationException">
        /// <paramref name="url" /> is null or an empty string.</exception>
        /// <exception cref="T:System.Net.WebException">
        /// <paramref name="url" /> refers to an image on the Web that cannot be accessed.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="url" /> refers to a file that is not an image.</exception>
        /// <exception cref="T:System.IO.FileNotFoundException">
        /// <paramref name="url" /> refers to a file that does not exist.</exception>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        public void Load(string url)
        {
            ImageLocation = url;
            Load();
        }

        /// <summary>Loads the image asynchronously.</summary>
        /// <filterpriority>2</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        /// </PermissionSet>
        public void LoadAsync()
        {
            if (string.IsNullOrEmpty(imageLocation))
            {
                throw new InvalidOperationException("PictureBoxNoImageLocation");
            }

            if (pictureBoxState[1])
            {
                return;
            }
            pictureBoxState[1] = true;

            if ((Image == null || imageInstallationType == ImageInstallationType.ErrorOrInitial) && InitialImage != null)
            {
                InstallNewImage(InitialImage, ImageInstallationType.ErrorOrInitial);
            }
            
            currentAsyncLoadOperation = AsyncOperationManager.CreateOperation((object) null);
            
            if (loadCompletedDelegate == null)
            {
                loadCompletedDelegate = LoadCompletedDelegate;
                loadProgressDelegate = LoadProgressDelegate;
                readBuffer = new byte[4096];
            }

            pictureBoxState[32] = false;
            pictureBoxState[2] = false;
            contentLength = -1;
            tempDownloadStream = new MemoryStream();
            
            new WaitCallback(BeginGetResponseDelegate).BeginInvoke(WebRequest.Create(CalculateUri(imageLocation)), null, null);
        }

        private void BeginGetResponseDelegate(object arg)
        {
            var state = (WebRequest) arg;
            state.BeginGetResponse(GetResponseCallback, state);
        }

        private void PostCompleted(Exception error, bool cancelled)
        {
            var asyncLoadOperation = currentAsyncLoadOperation;
            currentAsyncLoadOperation = null;
            asyncLoadOperation?.PostOperationCompleted(loadCompletedDelegate, new AsyncCompletedEventArgs(error, cancelled, null));
        }

        private void LoadCompletedDelegate(object arg)
        {
            var e = (AsyncCompletedEventArgs) arg;
            var fromStream = ErrorImage;
            
            var installationType = ImageInstallationType.ErrorOrInitial;
            
            if (!e.Cancelled)
            {
                if (e.Error == null)
                {
                    try
                    {
                        fromStream = Image.FromStream(tempDownloadStream);
                        installationType = ImageInstallationType.FromUrl;
                    }
                    catch (Exception ex)
                    {
                        e = new AsyncCompletedEventArgs(ex, false, null);
                    }
                }
            }

            if (!e.Cancelled)
            {
                InstallNewImage(fromStream, installationType);
            }
            
            tempDownloadStream = null;
            pictureBoxState[2] = false;
            pictureBoxState[1] = false;
            OnLoadCompleted(e);
        }

        private void LoadProgressDelegate(object arg) => OnLoadProgressChanged((ProgressChangedEventArgs) arg);

        private void GetResponseCallback(IAsyncResult result)
        {
            if (pictureBoxState[2])
            {
                PostCompleted(null, true);
            }
            else
            {
                try
                {
                    var response = ((WebRequest) result.AsyncState).EndGetResponse(result);
                    contentLength = (int) response.ContentLength;
                    totalBytesRead = 0;
                    var responseStream = response.GetResponseStream();
                    responseStream.BeginRead(readBuffer, 0, 4096, ReadCallBack, responseStream);
                }
                catch (Exception ex)
                {
                    PostCompleted(ex, false);
                }
            }
        }

        private void ReadCallBack(IAsyncResult result)
        {
            if (pictureBoxState[2])
            {
                PostCompleted(null, true);
            }
            else
            {
                var state = (Stream) result.AsyncState;
                try
                {
                    var count = state.EndRead(result);
                    if (count > 0)
                    {
                        totalBytesRead += count;
                        tempDownloadStream.Write(readBuffer, 0, count);
                        state.BeginRead(readBuffer, 0, 4096, ReadCallBack, state);
                        if (contentLength == -1)
                        {
                            return;
                        }
                        var progressPercentage = (int) (100.0 * ((double) totalBytesRead / (double) contentLength));
                        if (currentAsyncLoadOperation == null)
                        {
                            return;
                        }
                        currentAsyncLoadOperation.Post(loadProgressDelegate, new ProgressChangedEventArgs(progressPercentage, null));
                    }
                    else
                    {
                        tempDownloadStream.Seek(0L, SeekOrigin.Begin);
                        currentAsyncLoadOperation?.Post(loadProgressDelegate, new ProgressChangedEventArgs(100, null));
                        PostCompleted(null, false);
                        var stream = state;
                        state = null;
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    PostCompleted(ex, false);
                    state?.Close();
                }
            }
        }

        /// <summary>Loads the image at the specified location, asynchronously.</summary>
        /// <param name="url">The path for the image to display in the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</param>
        /// <filterpriority>2</filterpriority>
        public void LoadAsync(string url)
        {
            ImageLocation = url;
            LoadAsync();
        }

        /// <summary>Occurs when the asynchronous image-load operation is completed, been canceled, or raised an exception.</summary>
        /// <filterpriority>1</filterpriority>
        public event AsyncCompletedEventHandler LoadCompleted
        {
            add => Events.AddHandler(loadCompletedKey, value);
            remove => Events.RemoveHandler(loadCompletedKey, value);
        }

        /// <summary>Occurs when the progress of an asynchronous image-loading operation has changed.</summary>
        /// <filterpriority>1</filterpriority>
        public event ProgressChangedEventHandler LoadProgressChanged
        {
            add => Events.AddHandler(loadProgressChangedKey, value);
            remove => Events.RemoveHandler(loadProgressChangedKey, value);
        }

        private void ResetInitialImage()
        {
            pictureBoxState[4] = true;
            initialImage = defaultInitialImage;
        }

        private void ResetErrorImage()
        {
            pictureBoxState[8] = true;
            errorImage = defaultErrorImage;
        }

        private void ResetImage() => InstallNewImage(null, ImageInstallationType.DirectlySpecified);

        /// <summary>Gets or sets a value indicating whether control's elements are aligned to support locales using right-to-left languages.</summary>
        /// <returns>One of the <see cref="T:System.Windows.Forms.RightToLeft" /> values.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override RightToLeft RightToLeft
        {
            get => base.RightToLeft;
            set => base.RightToLeft = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.RightToLeft" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler RightToLeftChanged
        {
            add => base.RightToLeftChanged += value;
            remove => base.RightToLeftChanged -= value;
        }

        private bool ShouldSerializeInitialImage() => !pictureBoxState[4];

        private bool ShouldSerializeErrorImage() => !pictureBoxState[8];

        private bool ShouldSerializeImage() => imageInstallationType == ImageInstallationType.DirectlySpecified &&
                                               Image != null;

        /// <summary>Indicates how the image is displayed.</summary>
        /// <returns>One of the <see cref="T:System.Windows.Forms.PictureBoxSizeMode" /> values. The default is <see cref="F:System.Windows.Forms.PictureBoxSizeMode.Normal" />.</returns>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">The value assigned is not one of the <see cref="T:System.Windows.Forms.PictureBoxSizeMode" /> values. </exception>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [DefaultValue(PictureBoxSizeMode.Normal)]
        [Localizable(true)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public PictureBoxSizeMode SizeMode
        {
            get => sizeMode;
            set
            {
                if (value < PictureBoxSizeMode.Normal || value > PictureBoxSizeMode.Zoom)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int) value, typeof(PictureBoxSizeMode));
                }

                if (sizeMode == value)
                {
                    return;
                }
                
                if (value == PictureBoxSizeMode.AutoSize)
                {
                    AutoSize = true;
                    SetStyle(ControlStyles.FixedWidth | ControlStyles.FixedHeight, true);
                }

                if (value != PictureBoxSizeMode.AutoSize)
                {
                    AutoSize = false;
                    SetStyle(ControlStyles.FixedWidth | ControlStyles.FixedHeight, false);
                    savedSize = Size;
                }

                sizeMode = value;
                AdjustSize();
                Invalidate();
                OnSizeModeChanged(EventArgs.Empty);
            }
        }

        /// <summary>Occurs when <see cref="P:ImgBrowser.Helpers.PictureBox.SizeMode" /> changes.</summary>
        /// <filterpriority>1</filterpriority>
        public event EventHandler SizeModeChanged
        {
            add => Events.AddHandler(EVENT_SIZEMODECHANGED, (Delegate) value);
            remove => Events.RemoveHandler(EVENT_SIZEMODECHANGED, (Delegate) value);
        }

        /// <summary>Gets or sets a value that indicates whether the user can give the focus to this control by using the TAB key.</summary>
        /// <returns>true if the user can give the focus to the control by using the TAB key; otherwise, false. The default is true.</returns>
        /// <filterpriority>1</filterpriority>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new bool TabStop
        {
            get => base.TabStop;
            set => base.TabStop = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.TabStop" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler TabStopChanged
        {
            add => base.TabStopChanged += value;
            remove => base.TabStopChanged -= value;
        }

        /// <summary>Gets or sets the tab index value.</summary>
        /// <returns>The tab index value.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new int TabIndex
        {
            get => base.TabIndex;
            set => base.TabIndex = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.TabIndex" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler TabIndexChanged
        {
            add => base.TabIndexChanged += value;
            remove => base.TabIndexChanged -= value;
        }

        /// <summary>Gets or sets the text of the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</summary>
        /// <returns>The text of the <see cref="T:ImgBrowser.Helpers.PictureBox" />.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        public override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        /// <summary>Occurs when the value of the <see cref="P:ImgBrowser.Helpers.PictureBox.Text" /> property changes.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler TextChanged
        {
            add => base.TextChanged += value;
            remove => base.TextChanged -= value;
        }

        /// <summary>Overrides the <see cref="E:System.Windows.Forms.Control.Enter" /> property.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Enter
        {
            add => base.Enter += value;
            remove => base.Enter -= value;
        }

        /// <summary>Occurs when a key is released when the control has focus. </summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event KeyEventHandler KeyUp
        {
            add => base.KeyUp += value;
            remove => base.KeyUp -= value;
        }

        /// <summary>Occurs when a key is pressed when the control has focus.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event KeyEventHandler KeyDown
        {
            add => base.KeyDown += value;
            remove => base.KeyDown -= value;
        }

        /// <summary>Occurs when a key is pressed when the control has focus.</summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event KeyPressEventHandler KeyPress
        {
            add => base.KeyPress += value;
            remove => base.KeyPress -= value;
        }

        /// <summary>Occurs when input focus leaves the <see cref="T:ImgBrowser.Helpers.PictureBox" />. </summary>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Leave
        {
            add => base.Leave += value;
            remove => base.Leave -= value;
        }

        private void AdjustSize()
        {
            Size = sizeMode == PictureBoxSizeMode.AutoSize ? PreferredSize : savedSize;
        }

        // private void Animate() => Animate(!DesignMode && Visible && Enabled && ParentInternal != null);
        private void Animate() => Animate(!DesignMode && Visible && Enabled);

        private void StopAnimate() => Animate(false);

        private void Animate(bool animate)
        {
            if (animate == currentlyAnimating)
            {
                return;
            }
            if (animate)
            {
                if (image == null)
                {
                    return;
                }
                GifAnimator.Animate(image, OnFrameChanged);
                currentlyAnimating = true;
            }
            else
            {
                if (image == null)
                {
                    return;
                }
                GifAnimator.StopAnimate(image, OnFrameChanged);
                currentlyAnimating = false;
            }
        }

        /// <summary>Releases the unmanaged resources used by the <see cref="T:ImgBrowser.Helpers.PictureBox" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release managed and unmanaged resources; false to release unmanaged resources only.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAnimate();
            }
            DisposeImageStream();
            base.Dispose(disposing);
        }

        private void DisposeImageStream()
        {
            if (localImageStreamReader != null)
            {
                localImageStreamReader.Dispose();
                localImageStreamReader = null;
            }

            if (uriImageStream == null)
            {
                return;
            }
            
            uriImageStream.Dispose();
            localImageStreamReader = null;
        }

        // internal Size GetPreferredSizeCore(Size proposedSize) => image == null ? new Size(100,100) : image.Size + (SizeFromClientSize(Size.Empty) + Padding.Size);

        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data. </param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Animate();
        }

        private void OnFrameChanged(object o, EventArgs e)
        {
            if (Disposing || IsDisposed)
            {
                return;
            }
            if (InvokeRequired && IsHandleCreated)
            {
                lock (internalSyncObject)
                {
                    if (!handleValid)
                    {
                        return;
                    }
                    
                    BeginInvoke(new EventHandler(OnFrameChanged), o, (object) e);
                }
            }
            else
            {
                Invalidate();
            }
        }

        /// <summary>Raises the <see cref="E:System.Windows.Forms.Control.HandleDestroyed" /> event.</summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected override void OnHandleDestroyed(EventArgs e)
        {
            lock (internalSyncObject)
                handleValid = false;
            base.OnHandleDestroyed(e);
        }

        /// <summary>Raises the <see cref="E:System.Windows.Forms.Control.HandleCreated" /> event.</summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data. </param>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected override void OnHandleCreated(EventArgs e)
        {
            lock (internalSyncObject)
                handleValid = true;
            base.OnHandleCreated(e);
        }

        /// <summary>Raises the <see cref="E:ImgBrowser.Helpers.PictureBox.LoadCompleted" /> event.</summary>
        /// <param name="e">An <see cref="T:System.ComponentModel.AsyncCompletedEventArgs" /> that contains the event data. </param>
        protected virtual void OnLoadCompleted(AsyncCompletedEventArgs e)
        {
            var completedEventHandler = (AsyncCompletedEventHandler) Events[loadCompletedKey];
            completedEventHandler?.Invoke(this, e);
        }

        /// <summary>Raises the <see cref="E:ImgBrowser.Helpers.PictureBox.LoadProgressChanged" /> event.</summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.ProgressChangedEventArgs" /> that contains the event data.</param>
        protected virtual void OnLoadProgressChanged(ProgressChangedEventArgs e)
        {
            var changedEventHandler = (ProgressChangedEventHandler) Events[loadProgressChangedKey];
            changedEventHandler?.Invoke(this, e);
        }

        /// <summary>Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event.</summary>
        /// <param name="pe">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data. </param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            if (pictureBoxState[32])
            {
                try
                {
                    if (WaitOnLoad)
                    {
                        Load();
                    }
                    else
                    {
                        LoadAsync();
                    }
                }
                catch (Exception ex)
                {
                    if (ControlUtils.IsCriticalException(ex))
                    {
                        throw;
                    }

                    image = ErrorImage;
                }
            }

            if (image != null)
            {
                Animate();
                GifAnimator.UpdateFrames(Image);

                var rect = imageInstallationType == ImageInstallationType.ErrorOrInitial
                    ? ImageRectangleFromSizeMode(PictureBoxSizeMode.CenterImage)
                    : ImageRectangle;
                pe.Graphics.DrawImage(image, rect);
            }

            base.OnPaint(pe);
        }

        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data. </param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            Animate();
        }

        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data. </param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            Animate();
        }

        /// <summary>Raises the <see cref="E:System.Windows.Forms.Control.Resize" /> event.</summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (sizeMode == PictureBoxSizeMode.Zoom || sizeMode == PictureBoxSizeMode.StretchImage || sizeMode == PictureBoxSizeMode.CenterImage || BackgroundImage != null)
            {
                Invalidate();
            }
            savedSize = Size;
        }

        /// <summary>Raises the <see cref="E:ImgBrowser.Helpers.PictureBox.SizeModeChanged" /> event.</summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data. </param>
        protected virtual void OnSizeModeChanged(EventArgs e)
        {
            if (!(Events[EVENT_SIZEMODECHANGED] is EventHandler eventHandler))
            {
                return;
            }
            
            eventHandler(this, e);
        }

        /// <summary>Returns a string that represents the current <see cref="T:ImgBrowser.Helpers.PictureBox" /> control.</summary>
        /// <returns>A string that represents the current <see cref="T:ImgBrowser.Helpers.PictureBox" />. </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => base.ToString() + ", SizeMode: " + sizeMode.ToString("G");

        /// <summary>Gets or sets a value indicating whether an image is loaded synchronously.</summary>
        /// <returns>true if an image-loading operation is completed synchronously; otherwise, false. The default is false.</returns>
        /// <filterpriority>2</filterpriority>
        [Localizable(true)]
        [DefaultValue(false)]
        public bool WaitOnLoad
        {
            get => pictureBoxState[16];
            set => pictureBoxState[16] = value;
        }

        /// <summary>Signals the object that initialization is starting.</summary>
        void ISupportInitialize.BeginInit() => pictureBoxState[64] = true;

        /// <summary>Signals to the object that initialization is complete.</summary>
        void ISupportInitialize.EndInit()
        {
            if (!string.IsNullOrEmpty(ImageLocation) && WaitOnLoad)
            {
                Load();
            }

            pictureBoxState[64] = false;
        }

        private enum ImageInstallationType
        {
            DirectlySpecified,
            ErrorOrInitial,
            FromUrl,
        }

        public static class ControlUtils
        {
            public static Rectangle DeflateRect(Rectangle rect, Padding padding)
            {
                rect.X += padding.Left;
                rect.Y += padding.Top;
                rect.Width -= padding.Horizontal;
                rect.Height -= padding.Vertical;
                return rect;
            }
            
            public static bool IsCriticalException(Exception ex)
            {
                switch (ex)
                {
                    case NullReferenceException _:
                    case StackOverflowException _:
                    case OutOfMemoryException _:
                    case ThreadAbortException _:
                    case ExecutionEngineException _:
                    case IndexOutOfRangeException _:
                        return true;
                    default:
                        return ex is AccessViolationException;
                }
            }
            
            public static void DoLayout(Control elementToLayout, Control elementCausingLayout, string property)
            {
                if (elementCausingLayout == null)
                {
                    return;
                }
                
                xClearPreferredSizeCache(elementCausingLayout);
                if (elementToLayout == null)
                {
                    return;
                }
                
                xClearPreferredSizeCache(elementToLayout);
                elementToLayout.PerformLayout(elementCausingLayout, property);
            }

            public static void DoLayoutIf(bool condition, Control elementToLayout, Control elementCausingLayout, string property)
            {
                if (!condition)
                {
                    if (elementCausingLayout == null)
                    {
                        return;
                    }
                    xClearPreferredSizeCache(elementCausingLayout);
                }
                else
                {
                    DoLayout(elementToLayout, elementCausingLayout, property);
                }
            }

            // TODO Figure out how to implement this
            public static void xClearPreferredSizeCache(Control element)
            {
                // element.Properties.SetSize(CommonProperties._preferredSizeCacheProperty, LayoutUtils.InvalidSize);
            }
        }
    }
}