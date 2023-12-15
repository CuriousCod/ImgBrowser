using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JxlSharp
{
	internal static partial class UnsafeNativeJxl
	{
		internal struct OneHundredBytes
		{
			public int i0;

			public int i1;

			public int i2;

			public int i3;

			public int i4;

			public int i5;

			public int i6;

			public int i7;

			public int i8;

			public int i9;

			public int i10;

			public int i11;

			public int i12;

			public int i13;

			public int i14;

			public int i15;

			public int i16;

			public int i17;

			public int i18;

			public int i19;

			public int i20;

			public int i21;

			public int i22;

			public int i23;

			public int i24;
		}

		internal struct RGBAFloat
		{
			public float r;

			public float g;

			public float b;

			public float a;
		}

		internal struct XYValue
		{
			public double x;

			public double y;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 1)]
		internal struct JxlMemoryManager
		{
		}

		internal enum JxlDataType
		{
			JXL_TYPE_FLOAT = 0,
			JXL_TYPE_UINT8 = 2,
			JXL_TYPE_UINT16 = 3,
			JXL_TYPE_FLOAT16 = 5
		}

		internal enum JxlEndianness
		{
			JXL_NATIVE_ENDIAN,
			JXL_LITTLE_ENDIAN,
			JXL_BIG_ENDIAN
		}

		internal struct JxlPixelFormat
		{
			public uint num_channels;
			public JxlDataType data_type;
			public JxlEndianness endianness;
			public UIntPtr align;
		}

		internal enum JxlProgressiveDetail
		{
			kFrames,
			kDC,
			kLastPasses,
			kPasses,
			kDCProgressive,
			kDCGroups,
			kGroups
		}

		internal enum JxlColorSpace
		{
			JXL_COLOR_SPACE_RGB,
			JXL_COLOR_SPACE_GRAY,
			JXL_COLOR_SPACE_XYB,
			JXL_COLOR_SPACE_UNKNOWN
		}

		internal enum JxlWhitePoint
		{
			JXL_WHITE_POINT_D65 = 1,
			JXL_WHITE_POINT_CUSTOM = 2,
			JXL_WHITE_POINT_E = 10,
			JXL_WHITE_POINT_DCI = 11
		}

		internal enum JxlPrimaries
		{
			JXL_PRIMARIES_SRGB = 1,
			JXL_PRIMARIES_CUSTOM = 2,
			JXL_PRIMARIES_2100 = 9,
			JXL_PRIMARIES_P3 = 11
		}

		internal enum JxlTransferFunction
		{
			JXL_TRANSFER_FUNCTION_709 = 1,
			JXL_TRANSFER_FUNCTION_UNKNOWN = 2,
			JXL_TRANSFER_FUNCTION_LINEAR = 8,
			JXL_TRANSFER_FUNCTION_SRGB = 13,
			JXL_TRANSFER_FUNCTION_PQ = 16,
			JXL_TRANSFER_FUNCTION_DCI = 17,
			JXL_TRANSFER_FUNCTION_HLG = 18,
			JXL_TRANSFER_FUNCTION_GAMMA = 65535
		}

		internal enum JxlRenderingIntent
		{
			JXL_RENDERING_INTENT_PERCEPTUAL,
			JXL_RENDERING_INTENT_RELATIVE,
			JXL_RENDERING_INTENT_SATURATION,
			JXL_RENDERING_INTENT_ABSOLUTE
		}

		internal struct JxlColorEncoding
		{
			public JxlColorSpace color_space;

			public JxlWhitePoint white_point;

			public XYValue white_point_xy;

			public JxlPrimaries primaries;

			public XYValue primaries_red_xy;

			public XYValue primaries_green_xy;

			public XYValue primaries_blue_xy;

			public JxlTransferFunction transfer_function;

			public double gamma;

			public JxlRenderingIntent rendering_intent;
		}

		internal struct JxlColorProfile
		{
			public JxlColorEncoding color_encoding;

			public UIntPtr num_channels;
		}

		internal struct JxlCmsInterface
		{
			public unsafe void* init_data;

			public UIntPtr/*delegate*<void*, UIntPtr, UIntPtr, JxlColorProfile*, JxlColorProfile*, float, void*>*/ init;

			public UIntPtr/*delegate*<void*, UIntPtr, float*>*/ get_src_buf;

			public UIntPtr/*delegate*<void*, UIntPtr, float*>*/ get_dst_buf;

			public UIntPtr/*delegate*<void*, UIntPtr, float*, float*, UIntPtr, int>*/ run;

			public UIntPtr/*delegate*<void*, void>*/ destroy;
		}

		internal enum JxlOrientation
		{
			JXL_ORIENT_IDENTITY = 1,
			JXL_ORIENT_FLIP_HORIZONTAL,
			JXL_ORIENT_ROTATE_180,
			JXL_ORIENT_FLIP_VERTICAL,
			JXL_ORIENT_TRANSPOSE,
			JXL_ORIENT_ROTATE_90_CW,
			JXL_ORIENT_ANTI_TRANSPOSE,
			JXL_ORIENT_ROTATE_90_CCW
		}

		internal enum JxlExtraChannelType
		{
			JXL_CHANNEL_ALPHA,
			JXL_CHANNEL_DEPTH,
			JXL_CHANNEL_SPOT_COLOR,
			JXL_CHANNEL_SELECTION_MASK,
			JXL_CHANNEL_BLACK,
			JXL_CHANNEL_CFA,
			JXL_CHANNEL_THERMAL,
			JXL_CHANNEL_RESERVED0,
			JXL_CHANNEL_RESERVED1,
			JXL_CHANNEL_RESERVED2,
			JXL_CHANNEL_RESERVED3,
			JXL_CHANNEL_RESERVED4,
			JXL_CHANNEL_RESERVED5,
			JXL_CHANNEL_RESERVED6,
			JXL_CHANNEL_RESERVED7,
			JXL_CHANNEL_UNKNOWN,
			JXL_CHANNEL_OPTIONAL
		}

		internal struct JxlPreviewHeader
		{
			public uint xsize;

			public uint ysize;
		}

		internal struct JxlIntrinsicSizeHeader
		{
			public uint xsize;

			public uint ysize;
		}

		internal struct JxlAnimationHeader
		{
			public uint tps_numerator;

			public uint tps_denominator;

			public uint num_loops;

			public int have_timecodes;
		}

		internal enum JxlColorProfileTarget
		{
			JXL_COLOR_PROFILE_TARGET_ORIGINAL,
			JXL_COLOR_PROFILE_TARGET_DATA
		}

		internal struct JxlBasicInfo
		{
			public int have_container;

			public uint xsize;

			public uint ysize;

			public uint bits_per_sample;
			public uint exponent_bits_per_sample;
			public float intensity_target;
			public float min_nits;
			public int relative_to_max_display;
			public float linear_below;
			public int uses_original_profile;
			public int have_preview;
			public int have_animation;
			public JxlOrientation orientation;
			public uint num_color_channels;
			public uint num_extra_channels;

			public uint alpha_bits;

			public uint alpha_exponent_bits;

			public int alpha_premultiplied;

			public JxlPreviewHeader preview;

			public JxlAnimationHeader animation;

			public uint intrinsic_xsize;

			public uint intrinsic_ysize;

			public OneHundredBytes padding;
		}

		internal struct JxlExtraChannelInfo
		{
			public JxlExtraChannelType type;

			public uint bits_per_sample;

			public uint exponent_bits_per_sample;

			public uint dim_shift;

			public uint name_length;

			public int alpha_premultiplied;

			public RGBAFloat spot_color;

			public uint cfa_channel;
		}

		internal struct JxlHeaderExtensions
		{
			public ulong extensions;
		}

		internal enum JxlBlendMode
		{
			JXL_BLEND_REPLACE,
			JXL_BLEND_ADD,
			JXL_BLEND_BLEND,
			JXL_BLEND_MULADD,
			JXL_BLEND_MUL
		}

		internal struct JxlBlendInfo
		{
			public JxlBlendMode blendmode;

			public uint source;

			public uint alpha;

			public int clamp;
		}

		internal struct JxlLayerInfo
		{
			public int have_crop;

			public int crop_x0;

			public int crop_y0;

			public uint xsize;

			public uint ysize;

			public JxlBlendInfo blend_info;

			public uint save_as_reference;
		}

		internal struct JxlFrameHeader
		{
			public uint duration;

			public uint timecode;

			public uint name_length;

			public int is_last;

			public JxlLayerInfo layer_info;
		}

		internal enum JxlSignature
		{
			JXL_SIG_NOT_ENOUGH_BYTES,
			JXL_SIG_INVALID,
			JXL_SIG_CODESTREAM,
			JXL_SIG_CONTAINER
		}

		[Flags]
		internal enum JxlDecoderStatus
		{
			JXL_DEC_SUCCESS = 0,
			JXL_DEC_ERROR = 1,
			JXL_DEC_NEED_MORE_INPUT = 2,
			JXL_DEC_NEED_PREVIEW_OUT_BUFFER = 3,
			JXL_DEC_NEED_DC_OUT_BUFFER = 4,
			JXL_DEC_NEED_IMAGE_OUT_BUFFER = 5,
			JXL_DEC_JPEG_NEED_MORE_OUTPUT = 6,
			JXL_DEC_BOX_NEED_MORE_OUTPUT = 7,
			JXL_DEC_BASIC_INFO = 64,
			JXL_DEC_EXTENSIONS = 128,
			JXL_DEC_COLOR_ENCODING = 256,
			JXL_DEC_PREVIEW_IMAGE = 512,
			JXL_DEC_FRAME = 1024,
			JXL_DEC_DC_IMAGE = 2048,
			JXL_DEC_FULL_IMAGE = 4096,
			JXL_DEC_JPEG_RECONSTRUCTION = 8192,
			JXL_DEC_BOX = 16384,
			JXL_DEC_FRAME_PROGRESSION = 32768
		}

		internal enum JxlEncoderStatus
		{
			JXL_ENC_SUCCESS,
			JXL_ENC_ERROR,
			JXL_ENC_NEED_MORE_OUTPUT,
			JXL_ENC_NOT_SUPPORTED
		}

		internal enum JxlEncoderError
		{
			JXL_ENC_ERR_OK = 0,
			JXL_ENC_ERR_GENERIC = 1,
			JXL_ENC_ERR_OOM = 2,
			JXL_ENC_ERR_JBRD = 3,
			JXL_ENC_ERR_BAD_INPUT = 4,
			JXL_ENC_ERR_NOT_SUPPORTED = 128,
			JXL_ENC_ERR_API_USAGE = 129
		}

		internal enum JxlEncoderFrameSettingId
		{
			JXL_ENC_FRAME_SETTING_EFFORT = 0,
			JXL_ENC_FRAME_SETTING_DECODING_SPEED = 1,
			JXL_ENC_FRAME_SETTING_RESAMPLING = 2,
			JXL_ENC_FRAME_SETTING_EXTRA_CHANNEL_RESAMPLING = 3,
			JXL_ENC_FRAME_SETTING_ALREADY_DOWNSAMPLED = 4,
			JXL_ENC_FRAME_SETTING_PHOTON_NOISE = 5,
			JXL_ENC_FRAME_SETTING_NOISE = 6,
			JXL_ENC_FRAME_SETTING_DOTS = 7,
			JXL_ENC_FRAME_SETTING_PATCHES = 8,
			JXL_ENC_FRAME_SETTING_EPF = 9,
			JXL_ENC_FRAME_SETTING_GABORISH = 10,
			JXL_ENC_FRAME_SETTING_MODULAR = 11,
			JXL_ENC_FRAME_SETTING_KEEP_INVISIBLE = 12,
			JXL_ENC_FRAME_SETTING_GROUP_ORDER = 13,
			JXL_ENC_FRAME_SETTING_GROUP_ORDER_CENTER_X = 14,
			JXL_ENC_FRAME_SETTING_GROUP_ORDER_CENTER_Y = 15,
			JXL_ENC_FRAME_SETTING_RESPONSIVE = 16,
			JXL_ENC_FRAME_SETTING_PROGRESSIVE_AC = 17,
			JXL_ENC_FRAME_SETTING_QPROGRESSIVE_AC = 18,
			JXL_ENC_FRAME_SETTING_PROGRESSIVE_DC = 19,
			JXL_ENC_FRAME_SETTING_CHANNEL_COLORS_GLOBAL_PERCENT = 20,
			JXL_ENC_FRAME_SETTING_CHANNEL_COLORS_GROUP_PERCENT = 21,
			JXL_ENC_FRAME_SETTING_PALETTE_COLORS = 22,
			JXL_ENC_FRAME_SETTING_LOSSY_PALETTE = 23,
			JXL_ENC_FRAME_SETTING_COLOR_TRANSFORM = 24,
			JXL_ENC_FRAME_SETTING_MODULAR_COLOR_SPACE = 25,
			JXL_ENC_FRAME_SETTING_MODULAR_GROUP_SIZE = 26,
			JXL_ENC_FRAME_SETTING_MODULAR_PREDICTOR = 27,
			JXL_ENC_FRAME_SETTING_MODULAR_MA_TREE_LEARNING_PERCENT = 28,
			JXL_ENC_FRAME_SETTING_MODULAR_NB_PREV_CHANNELS = 29,
			JXL_ENC_FRAME_SETTING_JPEG_RECON_CFL = 30,
			JXL_ENC_FRAME_INDEX_BOX = 31,
			JXL_ENC_FRAME_SETTING_BROTLI_EFFORT = 32,
			JXL_ENC_FRAME_SETTING_FILL_ENUM = 65535
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 1)]
		internal struct JxlDecoder
		{
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 1)]
		internal struct JxlEncoder
		{
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 1)]
		internal struct JxlEncoderFrameSettings
		{
		}

		internal static int JXL_PARALLEL_RET_RUNNER_ERROR = -1;

		internal static int JXL_TRUE = 1;

		internal static int JXL_FALSE = 0;

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal static extern uint JxlDecoderVersion();

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlSignature JxlSignatureCheck(byte* buf, UIntPtr len);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoder* JxlDecoderCreate(JxlMemoryManager* memory_manager);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlDecoderReset(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlDecoderDestroy(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlDecoderRewind(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlDecoderSkipFrames(JxlDecoder* dec, UIntPtr amount);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSkipCurrentFrame(JxlDecoder* dec);

		[Obsolete]
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderDefaultPixelFormat(JxlDecoder* dec, JxlPixelFormat* format);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetParallelRunner(JxlDecoder* dec, IntPtr parallel_runner, void* parallel_runner_opaque);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern UIntPtr JxlDecoderSizeHintBasicInfo(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSubscribeEvents(JxlDecoder* dec, int events_wanted);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetKeepOrientation(JxlDecoder* dec, int skip_reorientation);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		public unsafe static extern JxlDecoderStatus JxlDecoderSetUnpremultiplyAlpha(JxlDecoder* dec, int unpremul_alpha);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetRenderSpotcolors(JxlDecoder* dec, int render_spotcolors);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetCoalescing(JxlDecoder* dec, int coalescing);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderProcessInput(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetInput(JxlDecoder* dec, byte* data, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern UIntPtr JxlDecoderReleaseInput(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlDecoderCloseInput(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetBasicInfo(JxlDecoder* dec, JxlBasicInfo* info);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetExtraChannelInfo(JxlDecoder* dec, UIntPtr index, JxlExtraChannelInfo* info);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetExtraChannelName(JxlDecoder* dec, UIntPtr index, byte* name, UIntPtr size);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetColorAsEncodedProfile(JxlDecoder* dec, JxlPixelFormat* unused_format, JxlColorProfileTarget target, JxlColorEncoding* color_encoding);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetICCProfileSize(JxlDecoder* dec, JxlPixelFormat* unused_format, JxlColorProfileTarget target, UIntPtr* size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetColorAsICCProfile(JxlDecoder* dec, JxlPixelFormat* unused_format, JxlColorProfileTarget target, byte* icc_profile, UIntPtr size);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetPreferredColorProfile(JxlDecoder* dec, JxlColorEncoding* color_encoding);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetDesiredIntensityTarget(JxlDecoder* dec, float desired_intensity_target);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderPreviewOutBufferSize(JxlDecoder* dec, JxlPixelFormat* format, UIntPtr* size);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetPreviewOutBuffer(JxlDecoder* dec, JxlPixelFormat* format, void* buffer, UIntPtr size);
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetFrameHeader(JxlDecoder* dec, JxlFrameHeader* header);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetFrameName(JxlDecoder* dec, byte* name, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetExtraChannelBlendInfo(JxlDecoder* dec, UIntPtr index, JxlBlendInfo* blend_info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		[Obsolete]
		internal unsafe static extern JxlDecoderStatus JxlDecoderDCOutBufferSize(JxlDecoder* dec, JxlPixelFormat* format, UIntPtr* size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		[Obsolete]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetDCOutBuffer(JxlDecoder* dec, JxlPixelFormat* format, void* buffer, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderImageOutBufferSize(JxlDecoder* dec, JxlPixelFormat* format, UIntPtr* size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetImageOutBuffer(JxlDecoder* dec, JxlPixelFormat* format, void* buffer, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetImageOutCallback(JxlDecoder* dec, JxlPixelFormat* format, UIntPtr/*delegate*<void*, UIntPtr, UIntPtr, UIntPtr, void*, void>*/ callback, void* opaque);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetMultithreadedImageOutCallback(JxlDecoder* dec, JxlPixelFormat* format, UIntPtr/*delegate*<void*, UIntPtr, UIntPtr, void*>*/ init_callback, UIntPtr/*delegate*<void*, UIntPtr, UIntPtr, UIntPtr, UIntPtr, void*, void>*/ run_callback, UIntPtr/*delegate*<void*, void>*/ destroy_callback, void* init_opaque);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderExtraChannelBufferSize(JxlDecoder* dec, JxlPixelFormat* format, UIntPtr* size, uint index);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetExtraChannelBuffer(JxlDecoder* dec, JxlPixelFormat* format, void* buffer, UIntPtr size, uint index);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetJPEGBuffer(JxlDecoder* dec, byte* data, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern UIntPtr JxlDecoderReleaseJPEGBuffer(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetBoxBuffer(JxlDecoder* dec, byte* data, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern UIntPtr JxlDecoderReleaseBoxBuffer(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetDecompressBoxes(JxlDecoder* dec, int decompress);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetBoxType(JxlDecoder* dec, byte* type, int decompressed);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderGetBoxSizeRaw(JxlDecoder* dec, ulong* size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderSetProgressiveDetail(JxlDecoder* dec, JxlProgressiveDetail detail);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern UIntPtr JxlDecoderGetIntendedDownsamplingRatio(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlDecoderStatus JxlDecoderFlushImage(JxlDecoder* dec);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal static extern uint JxlEncoderVersion();

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoder* JxlEncoderCreate(JxlMemoryManager* memory_manager);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderReset(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderDestroy(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderSetCms(JxlEncoder* enc, JxlCmsInterface cms);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetParallelRunner(JxlEncoder* enc, IntPtr parallel_runner, void* parallel_runner_opaque);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderError JxlEncoderGetError(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderProcessOutput(JxlEncoder* enc, byte** next_out, UIntPtr* avail_out);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetFrameHeader(JxlEncoderFrameSettings* frame_settings, JxlFrameHeader* frame_header);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetExtraChannelBlendInfo(JxlEncoderFrameSettings* frame_settings, UIntPtr index, JxlBlendInfo* blend_info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetFrameName(JxlEncoderFrameSettings* frame_settings, byte* frame_name);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderAddJPEGFrame(JxlEncoderFrameSettings* frame_settings, byte* buffer, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderAddImageFrame(JxlEncoderFrameSettings* frame_settings, JxlPixelFormat* pixel_format, void* buffer, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetExtraChannelBuffer(JxlEncoderFrameSettings* frame_settings, JxlPixelFormat* pixel_format, void* buffer, UIntPtr size, uint index);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderAddBox(JxlEncoder* enc, byte* type, byte* contents, UIntPtr size, int compress_box);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderUseBoxes(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderCloseBoxes(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderCloseFrames(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderCloseInput(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetColorEncoding(JxlEncoder* enc, JxlColorEncoding* color);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetICCProfile(JxlEncoder* enc, byte* icc_profile, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderInitBasicInfo(JxlBasicInfo* info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderInitFrameHeader(JxlFrameHeader* frame_header);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderInitBlendInfo(JxlBlendInfo* blend_info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetBasicInfo(JxlEncoder* enc, JxlBasicInfo* info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlEncoderInitExtraChannelInfo(JxlExtraChannelType type, JxlExtraChannelInfo* info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetExtraChannelInfo(JxlEncoder* enc, UIntPtr index, JxlExtraChannelInfo* info);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetExtraChannelName(JxlEncoder* enc, UIntPtr index, byte* name, UIntPtr size);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderFrameSettingsSetOption(JxlEncoderFrameSettings* frame_settings, JxlEncoderFrameSettingId option, long value);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		public unsafe static extern JxlEncoderStatus JxlEncoderFrameSettingsSetFloatOption(JxlEncoderFrameSettings* frame_settings, JxlEncoderFrameSettingId option, float value);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderUseContainer(JxlEncoder* enc, int use_container);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderStoreJPEGMetadata(JxlEncoder* enc, int store_jpeg_metadata);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetCodestreamLevel(JxlEncoder* enc, int level);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern int JxlEncoderGetRequiredCodestreamLevel(JxlEncoder* enc);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetFrameLossless(JxlEncoderFrameSettings* frame_settings, int lossless);

		[Obsolete]
		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderOptionsSetLossless(JxlEncoderFrameSettings* frame_settings, int lossless);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		[Obsolete]
		internal unsafe static extern JxlEncoderStatus JxlEncoderOptionsSetEffort(JxlEncoderFrameSettings* frame_settings, int effort);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		[Obsolete]
		internal unsafe static extern JxlEncoderStatus JxlEncoderOptionsSetDecodingSpeed(JxlEncoderFrameSettings* frame_settings, int tier);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderStatus JxlEncoderSetFrameDistance(JxlEncoderFrameSettings* frame_settings, float distance);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		[Obsolete]
		internal unsafe static extern JxlEncoderStatus JxlEncoderOptionsSetDistance(JxlEncoderFrameSettings* A_0, float A_1);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern JxlEncoderFrameSettings* JxlEncoderFrameSettingsCreate(JxlEncoder* enc, JxlEncoderFrameSettings* source);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		[Obsolete]
		internal unsafe static extern JxlEncoderFrameSettings* JxlEncoderOptionsCreate(JxlEncoder* A_0, JxlEncoderFrameSettings* A_1);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlColorEncodingSetToSRGB(JxlColorEncoding* color_encoding, int is_gray);

		[DllImport("jxl.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlColorEncodingSetToLinearSRGB(JxlColorEncoding* color_encoding, int is_gray);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern int JxlResizableParallelRunner(void* runner_opaque, void* jpegxl_opaque, IntPtr init, IntPtr func, uint start_range, uint end_range);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void* JxlResizableParallelRunnerCreate(JxlMemoryManager* memory_manager);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlResizableParallelRunnerSetThreads(void* runner_opaque, UIntPtr num_threads);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal static extern uint JxlResizableParallelRunnerSuggestThreads(ulong xsize, ulong ysize);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlResizableParallelRunnerDestroy(void* runner_opaque);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "JxlThreadParallelRunner", CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern int _JxlThreadParallelRunner(void* runner_opaque, void* jpegxl_opaque, IntPtr init, IntPtr func, uint start_range, uint end_range);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void* JxlThreadParallelRunnerCreate(JxlMemoryManager* memory_manager, UIntPtr num_worker_threads);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal unsafe static extern void JxlThreadParallelRunnerDestroy(void* runner_opaque);

		[DllImport("jxl_threads.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		[MethodImpl(MethodImplOptions.ForwardRef)]
		internal static extern UIntPtr JxlThreadParallelRunnerDefaultNumWorkerThreads();
	}
}
