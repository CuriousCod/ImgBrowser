using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace JxlSharp
{
	public enum JxlOrientation
	{
		Identity = 1,
		FlipHorizontal = 2,
		Rotate180 = 3,
		FlipVertical = 4,
		Transpose = 5,
		Rotate90CW = 6,
		AntiTranspose = 7,
		Rotate90CCW = 8,
	}

	public enum JxlEndianness
	{
		NativeEndian = 0,
		LittleEndian = 1,
		BigEndian = 2,
	}

	public enum JxlDataType
	{
		Float = 0,
		UInt8 = 2,
		UInt16 = 3,
		Float16 = 5,
	}

	public struct JxlAnimationHeader
	{
		public JxlAnimationHeader(uint tpsNumerator, uint tpsDenominator, int numLoops, bool haveTimecodes)
		{
			this.TpsNumerator = tpsNumerator;
			this.TpsDenominator = tpsDenominator;
			this.NumLoops = numLoops;
			this.HaveTimecodes = haveTimecodes;
		}

		public uint TpsNumerator { get; set; }
		public uint TpsDenominator { get; set; }
		public int NumLoops { get; set; }
		public bool HaveTimecodes { get; set; }
	}

	[Flags]
	public enum JxlDecoderStatus
	{
		Success = 0,
		Error = 1,
		NeedMoreInput = 2,
		NeedPreviewOutBuffer = 3,
		NeedDcOutBuffer = 4,
		NeedImageOutBuffer = 5,
		JpegNeedMoreOutput = 6,
		BoxNeedMoreOutput = 7,
		BasicInfo = 0x40,
		Extensions = 0x80,
		ColorEncoding = 0x100,
		PreviewImage = 0x200,
		Frame = 0x400,

		DcImage = 0x800,

		FullImage = 0x1000,

		JpegReconstruction = 0x2000,

		Box = 0x4000,

		FrameProgression = 0x8000,
	}

	public class JxlPixelFormat
	{
		public JxlPixelFormat(System.Drawing.Imaging.PixelFormat pixelFormat)
		{
			this.NumChannels = JXL.GetBytesPerPixel(pixelFormat);
			this.DataType = JxlDataType.UInt8;
		}
		public JxlPixelFormat()
		{
			this.NumChannels = 3;
			this.DataType = JxlDataType.UInt8;
		}
		internal UnsafeNativeJxl.JxlPixelFormat pixelFormat;

		public int NumChannels
		{
			get
			{
				return (int)pixelFormat.num_channels;
			}
			set
			{
				pixelFormat.num_channels = (uint)value;
			}
		}

		public JxlDataType DataType
		{
			get
			{
				return (JxlDataType)pixelFormat.data_type;
			}
			set
			{
				pixelFormat.data_type = (UnsafeNativeJxl.JxlDataType)value;
			}
		}

		public JxlEndianness Endianness
		{
			get
			{
				return (JxlEndianness)pixelFormat.endianness;
			}
			set
			{
				pixelFormat.endianness = (UnsafeNativeJxl.JxlEndianness)value;
			}
		}

		public int Align
		{
			get
			{
				return (int)pixelFormat.align;
			}
			set
			{
				pixelFormat.align = (UIntPtr)value;
			}
		}
	}


	public class JxlBasicInfo
	{
		public JxlBasicInfo()
		{
			UnsafeNativeJxl.InitBasicInfo(out basicInfo);
			this.UsesOriginalProfile = true;
		}

		internal JxlBasicInfo(ref UnsafeNativeJxl.JxlBasicInfo basicInfo)
		{
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref basicInfo, this);
		}

		internal UnsafeNativeJxl.JxlBasicInfo basicInfo;

		public bool HaveContainer
		{
			get
			{
				return Convert.ToBoolean(basicInfo.have_container);
			}
			set
			{
				basicInfo.have_container = Convert.ToInt32(value);
			}
		}

		public int Width
		{
			get
			{
				return (int)basicInfo.xsize;
			}
			set
			{
				basicInfo.xsize = (uint)value;
			}
		}

		public int Height
		{
			get
			{
				return (int)basicInfo.ysize;
			}
			set
			{
				basicInfo.ysize = (uint)value;
			}
		}

		public int BitsPerSample
		{
			get
			{
				return (int)basicInfo.bits_per_sample;
			}
			set
			{
				basicInfo.bits_per_sample = (uint)value;
			}
		}

		public int ExponentBitsPerSample
		{
			get
			{
				return (int)basicInfo.exponent_bits_per_sample;
			}
			set
			{
				basicInfo.exponent_bits_per_sample = (uint)value;
			}
		}

		public float IntensityTarget
		{
			get
			{
				return basicInfo.intensity_target;
			}
			set
			{
				basicInfo.intensity_target = value;
			}
		}

		public float MinNits
		{
			get
			{
				return basicInfo.min_nits;
			}
			set
			{
				basicInfo.min_nits = value;
			}
		}

		public bool RelativeToMaxDisplay
		{
			get
			{
				return Convert.ToBoolean(basicInfo.relative_to_max_display);
			}
			set
			{
				basicInfo.relative_to_max_display = Convert.ToInt32(value);
			}
		}

		public float LinearBelow
		{
			get
			{
				return basicInfo.linear_below;
			}
			set
			{
				basicInfo.linear_below = value;
			}
		}

		public bool UsesOriginalProfile
		{
			get
			{
				return Convert.ToBoolean(basicInfo.uses_original_profile);
			}
			set
			{
				basicInfo.uses_original_profile = Convert.ToInt32(value);
			}
		}

		public bool HavePreview
		{
			get
			{
				return Convert.ToBoolean(basicInfo.have_preview);
			}
			set
			{
				basicInfo.have_preview = Convert.ToInt32(value);
			}
		}

		public bool HaveAnimation
		{
			get
			{
				return Convert.ToBoolean(basicInfo.have_animation);
			}
			set
			{
				basicInfo.have_animation = Convert.ToInt32(value);
			}
		}

		public JxlOrientation Orientation
		{
			get
			{
				return (JxlOrientation)basicInfo.orientation;
			}
			set
			{
				basicInfo.orientation = (UnsafeNativeJxl.JxlOrientation)value;
			}
		}

		public int NumColorChannels
		{
			get
			{
				return (int)basicInfo.num_color_channels;
			}
			set
			{
				basicInfo.num_color_channels = (uint)value;
			}
		}

		public int NumExtraChannels
		{
			get
			{
				return (int)basicInfo.num_extra_channels;
			}
			set
			{
				basicInfo.num_extra_channels = (uint)value;
			}
		}

		public int AlphaBits
		{
			get
			{
				return (int)basicInfo.alpha_bits;
			}
			set
			{
				basicInfo.alpha_bits = (uint)value;
			}
		}

		public int AlphaExponentBits
		{
			get
			{
				return (int)basicInfo.alpha_exponent_bits;
			}
			set
			{
				basicInfo.alpha_exponent_bits = (uint)value;
			}
		}

		public bool AlphaPremultiplied
		{
			get
			{
				return Convert.ToBoolean(basicInfo.alpha_premultiplied);
			}
			set
			{
				basicInfo.alpha_premultiplied = Convert.ToInt32(value);
			}
		}

		public Size Preview
		{
			get
			{
				return new Size((int)basicInfo.preview.xsize, (int)basicInfo.preview.ysize);
			}
			set
			{
				basicInfo.preview.xsize = (uint)value.Width;
				basicInfo.preview.ysize = (uint)value.Height;
			}
		}


		public JxlAnimationHeader Animation
		{
			get
			{
				return new JxlAnimationHeader()
				{
					TpsNumerator = basicInfo.animation.tps_numerator,
					TpsDenominator = basicInfo.animation.tps_denominator,
					HaveTimecodes = Convert.ToBoolean(basicInfo.animation.have_timecodes),
					NumLoops = (int)basicInfo.animation.num_loops
				};

			}
			set
			{
				basicInfo.animation.tps_numerator = value.TpsNumerator;
				basicInfo.animation.tps_denominator = value.TpsDenominator;
				basicInfo.animation.have_timecodes = Convert.ToInt32(value.HaveTimecodes);
				basicInfo.animation.num_loops = (uint)value.NumLoops;
			}
		}

		public int IntrinsicWidth
		{
			get
			{
				return (int)basicInfo.intrinsic_xsize;
			}
			set
			{
				basicInfo.intrinsic_xsize = (uint)value;
			}
		}

		public int IntrinsicHeight
		{
			get
			{
				return (int)basicInfo.intrinsic_ysize;
			}
			set
			{
				basicInfo.intrinsic_ysize = (uint)value;
			}
		}
	}

	public enum JxlColorSpace
	{
		RGB,
		Gray,
		XYB,
		Unknown,
	}

	public enum JxlWhitePoint
	{
		D65 = 1,
		Custom = 2,
		E = 10,
		DCI = 11,
	}

	public enum JxlPrimaries
	{
		SRGB = 1,
		Custom = 2,
		_2100 = 9,
		P3 = 11,
	}

	public enum JxlTransferFunction
	{
		_709 = 1,
		Unknown = 2,
		Linear = 8,
		SRGB = 13,
		PQ = 16,
		DCI = 17,
		HLG = 18,
		Gamma = 65535,
	}

	public enum JxlRenderingIntent
	{
		Perceptual = 0,
		Relative,
		Saturation,
		Absolute,
	}

	public struct XYValue
	{
		public XYValue(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}
		public double X { get; set; }
		public double Y { get; set; }
	}

	public class JxlColorEncoding
	{
		public void SetToSRGB(bool isGray)
		{
			UnsafeNativeJxl.JxlColorEncodingSetToSRGB(out this.colorEncoding, isGray);
		}
		public void SetToSRGB()
		{
			UnsafeNativeJxl.JxlColorEncodingSetToSRGB(out this.colorEncoding, false);
		}
		public void SetToLinearSRGB(bool isGray)
		{
			UnsafeNativeJxl.JxlColorEncodingSetToLinearSRGB(out this.colorEncoding, isGray);
		}
		public void SetToLinearSRGB()
		{
			UnsafeNativeJxl.JxlColorEncodingSetToLinearSRGB(out this.colorEncoding, false);
		}

		internal UnsafeNativeJxl.JxlColorEncoding colorEncoding;

		public JxlColorSpace ColorSpace
		{
			get
			{
				return (JxlColorSpace)colorEncoding.color_space;
			}
			set
			{
				colorEncoding.color_space = (UnsafeNativeJxl.JxlColorSpace)value;
			}
		}

		public JxlWhitePoint WhitePoint
		{
			get
			{
				return (JxlWhitePoint)colorEncoding.white_point;
			}
			set
			{
				colorEncoding.white_point = (UnsafeNativeJxl.JxlWhitePoint)value;
			}
		}
		public XYValue WhitePointXY
		{
			get
			{
				unsafe
				{
					return new XYValue(colorEncoding.white_point_xy.x, colorEncoding.white_point_xy.y);
				}
			}
			set
			{
				unsafe
				{
					colorEncoding.white_point_xy.x = value.X;
					colorEncoding.white_point_xy.y = value.Y;
				}
			}
		}

		public JxlPrimaries Primaries
		{
			get
			{
				return (JxlPrimaries)colorEncoding.primaries;
			}
			set
			{
				colorEncoding.primaries = (UnsafeNativeJxl.JxlPrimaries)value;
			}
		}

		public XYValue PrimariesRedXY
		{
			get
			{
				unsafe
				{
					return new XYValue(colorEncoding.primaries_red_xy.x, colorEncoding.primaries_red_xy.y);
				}
			}
			set
			{
				unsafe
				{
					colorEncoding.primaries_red_xy.x = value.X;
					colorEncoding.primaries_red_xy.y = value.Y;
				}
			}
		}

		public XYValue PrimariesGreenXY
		{
			get
			{
				unsafe
				{
					return new XYValue(colorEncoding.primaries_green_xy.x, colorEncoding.primaries_green_xy.y);
				}
			}
			set
			{
				unsafe
				{
					colorEncoding.primaries_green_xy.x = value.X;
					colorEncoding.primaries_green_xy.y = value.Y;
				}
			}
		}

		public XYValue PrimariesBlueXY
		{
			get
			{
				unsafe
				{
					return new XYValue(colorEncoding.primaries_blue_xy.x, colorEncoding.primaries_blue_xy.y);
				}
			}
			set
			{
				unsafe
				{
					colorEncoding.primaries_blue_xy.x = value.X;
					colorEncoding.primaries_blue_xy.y = value.Y;
				}
			}
		}

		public JxlTransferFunction TransferFunction
		{
			get
			{
				return (JxlTransferFunction)colorEncoding.transfer_function;
			}
			set
			{
				colorEncoding.transfer_function = (UnsafeNativeJxl.JxlTransferFunction)value;
			}
		}

		public double Gamma
		{
			get
			{
				return colorEncoding.gamma;
			}
			set
			{
				colorEncoding.gamma = value;
			}
		}

		public JxlRenderingIntent RenderingIntent
		{
			get
			{
				return (JxlRenderingIntent)colorEncoding.rendering_intent;
			}
			set
			{
				colorEncoding.rendering_intent = (UnsafeNativeJxl.JxlRenderingIntent)value;
			}
		}
	}

	public struct JxlLayerInfo
	{
		public bool HaveCrop;

		public int CropX0;

		public int CropY0;

		public int Width;

		public int Height;

		public JxlBlendInfo BlendInfo;

		public int SaveAsReference;
	}

	public enum JxlBlendMode
	{
		Replace = 0,
		Add = 1,
		Blend = 2,
		MulAdd = 3,
		Mul = 4,
	}

	public struct JxlBlendInfo
	{
		static JxlBlendInfo _defaultValues;
		static JxlBlendInfo()
		{
			UnsafeNativeJxl.JxlBlendInfo blendInfo;
			UnsafeNativeJxl.InitBlendInfo(out blendInfo);
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref blendInfo, out _defaultValues);
		}

		public void Initialize()
		{
			this.BlendMode = _defaultValues.BlendMode;
			this.Source = _defaultValues.Source;
			this.Alpha = _defaultValues.Alpha;
			this.Clamp = _defaultValues.Clamp;
		}

		public JxlBlendInfo(bool unused)
		{
			this.BlendMode = _defaultValues.BlendMode;
			this.Source = _defaultValues.Source;
			this.Alpha = _defaultValues.Alpha;
			this.Clamp = _defaultValues.Clamp;
		}


		public JxlBlendMode BlendMode;
		public int Source;
		public int Alpha;
		public bool Clamp;
	}

	public class JxlFrameHeader
	{
		private static UnsafeNativeJxl.JxlFrameHeader _defaultFrameHeader;
		static JxlFrameHeader()
		{
			UnsafeNativeJxl.InitFrameHeader(out _defaultFrameHeader);
		}

		public JxlFrameHeader()
		{
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref _defaultFrameHeader, this);
		}

		internal JxlFrameHeader(ref UnsafeNativeJxl.JxlFrameHeader header2)
		{
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref header2, this);
		}

		public uint Duration;

		public uint Timecode;

		public string Name;

		public bool IsLast;

		public JxlLayerInfo LayerInfo;
	}

	public enum JxlExtraChannelType
	{
		Alpha,
		Depth,
		SpotColor,
		SelectionMask,
		Black,
		Cfa,
		Thermal,
		Reserved0,
		Reserved1,
		Reserved2,
		Reserved3,
		Reserved4,
		Reserved5,
		Reserved6,
		Reserved7,
		Unknown,
		Optional
	}

	public struct RGBAFloat
	{
		public float R;
		public float G;
		public float B;
		public float A;
	}

	public class JxlExtraChannelInfo
	{
		public JxlExtraChannelInfo(JxlExtraChannelType type)
		{
			UnsafeNativeJxl.JxlExtraChannelInfo extraChannelInfo;
			UnsafeNativeJxl.InitExtraChannelInfo((UnsafeNativeJxl.JxlExtraChannelType)type, out extraChannelInfo);
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref extraChannelInfo, this);
		}

		internal JxlExtraChannelInfo(ref UnsafeNativeJxl.JxlExtraChannelInfo extraChannelInfo)
		{
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref extraChannelInfo, this);
		}

		public JxlExtraChannelType Type;

		public int BitsPerSample;

		public int ExponentBitsPerSample;

		public int DimShift;

		public int NameLength;

		public bool AlphaPremultiplied;

		public RGBAFloat SpotColor;

		public int CfaChannel;


	}

	public enum JxlColorProfileTarget
	{
		Original = 0,

		Data = 1,
	}

	public enum JxlEncoderError
	{
		Ok = 0,
		GenericError = 1,
		OutOfMemory = 2,
		JpegBitstreamReconstructionData = 3,
		BadInput = 4,
		NotSupported = 128,
		ApiUsageError = 129
	}

	public enum JxlEncoderStatus
	{
		Success,
		Error,
		NeedMoreOutput,
		NotSupported
	}

	public class RangeAttribute : Attribute
	{
		public int MinValue { get; set; }
		public int MaxValue { get; set; }
		public RangeAttribute(int minValue, int maxValue)
		{
			this.MinValue = minValue;
			this.MaxValue = maxValue;
		}
		public RangeAttribute() { }
	}

	public enum JxlEncoderFrameSettingId
	{
		[Description("Sets encoder effort/speed level without affecting decoding speed.\r\n" +
			"Valid values are, from faster to slower speed:\r\n" +
			"1:lightning, 2:thunder, 3:falcon, 4:cheetah, 5:hare, 6:wombat, 7:squirrel, 8:kitten, 9:tortoise\r\n" +
			"Default: squirrel (7)")]
		[DefaultValue(7)]
		[Range(1, 9)]
		Effort = 0,
		[Description("Sets the decoding speed tier for the provided options.\r\n" +
			" Minimum is 0  (slowest to decode, best quality/density), and maximum is 4 (fastest to " +
			"decode, at the cost of some quality/density).\r\n" +
			"Default is 0.")]
		[DefaultValue(0)]
		[Range(0, 4)]
		DecodingSpeed = 1,
		[Description("Sets resampling option.\r\n" +
			"If enabled, the image is downsampled before compression, and upsampled to original size in the decoder.\r\n" +
			"Integer option, use -1 for the default behavior (resampling only applied for low quality), " +
			"1 for no downsampling (1x1), 2 for 2x2 downsampling, 4 for 4x4 " +
			"downsampling, 8 for 8x8 downsampling. ")]
		[DefaultValue(-1)]
		[Range(1, 4)]
		Resampling = 2,
		[Description("Similar to RESAMPLING, but for extra channels.\r\n" +
			"Integer option, use -1 for the default behavior (depends on encoder implementation), " +
			"1 for no downsampling (1x1), 2 for 2x2 downsampling, 4 for 4x4 downsampling, " +
			"8 for 8x8 downsampling. ")]
		[DefaultValue(-1)]
		[Range(1, 8)]
		ExtraChannelResampling = 3,
		[Description("Indicates the frame added with JxlEncoderAddImageFrame is already " +
		"downsampled by the downsampling factor set with  " +
		"Resampling.\r\n" + "The input frame must then be given in the " +
		"downsampled resolution, not the full image resolution. The downsampled " +
		"resolution is given by ceil(xsize / resampling), ceil(ysize / resampling) " +
		"with xsize and ysize the dimensions given in the basic info, and resampling " +
		"the factor set with Resampling\r\n" +
		"Use 0 to disable, 1 to enable. Default value is 0. ")]
		[Browsable(false)]
		[DefaultValue(0)]
		[Range(0, 1)]
		AlreadyDownsampled = 4,
		[Description("Adds noise to the image emulating photographic film noise, the higher the given number, " +
			"the grainier the image will be.\r\n" +
			"As an example, a value of 100 gives low noise whereas a value of 3200 gives a lot of noise.\r\n" +
			"The default value is 0.")]
		[DefaultValue(0)]
		[Range(0, 3200)]
		PhotonNoise = 5,
		[Description("Enables adaptive noise generation. This setting is not recommended for use, " +
			"please use PHOTON_NOISE instead.\r\n" +
			"Use -1 for the default (encoder chooses), 0 to disable, 1 to enable. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		Noise = 6,
		[Description("Enables or disables dots generation.\r\n" +
			"Use -1 for the default (encoder chooses), 0 to disable, 1 to enable. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		Dots = 7,
		[Description("Enables or disables patches generation.\r\n" +
			"Use -1 for the default (encoder chooses), 0 to disable, 1 to enable. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		Patches = 8,
		[Description("Edge preserving filter level, -1 to 3.\r\n" +
			"Use -1 for the default (encoder  chooses), 0 to 3 to set a strength. ")]
		[DefaultValue(-1)]
		[Range(0, 3)]
		EPF = 9,
		[Description("Enables or disables the gaborish filter\r\n" +
			"Use -1 for the default (encoder chooses), 0 to disable, 1 to enable. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		Gaborish = 10,
		[Browsable(false)]
		[Description("Enables modular encoding rather than VarDCT mode.\r\n" +
			"Use -1 for default (encoder chooses), " +
			"0 to enforce VarDCT mode (e.g. for photographic images), " +
			"1 to enforce modular mode (e.g. for lossless images). ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		Modular = 11,
		[Description("Enables or disables preserving color of invisible pixels.\r\n" +
			"Use -1 for the default (1 if lossless, 0 if lossy), 0 to disable, 1 to enable. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		KeepInvisible = 12,
		[Description("Determines the order in which 256x256 regions are stored in the codestream " +
			"for progressive rendering.\r\n" +
			"Use -1 for the encoder default, 0 for scanline order, 1 for center-first order.")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		GroupOrder = 13,
		[Description("Determines the horizontal position of center for the center-first group order.\r\n" +
			"Use -1 to automatically use the middle of the image, 0..xsize to specifically set it.")]
		[DefaultValue(-1)]
		GroupOrderCenterX = 14,
		[Description("Determines the center for the center-first group order.\r\n" +
			"Use -1 to automatically use the middle of the image, 0..ysize to specifically set it.")]
		[DefaultValue(-1)]
		GroupOrderCenterY = 15,
		[Description("Enables or disables progressive encoding for modular mode.\r\n" +
			"Use -1 for the encoder default, 0 to disable, 1 to enable.")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		Responsive = 16,
		[Description("Set the progressive mode for the AC coefficients of VarDCT, " +
			"using spectral progression from the DCT coefficients.\r\n" +
			"Use -1 for the encoder default, 0 to disable, 1 to enable. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		ProgressiveAC = 17,
		[Description("Set the progressive mode for the AC coefficients of VarDCT, " +
			"using quantization of the least significant bits.\r\n" +
			"Use -1 for the encoder default, 0 to disable, 1 to enable.")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		QProgressiveAC = 18,
		[Description("Set the progressive mode using lower-resolution DC images for VarDCT.\r\n" +
			"Use -1 for the encoder default, 0 to disable, 1 to have an extra 64x64 lower resolution pass, " +
			"2 to have a 512x512 and 64x64 lower resolution pass. ")]
		[DefaultValue(-1)]
		[Range(0, 2)]
		ProgressiveDC = 19,
		[Description("Use Global channel palette if the amount of colors is smaller than this percentage of range.\r\n" +
			"Use 0-100 to set an explicit percentage, -1 to use the encoder default. Used for modular encoding.")]
		[DefaultValue(-1)]
		[Range(0, 100)]
		ChannelColorsGlobalPercent = 20,
		[Description("Use Local (per-group) channel palette if the amount of colors is smaller " +
			"than this percentage of range.\r\n" +
			"Use 0-100 to set an explicit percentage, -1 to use the encoder default. Used for modular encoding.")]
		[DefaultValue(-1)]
		[Range(0, 100)]
		ChannelColorsGroupPercent = 21,
		[Description("Use color palette if amount of colors is smaller than or equal to this amount, " +
			"or -1 to use the encoder default. Used for modular encoding. ")]
		[DefaultValue(-1)]
		[Range(1, 256)]
		PaletteColors = 22,
		[Description("Enables or disables delta palette.\r\n" +
			"Use -1 for the default (encoder chooses), 0 to disable, 1 to enable. Used in modular mode. ")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		LossyPalette = 23,
		[Description("Color transform for internal encoding:\r\n" +
			"-1 = default, 0=XYB, 1=none (RGB), 2=YCbCr.\r\n" +
			"The XYB setting performs the forward XYB transform. None and " +
			"YCbCr both perform no transform, but YCbCr is used to indicate that the " +
			"encoded data losslessly represents YCbCr values. ")]
		[DefaultValue(-1)]
		[Range(0, 2)]
		ColorTransform = 24,
		[Description("Reversible color transform for modular encoding:\r\n" +
			"-1=default, 0-41=RCT index, e.g. index 0 = none, index 6 = YCoCg.\r\n" +
			"If this option is set to a non-default value, the RCT will be globally " +
			"applied to the whole frame.\r\n" +
			"The default behavior is to try several RCTs locally per modular group, " +
			"depending on the speed and distance setting.")]
		[DefaultValue(-1)]
		[Range(0, 41)]
		ModularColorSpace = 25,
		[Description("Group size for modular encoding:\r\n-1=default, 0=128, 1=256, 2=512, 3=1024.")]
		[DefaultValue(-1)]
		[Range(0, 3)]
		ModularGroupSize = 26,
		[Description("Predictor for modular encoding.\r\n" +
			"-1 = default, 0=zero, 1=left, 2=top, 3=avg0, 4=select, 5=gradient, 6=weighted, " +
			"7=topright, 8=topleft,  9=leftleft, 10=avg1, 11=avg2, 12=avg3, 13=toptop predictive average, " +
			"14=mix 5 and 6, 15=mix everything. ")]
		[DefaultValue(-1)]
		[Range(0, 15)]
		ModularPredictor = 27,
		[Description("Fraction of pixels used to learn MA trees as a percentage.\r\n" +
			"-1 = default, 0 = no MA and fast decode, 50 = default value, 100 = all, values above " +
		"100 are also permitted.\r\n" +
			"Higher values use more encoder memory.")]
		[DefaultValue(-1)]
		[Range(0, 200)]
		ModularMaTreeLearningPercent = 28,
		[Description("Number of extra (previous-channel) MA tree properties to use.\r\n" +
			"-1 = default, 0-11 = valid values. Recommended values are in the range 0 to 3, " +
		"or 0 to amount of channels minus 1 (including all extra channels, and " +
		"excluding color channels when using VarDCT mode).\r\nHigher value gives slower " +
		"encoding and slower decoding.")]
		[DefaultValue(-1)]
		[Range(0, 11)]
		ModularNbPrevChannels = 29,
		[Description("Enable or disable CFL (chroma-from-luma) for lossless JPEG recompression.\r\n" +
		"-1 = default, 0 = disable CFL, 1 = enable CFL.")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		JpegReconChromaFromLuma = 30,
		[Description("Prepare the frame for indexing in the frame index box.\r\n" +
		" 0 = ignore this frame (same as not setting a value)," +
		" 1 = index this frame within the Frame Index Box.\r\n" +
		" If any frames are indexed, the first frame needs to" +
		" be indexed, too. If the first frame is not indexed, and" +
		" a later frame is attempted to be indexed, JXL_ENC_ERROR will occur." +
		" If non-keyframes, i.e., frames with cropping, blending or patches are" +
		" attempted to be indexed, JXL_ENC_ERROR will occur.")]
		[DefaultValue(-1)]
		[Range(0, 1)]
		FrameIndexBox = 31,
		[Description("Sets brotli encode effort for use in JPEG recompression and compressed metadata boxes (brob).\r\n" +
			"Can be -1 (default) or 0 (fastest) to 11 (slowest).\r\n" +
		"Default is based on the general encode effort in case of JPEG recompression, and 4 for brob boxes.")]
		[DefaultValue(-1)]
		[Range(0, 11)]
		BrotliEffort = 32,
	}

	//Remove this class if building for .NET Framework 2.0:
	public static class JxlEncoderFrameSettingIdExtensions
	{
		public static string GetDescription(this JxlEncoderFrameSettingId settingId)
		{
			var members = typeof(JxlEncoderFrameSettingId).GetMember(settingId.ToString());
			if (members == null || members.Length == 0) return "";
			var member = members[0];
			var attributes = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attributes == null || attributes.Length == 0) return "";
			var descriptionAttribute = (DescriptionAttribute)attributes[0];
			return descriptionAttribute.Description;
		}
	}

	public enum JxlProgressiveDetail
	{
		Frames,
		DC,
		LastPasses,
		Passes,
		DCProgressive,
		DCGroups,
		Groups
	}
}
