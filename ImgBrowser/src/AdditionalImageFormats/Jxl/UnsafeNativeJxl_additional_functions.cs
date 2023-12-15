using System;
using System.Collections.Generic;
using System.Text;

namespace JxlSharp
{
	using UIntPtr = UIntPtr;
	using size_t = UIntPtr;

	internal static unsafe partial class UnsafeNativeJxl
	{
		internal static JxlSignature JxlSignatureCheck(byte[] buf)
		{
			fixed (byte* pBuf = buf)
			{
				return JxlSignatureCheck(pBuf, (size_t)buf.Length);
			}
		}

		internal static unsafe void JxlColorEncodingSetToSRGB(out JxlColorEncoding color_encoding, bool is_gray)
		{
			fixed (JxlColorEncoding* pColorEncoding = &color_encoding)
			{
				UnsafeNativeJxl.JxlColorEncodingSetToSRGB(pColorEncoding, Convert.ToInt32(is_gray));
			}
		}

		internal static unsafe void JxlColorEncodingSetToLinearSRGB(out JxlColorEncoding color_encoding, bool is_gray)
		{
			fixed (JxlColorEncoding* pColorEncoding = &color_encoding)
			{
				UnsafeNativeJxl.JxlColorEncodingSetToLinearSRGB(pColorEncoding, Convert.ToInt32(is_gray));
			}
		}

		internal static void InitBasicInfo(out JxlBasicInfo info)
		{
			fixed (JxlBasicInfo* pInfo = &info)
			{
				UnsafeNativeJxl.JxlEncoderInitBasicInfo(pInfo);
			}
		}

		internal static void InitFrameHeader(out JxlFrameHeader frame_header)
		{
			fixed (JxlFrameHeader* pFrameHeader = &frame_header)
			{
				UnsafeNativeJxl.JxlEncoderInitFrameHeader(pFrameHeader);
			}
		}

		internal static void InitBlendInfo(out JxlBlendInfo blend_info)
		{
			fixed (JxlBlendInfo* pBlendInfo = &blend_info)
			{
				UnsafeNativeJxl.JxlEncoderInitBlendInfo(pBlendInfo);
			}
		}

		internal static void InitExtraChannelInfo(JxlExtraChannelType type, out JxlExtraChannelInfo info)
		{
			fixed (JxlExtraChannelInfo* pInfo = &info)
			{
				UnsafeNativeJxl.JxlEncoderInitExtraChannelInfo(type, pInfo);
			}
		}
	}
}
