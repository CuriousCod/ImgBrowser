using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JxlSharp
{
	public class JxlEncoder : IDisposable
	{
		UnsafeNativeJxl.JxlEncoderWrapper encoderWrapper;

		JxlEncoderFrameSettings _frameSettings;

		public JxlEncoderFrameSettings FrameSettings
		{
			get
			{
				if (_frameSettings == null)
				{
					_frameSettings = new JxlEncoderFrameSettings(this, encoderWrapper.FrameSettings);
				}
				return _frameSettings;
			}
		}

		public JxlEncoder(Stream outputStream) : this(outputStream, 16 * 1024 * 1024)
		{

		}

		public JxlEncoder(Stream outputStream, int outputBufferSize)
		{
			this.encoderWrapper = new UnsafeNativeJxl.JxlEncoderWrapper(outputStream, outputBufferSize);
		}

		public bool IsDisposed
		{
			get
			{
				return encoderWrapper.IsDisposed;
			}
		}

		public void Dispose()
		{
			encoderWrapper.Dispose();
		}

		public void Reset()
		{
			encoderWrapper.Reset();
		}

		public JxlEncoderError GetError()
		{
			return (JxlEncoderError)encoderWrapper.GetError();
		}

		public JxlEncoderStatus ProcessOutput()
		{
			return (JxlEncoderStatus)encoderWrapper.ProcessOutput();
		}

		public JxlEncoderStatus AddBox(string boxType, byte[] contents, bool compressBox)
		{
			return (JxlEncoderStatus)encoderWrapper.AddBox(boxType, contents, compressBox);
		}

		public JxlEncoderStatus UseBoxes()
		{
			return (JxlEncoderStatus)encoderWrapper.UseBoxes();
		}

		public void CloseBoxes()
		{
			encoderWrapper.CloseBoxes();
		}

		public void CloseFrames()
		{
			encoderWrapper.CloseFrames();
		}

		public void CloseInput()
		{
			encoderWrapper.CloseInput();
		}

		public JxlEncoderStatus SetColorEncoding(JxlColorEncoding color)
		{
			return (JxlEncoderStatus)encoderWrapper.SetColorEncoding(ref color.colorEncoding);
		}

		public JxlEncoderStatus SetICCProfile(byte[] iccProfile)
		{
			return (JxlEncoderStatus)encoderWrapper.SetICCProfile(iccProfile);
		}

		public JxlEncoderStatus SetBasicInfo(JxlBasicInfo info)
		{
			return (JxlEncoderStatus)encoderWrapper.SetBasicInfo(ref info.basicInfo);
		}

		public JxlEncoderStatus SetExtraChannelInfo(int index, JxlExtraChannelInfo info)
		{
			UnsafeNativeJxl.JxlExtraChannelInfo info2 = new UnsafeNativeJxl.JxlExtraChannelInfo();
			UnsafeNativeJxl.CopyFields.ReadFromPublic(out info2, info);
			return (JxlEncoderStatus)encoderWrapper.SetExtraChannelInfo(index, ref info2);
		}

		public JxlEncoderStatus SetExtraChannelName(int index, string name)
		{
			return (JxlEncoderStatus)encoderWrapper.SetExtraChannelName(index, name);
		}

		public JxlEncoderStatus UseContainer(bool useContainer)
		{
			return (JxlEncoderStatus)encoderWrapper.UseContainer(useContainer);
		}

		public JxlEncoderStatus StoreJPEGMetadata(bool storeJpegMetadata)
		{
			return (JxlEncoderStatus)encoderWrapper.StoreJPEGMetadata(storeJpegMetadata);
		}

		public JxlEncoderStatus SetCodestreamLevel(int level)
		{
			return (JxlEncoderStatus)encoderWrapper.SetCodestreamLevel(level);
		}

		public int GetRequiredCodestreamLevel()
		{
			return encoderWrapper.GetRequiredCodestreamLevel();
		}

		public JxlEncoderFrameSettings CreateFrameSettings()
		{
			return new JxlEncoderFrameSettings(this, encoderWrapper.FrameSettingsCreate());
		}

		public JxlEncoderStatus AddJPEGFrame(JxlEncoderFrameSettings frameSettings, byte[] buffer)
		{
			return (JxlEncoderStatus)encoderWrapper.AddJPEGFrame(frameSettings.Wrapper, buffer);
		}


		public JxlEncoderStatus AddImageFrame(JxlEncoderFrameSettings frameSettings, JxlPixelFormat pixelFormat, IntPtr buffer, int size)
		{
			unsafe
			{
				return (JxlEncoderStatus)encoderWrapper.AddImageFrame(frameSettings.Wrapper, ref pixelFormat.pixelFormat, (void*)buffer, size);
			}
		}

		public JxlEncoderStatus AddImageFrame(JxlEncoderFrameSettings frameSettings, JxlPixelFormat pixelFormat, byte[] buffer)
		{
			unsafe
			{
				fixed (byte* pBuffer = buffer)
				{
					return AddImageFrame(frameSettings, pixelFormat, (IntPtr)pBuffer, buffer.Length);
				}
			}
		}

		public JxlEncoderStatus SetExtraChannelBuffer(JxlEncoderFrameSettings frameSettings, JxlPixelFormat pixelFormat, IntPtr buffer, int size, int index)
		{
			unsafe
			{
				return (JxlEncoderStatus)encoderWrapper.SetExtraChannelBuffer(frameSettings.Wrapper, ref pixelFormat.pixelFormat, (void*)buffer, size, index);
			}
		}
	}

	public class JxlEncoderFrameSettings
	{
		UnsafeNativeJxl.JxlEncoderFrameSettingsWrapper frameSettings;
		WeakReference _parent;
		
		internal UnsafeNativeJxl.JxlEncoderFrameSettingsWrapper Wrapper
		{
			get
			{
				return frameSettings;
			}
		}

		public JxlEncoder Parent
		{
			get
			{
				return (JxlEncoder)_parent.Target;
			}
			private set
			{
				_parent = new WeakReference(value);
			}
		}

		internal JxlEncoderFrameSettings(JxlEncoder parent, UnsafeNativeJxl.JxlEncoderFrameSettingsWrapper frameSettings)
		{
			this.Parent = parent;
			this.frameSettings = frameSettings;
		}

		public JxlEncoderFrameSettings Clone()
		{
			var parent = this.Parent;
			if (parent == null) return null;
			return new JxlEncoderFrameSettings(parent, frameSettings.Clone());
		}

		public JxlEncoderStatus SetFrameHeader(JxlFrameHeader frameHeader)
		{
			UnsafeNativeJxl.JxlFrameHeader header2 = new UnsafeNativeJxl.JxlFrameHeader();
			UnsafeNativeJxl.CopyFields.ReadFromPublic(out header2, frameHeader);
			JxlEncoderStatus status;
			status = (JxlEncoderStatus)frameSettings.SetFrameName(frameHeader.Name);
			if (status != JxlEncoderStatus.Success)
			{
				return status;
			}
			return (JxlEncoderStatus)frameSettings.SetFrameHeader(ref header2);
		}

		public JxlEncoderStatus SetExtraChannelBlendInfo(int index, JxlBlendInfo blendInfo)
		{
			UnsafeNativeJxl.JxlBlendInfo blendInfo2 = new UnsafeNativeJxl.JxlBlendInfo();
			UnsafeNativeJxl.CopyFields.ReadFromPublic(out blendInfo2, ref blendInfo);
			return (JxlEncoderStatus)frameSettings.SetExtraChannelBlendInfo(index, ref blendInfo2);
		}

		public JxlEncoderStatus SetFrameName(string frameName)
		{
			return (JxlEncoderStatus)frameSettings.SetFrameName(frameName);
		}

		public JxlEncoderStatus SetOption(JxlEncoderFrameSettingId option, long value)
		{
			return (JxlEncoderStatus)frameSettings.SetOption((UnsafeNativeJxl.JxlEncoderFrameSettingId)option, value);
		}

		public JxlEncoderStatus SetFloatOption(JxlEncoderFrameSettingId option, float value)
		{
			return (JxlEncoderStatus)frameSettings.SetFloatOption((UnsafeNativeJxl.JxlEncoderFrameSettingId)option, value);
		}

		public JxlEncoderStatus SetFrameLossless(bool lossless)
		{
			return (JxlEncoderStatus)frameSettings.SetFrameLossless(lossless);
		}

		public JxlEncoderStatus SetFrameDistance(float distance)
		{
			return (JxlEncoderStatus)frameSettings.SetFrameDistance(distance);
		}

		public JxlEncoderStatus SetFrameDistance(double distance)
		{
			return (JxlEncoderStatus)frameSettings.SetFrameDistance((float)distance);
		}
	}
}
