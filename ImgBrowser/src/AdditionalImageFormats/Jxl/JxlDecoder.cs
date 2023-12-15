using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace JxlSharp
{
	public class JxlDecoder : IDisposable
	{
		JxlDecoderStatus lastStatus;
		bool skipNextProcessInput = false;
		UnsafeNativeJxl.JxlDecoderWrapper decoderWrapper;
		public JxlDecoder()
		{
			decoderWrapper = new UnsafeNativeJxl.JxlDecoderWrapper();
			//decompressBoxes = false;
			lastStatus = JxlDecoderStatus.Success;
			skipNextProcessInput = false;
		}
		public void Dispose()
		{
			decoderWrapper.Dispose();
		}

		public void Reset()
		{
			decoderWrapper.Reset();
			//decompressBoxes = false;
			lastStatus = JxlDecoderStatus.Success;
			skipNextProcessInput = false;
		}

		public void Rewind()
		{
			decoderWrapper.Rewind();
			lastStatus = JxlDecoderStatus.Success;
		}

		public void SkipFrames(int amount)
		{
			decoderWrapper.SkipFrames(amount);
		}

		public JxlDecoderStatus SkipCurrentFrame()
		{
			return (JxlDecoderStatus)decoderWrapper.SkipCurrentFrame();
		}

		[Obsolete]
		public JxlDecoderStatus GetDefaultPixelFormat(out JxlPixelFormat format)
		{
			format = new JxlPixelFormat();
			var status = (JxlDecoderStatus)decoderWrapper.GetDefaultPixelFormat(out format.pixelFormat);
			return status;
		}

		public int GetSizeHintBasicInfo()
		{
			return decoderWrapper.GetSizeHintBasicInfo();
		}

		public JxlDecoderStatus SubscribeEvents(JxlDecoderStatus eventsWanted)
		{
			return (JxlDecoderStatus)decoderWrapper.SubscribeEvents((int)eventsWanted);
		}

		public JxlDecoderStatus SetKeepOrientation(bool keepOrientation)
		{
			return (JxlDecoderStatus)decoderWrapper.SetKeepOrientation(keepOrientation);
		}

		public JxlDecoderStatus SetUnpremultiplyAlpha(bool unpremulAlpha)
		{
			return (JxlDecoderStatus)decoderWrapper.SetUnpremultiplyAlpha(unpremulAlpha);
		}

		public JxlDecoderStatus SetRenderSpotcolors(bool renderSpotcolors)
		{
			return (JxlDecoderStatus)decoderWrapper.SetRenderSpotcolors(renderSpotcolors);
		}

		public JxlDecoderStatus SetCoalescing(bool coalescing)
		{
			return (JxlDecoderStatus)decoderWrapper.SetCoalescing(coalescing);
		}

		public JxlDecoderStatus ProcessInput()
		{
			if (skipNextProcessInput)
			{
				skipNextProcessInput = false;
				return lastStatus;
			}
			lastStatus = (JxlDecoderStatus)decoderWrapper.ProcessInput();
			return lastStatus;
		}

		public JxlDecoderStatus SetInput([In] IntPtr data, int size)
		{
			unsafe
			{
				return (JxlDecoderStatus)decoderWrapper.SetInput((byte*)data, size);
			}
		}

		public JxlDecoderStatus SetInput(byte[] data)
		{
			return (JxlDecoderStatus)decoderWrapper.SetInput(data);
		}

		public int ReleaseInput()
		{
			return decoderWrapper.ReleaseInput();
		}

		public void CloseInput()
		{
			decoderWrapper.CloseInput();
		}

		public JxlDecoderStatus GetBasicInfo(out JxlBasicInfo info)
		{
			info = new JxlBasicInfo();
			return (JxlDecoderStatus)decoderWrapper.GetBasicInfo(out info.basicInfo);
		}

		public JxlDecoderStatus GetExtraChannelInfo(int index, out JxlExtraChannelInfo info)
		{
			UnsafeNativeJxl.JxlExtraChannelInfo info2;
			var status = (JxlDecoderStatus)decoderWrapper.GetExtraChannelInfo(index, out info2);
			info = new JxlExtraChannelInfo(ref info2);
			return status;
		}

		public JxlDecoderStatus GetExtraChannelName(int index, out string name)
		{
			return (JxlDecoderStatus)decoderWrapper.GetExtraChannelName(index, out name);
		}

		public JxlDecoderStatus GetColorAsEncodedProfile(JxlPixelFormat unusedFormat, JxlColorProfileTarget target, out JxlColorEncoding colorEncoding)
		{
			colorEncoding = new JxlColorEncoding();
			unsafe
			{
				if (unusedFormat != null)
				{
					fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &unusedFormat.pixelFormat)
					{
						var status = (JxlDecoderStatus)decoderWrapper.GetColorAsEncodedProfile(pFormat, (UnsafeNativeJxl.JxlColorProfileTarget)target, out colorEncoding.colorEncoding);
						return status;
					}
				}
				else
				{
					var status = (JxlDecoderStatus)decoderWrapper.GetColorAsEncodedProfile(null, (UnsafeNativeJxl.JxlColorProfileTarget)target, out colorEncoding.colorEncoding);
					return status;
				}
			}
		}

		public JxlDecoderStatus GetICCProfileSize(JxlPixelFormat unusedFormat, JxlColorProfileTarget target, out int size)
		{
			JxlDecoderStatus status;
			unsafe
			{
				if (unusedFormat != null)
				{
					fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &unusedFormat.pixelFormat)
					{
						status = (JxlDecoderStatus)decoderWrapper.GetICCProfileSize(pFormat, (UnsafeNativeJxl.JxlColorProfileTarget)target, out size);
						return status;
					}
				}
				else
				{
					status = (JxlDecoderStatus)decoderWrapper.GetICCProfileSize(null, (UnsafeNativeJxl.JxlColorProfileTarget)target, out size);
					return status;
				}
			}
		}

		public JxlDecoderStatus GetColorAsICCProfile(JxlPixelFormat unusedFormat, JxlColorProfileTarget target, out byte[] iccProfile)
		{
			unsafe
			{
				if (unusedFormat != null)
				{
					fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &unusedFormat.pixelFormat)
					{
						var status = (JxlDecoderStatus)decoderWrapper.GetColorAsICCProfile(pFormat, (UnsafeNativeJxl.JxlColorProfileTarget)target, out iccProfile);
						return status;
					}
				}
				else
				{
					var status = (JxlDecoderStatus)decoderWrapper.GetColorAsICCProfile(null, (UnsafeNativeJxl.JxlColorProfileTarget)target, out iccProfile);
					return status;
				}
			}
		}

		public JxlDecoderStatus SetPreferredColorProfile(JxlColorEncoding colorEncoding)
		{
			UnsafeNativeJxl.JxlColorEncoding colorEncoding2;
			UnsafeNativeJxl.CopyFields.ReadFromPublic(out colorEncoding2, colorEncoding);
			unsafe
			{
				var status = (JxlDecoderStatus)decoderWrapper.SetPreferredColorProfile(&colorEncoding2);
				return status;
			}
		}

		public JxlDecoderStatus GetPreviewOutBufferSize(JxlPixelFormat format, out int size)
		{
			UnsafeNativeJxl.JxlPixelFormat pixelFormat2;
			UnsafeNativeJxl.CopyFields.ReadFromPublic(out pixelFormat2, format);
			unsafe
			{
				var status = (JxlDecoderStatus)decoderWrapper.GetPreviewOutBufferSize(&pixelFormat2, out size);
				return status;
			}
		}

		public JxlDecoderStatus SetPreviewOutBuffer(JxlPixelFormat format, IntPtr buffer, int size)
		{
			//TODO remove the dangling pointer
			UnsafeNativeJxl.JxlPixelFormat pixelFormat2;
			UnsafeNativeJxl.CopyFields.ReadFromPublic(out pixelFormat2, format);
			unsafe
			{
				var status = (JxlDecoderStatus)decoderWrapper.SetPreviewOutBuffer(&pixelFormat2, (void*)buffer, size);
				return status;
			}
		}

		public JxlDecoderStatus GetFrameHeader(out JxlFrameHeader header)
		{
			UnsafeNativeJxl.JxlFrameHeader header2;
			string name;
			var status = (JxlDecoderStatus)decoderWrapper.GetFrameHeaderAndName(out header2, out name);
			header = new JxlFrameHeader(ref header2);
			//UnsafeNativeJxl.CopyFields.WriteToPublic(ref header2, header);
			header.Name = name;
			return status;
		}

		public JxlDecoderStatus GetFrameName(out string name)
		{
			var status = (JxlDecoderStatus)decoderWrapper.GetFrameName(out name);
			return status;
		}

		public JxlDecoderStatus GetExtraChannelBlendInfo(int index, out JxlBlendInfo blend_info)
		{
			UnsafeNativeJxl.JxlBlendInfo blendInfo2;
			var status = (JxlDecoderStatus)decoderWrapper.GetExtraChannelBlendInfo(index, out blendInfo2);
			UnsafeNativeJxl.CopyFields.WriteToPublic(ref blendInfo2, out blend_info);
			return status;
		}

		[Obsolete]
		public JxlDecoderStatus GetDCOutBufferSize(JxlPixelFormat format, out int size)
		{
			unsafe
			{
				fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &format.pixelFormat)
				{
					var status = (JxlDecoderStatus)decoderWrapper.GetDCOutBufferSize(pFormat, out size);
					return status;
				}
			}
		}
		[Obsolete]
		public JxlDecoderStatus SetDCOutBuffer(JxlPixelFormat format, IntPtr buffer, int size)
		{
			//TODO: prevent dangling pointer
			unsafe
			{
				fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &format.pixelFormat)
				{
					var status = (JxlDecoderStatus)decoderWrapper.SetDCOutBuffer(pFormat, (void*)buffer, size);
					return status;
				}
			}
		}

		public JxlDecoderStatus GetImageOutBufferSize(JxlPixelFormat format, out int size)
		{
			unsafe
			{
				fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &format.pixelFormat)
				{
					var status = (JxlDecoderStatus)decoderWrapper.GetImageOutBufferSize(pFormat, out size);
					return status;
				}
			}
		}
		public JxlDecoderStatus SetImageOutBuffer(JxlPixelFormat format, IntPtr buffer, int size)
		{
			unsafe
			{
				fixed (UnsafeNativeJxl.JxlPixelFormat* pFormat = &format.pixelFormat)
				{
					var status = (JxlDecoderStatus)decoderWrapper.SetImageOutBuffer(pFormat, (void*)buffer, size);
					return status;
				}
			}
		}

		public JxlDecoderStatus SetImageOutCallback(JxlPixelFormat format, IntPtr callback, IntPtr opaque)
		{
			JxlDecoderStatus status;
			unsafe
			{
				status = (JxlDecoderStatus)decoderWrapper.SetImageOutCallback(ref format.pixelFormat, (UIntPtr)(void*)callback, (void*)opaque);
			}
			return status;
		}

		public JxlDecoderStatus GetExtraChannelBufferSize(JxlPixelFormat format, out int size, int index)
		{
			var status = (JxlDecoderStatus)decoderWrapper.GetExtraChannelBufferSize(ref format.pixelFormat, out size, index);
			return status;
		}
		public JxlDecoderStatus SetExtraChannelBuffer(JxlPixelFormat format, IntPtr buffer, int size, int index)
		{
			unsafe
			{
				var status = (JxlDecoderStatus)decoderWrapper.SetExtraChannelBuffer(ref format.pixelFormat, (void*)buffer, size, (uint)index);
				return status;
			}
		}

		public JxlDecoderStatus SetJPEGBuffer(byte[] data, int outputPosition = 0)
		{
			var status = (JxlDecoderStatus)decoderWrapper.SetJPEGBuffer(data, outputPosition);
			return status;
		}

		public int ReleaseJPEGBuffer()
		{
			return decoderWrapper.ReleaseJPEGBuffer();
		}

		public JxlDecoderStatus SetBoxBuffer(byte[] data)
		{
			return (JxlDecoderStatus)decoderWrapper.SetBoxBuffer(data);
		}

		public int ReleaseBoxBuffer()
		{
			return decoderWrapper.ReleaseBoxBuffer();
		}

		public JxlDecoderStatus SetDecompressBoxes(bool decompress)
		{
			//this.decompressBoxes = decompress;
			return (JxlDecoderStatus)decoderWrapper.SetDecompressBoxes(decompress);
		}

		public JxlDecoderStatus GetBoxType(out string boxType, bool decompressed)
		{
			return (JxlDecoderStatus)decoderWrapper.GetBoxType(out boxType, decompressed);
		}

		public JxlDecoderStatus GetBoxSizeRaw(out ulong size)
		{
			return (JxlDecoderStatus)decoderWrapper.GetBoxSizeRaw(out size);
		}

		public JxlDecoderStatus GetBox(out byte[] box, out string boxType, bool decompressBox)
		{
			box = null;
			boxType = "";
			ulong boxSize;
			if (lastStatus != JxlDecoderStatus.Box)
			{
				return JxlDecoderStatus.Error;
			}
			JxlDecoderStatus status;
			status = (JxlDecoderStatus)decoderWrapper.SetDecompressBoxes(decompressBox);
			if (status != JxlDecoderStatus.Success)
			{
				return status;
			}
			status = (JxlDecoderStatus)decoderWrapper.GetBoxSizeRaw(out boxSize);
			if (status != JxlDecoderStatus.Success)
			{
				return status;
			}
			status = (JxlDecoderStatus)decoderWrapper.GetBoxType(out boxType, decompressBox);
			if (status != JxlDecoderStatus.Success)
			{
				return status;
			}
			box = new byte[(int)boxSize];
			status = (JxlDecoderStatus)decoderWrapper.SetBoxBuffer(box);
			if (status != JxlDecoderStatus.Success)
			{
				return status;
			}
			status = ProcessInput();
			decoderWrapper.ReleaseBoxBuffer();
			if (status < JxlDecoderStatus.BasicInfo)
			{
				return status;
			}
			skipNextProcessInput = true;
			return JxlDecoderStatus.Success;
		}

		public JxlDecoderStatus SetProgressiveDetail(JxlProgressiveDetail detail)
		{
			return (JxlDecoderStatus)decoderWrapper.SetProgressiveDetail((UnsafeNativeJxl.JxlProgressiveDetail)detail);
		}

		public int GetIntendedDownsamplingRatio()
		{
			return (int)decoderWrapper.GetIntendedDownsamplingRatio();
		}

		public JxlDecoderStatus FlushImage()
		{
			return (JxlDecoderStatus)decoderWrapper.FlushImage();
		}

	}

}
