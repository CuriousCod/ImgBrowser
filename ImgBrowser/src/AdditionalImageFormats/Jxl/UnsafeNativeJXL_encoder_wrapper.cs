using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace JxlSharp
{
	//We declare a type to equal itself because of intellisense
	//otherwise intellisense would display the basic type under a different name
	//example: All "int" types would suddenly be displayed as JXL_BOOL if we didn't do this
	using UInt32 = UInt32;
	using Int32 = Int32;
	using IntPtr = IntPtr;
	using UIntPtr = UIntPtr;
	using Byte = Byte;
	using UInt64 = UInt64;

	//typedefs for C types
	using int32_t = Int32;
	using uint32_t = UInt32;
	using uint8_t = Byte;
	using size_t = UIntPtr;
	using JXL_BOOL = Int32;
	using uint64_t = UInt64;
	//"JxlParallelRunner" is a function pointer type
	using JxlParallelRunner = IntPtr;

	internal unsafe partial class UnsafeNativeJxl
	{
		internal class JxlEncoderWrapper : IDisposable
		{
			JxlEncoder* enc;
			void* parallelRunner;

			JxlEncoderFrameSettingsWrapper _frameSettings;

			public JxlEncoderFrameSettingsWrapper FrameSettings
			{
				get
				{
					if (_frameSettings == null)
					{
						_frameSettings = FrameSettingsCreate();
					}
					return _frameSettings;
				}
			}

			//byte* pOutputBuffer;
			//GCHandle outputBufferGcHandle;
			
			byte[] outputBuffer;
			int outputBufferInitialSize = 1024 * 1024;
			Stream outputStream;

			public JxlEncoderWrapper(Stream outputStream) : this (outputStream, 16 * 1024 * 1024)
			{

			}

			public bool IsDisposed
			{
				get
				{
					return enc == null;
				}
			}

			public JxlEncoderWrapper(Stream outputStream, int outputBufferSize)
			{
				this.outputStream = outputStream;
				enc = UnsafeNativeJxl.JxlEncoderCreate(null);
				parallelRunner = UnsafeNativeJxl.JxlThreadParallelRunnerCreate(null, (size_t)Environment.ProcessorCount);
				UnsafeNativeJxl.JxlEncoderSetParallelRunner(enc, JxlThreadParallelRunner, parallelRunner);
			}

			public JxlEncoderWrapper(int initialCapacity)
			{
			}
			~JxlEncoderWrapper()
			{
				Dispose();
			}

			public void Dispose()
			{
				if (enc != null)
				{
					//ReleaseInput();
					//ReleaseJPEGBuffer();
					//ReleaseBoxBuffer();
					UnsafeNativeJxl.JxlEncoderDestroy(enc);
					enc = null;
					GC.SuppressFinalize(this);
				}
				if (parallelRunner != null)
				{
					UnsafeNativeJxl.JxlThreadParallelRunnerDestroy(parallelRunner);
					parallelRunner = null;
				}
			}
			[DebuggerStepThrough()]
			private void CheckIfDisposed()
			{
				if (IsDisposed) throw new ObjectDisposedException(nameof(enc));
			}

			public void Reset()
			{
				CheckIfDisposed();
				//ReleaseInput();
				//ReleaseJPEGBuffer();
				//ReleaseBoxBuffer();
				UnsafeNativeJxl.JxlEncoderReset(enc);
				UnsafeNativeJxl.JxlEncoderSetParallelRunner(enc, JxlThreadParallelRunner, parallelRunner);
			}

			public void SetCms(JxlCmsInterface cms)
			{
				CheckIfDisposed();
				UnsafeNativeJxl.JxlEncoderSetCms(enc, cms);
			}

			//public JxlEncoderStatus SetParallelRunner(IntPtr parallel_runner, void* parallel_runner_opaque)
			//{ }

			public JxlEncoderError GetError()
			{
				return UnsafeNativeJxl.JxlEncoderGetError(enc);
			}

			public JxlEncoderStatus ProcessOutput(byte** next_out, size_t* avail_out)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderProcessOutput(enc, next_out, avail_out);
			}

			public JxlEncoderStatus ProcessOutput()
			{
				CheckIfDisposed();
				if (this.outputBuffer == null)
				{
					this.outputBuffer = new byte[this.outputBufferInitialSize];
				}
				fixed (byte* pOutput = this.outputBuffer)
				{
					int outputPosition = 0;
				repeat:
					byte* currentOutput = pOutput + outputPosition;
					byte* currentOutputInitial = currentOutput;
					size_t bytesRemaining = (size_t)(outputBuffer.Length - outputPosition);
					var status = ProcessOutput(&currentOutput, &bytesRemaining);
					outputPosition = (int)(currentOutput - currentOutputInitial);
					if (status == JxlEncoderStatus.JXL_ENC_NEED_MORE_OUTPUT)
					{
						byte[] nextBuffer = new byte[this.outputBuffer.Length * 4];
						Array.Copy(this.outputBuffer, nextBuffer, outputPosition);
						this.outputBuffer = nextBuffer;
						goto repeat;
					}
					this.outputStream.Write(this.outputBuffer, 0, outputPosition);
					return status;
				}
			}

			public JxlEncoderStatus AddBox(string boxType, byte[] contents, bool compressBox)
			{
				CheckIfDisposed();
				byte[] boxTypeBytes = Encoding.UTF8.GetBytes(boxType);
				if (boxTypeBytes.Length != 4)
				{
					byte[] bytes2 = new byte[4];
					int i;
					for (i = 0; i < boxTypeBytes.Length && i < bytes2.Length; i++)
					{
						bytes2[i] = boxTypeBytes[i];
					}
					for (; i < bytes2.Length; i++)
					{
						bytes2[i] = (byte)' ';
					}
					boxTypeBytes = bytes2;
				}
				fixed (byte* pBoxType = boxTypeBytes)
				{
					fixed (byte* pContents = contents)
					{
						return UnsafeNativeJxl.JxlEncoderAddBox(enc, pBoxType, pContents, (size_t)contents.Length, Convert.ToInt32(compressBox));
					}
				}
			}

			public JxlEncoderStatus UseBoxes()
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderUseBoxes(enc);
			}

			public void CloseBoxes()
			{
				CheckIfDisposed();
				UnsafeNativeJxl.JxlEncoderCloseBoxes(enc);
			}

			public void CloseFrames()
			{
				CheckIfDisposed();
				UnsafeNativeJxl.JxlEncoderCloseFrames(enc);
			}

			public void CloseInput()
			{
				CheckIfDisposed();
				UnsafeNativeJxl.JxlEncoderCloseInput(enc);
			}

			public JxlEncoderStatus SetColorEncoding(ref JxlColorEncoding color)
			{
				CheckIfDisposed();
				fixed (JxlColorEncoding* pColor = &color)
				{
					return UnsafeNativeJxl.JxlEncoderSetColorEncoding(enc, pColor);
				}
			}

			public JxlEncoderStatus SetICCProfile(byte[] icc_profile)
			{
				CheckIfDisposed();
				fixed (byte* pProfile = icc_profile)
				{
					return UnsafeNativeJxl.JxlEncoderSetICCProfile(enc, pProfile, (size_t)icc_profile.Length);
				}
			}

			public JxlEncoderStatus SetBasicInfo(ref JxlBasicInfo info)
			{
				CheckIfDisposed();
				fixed (JxlBasicInfo* pInfo = &info)
				{
					return UnsafeNativeJxl.JxlEncoderSetBasicInfo(enc, pInfo);
				}
			}

			public JxlEncoderStatus SetExtraChannelInfo(int index, ref JxlExtraChannelInfo info)
			{
				CheckIfDisposed();
				fixed (JxlExtraChannelInfo* pInfo = &info)
				{
					return UnsafeNativeJxl.JxlEncoderSetExtraChannelInfo(enc, (size_t)index, pInfo);
				}
			}

			public JxlEncoderStatus SetExtraChannelName(int index, string name)
			{
				CheckIfDisposed();
				int byteCount = Encoding.UTF8.GetByteCount(name);
				byte[] bytes = new byte[byteCount + 1];
				Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 0);
				fixed (byte* pBytes = bytes)
				{
					return UnsafeNativeJxl.JxlEncoderSetExtraChannelName(enc, (size_t)index, pBytes, (size_t)bytes.Length);
				}
			}

			public JxlEncoderStatus UseContainer(bool use_container)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderUseContainer(enc, Convert.ToInt32(use_container));
			}

			public JxlEncoderStatus StoreJPEGMetadata(bool store_jpeg_metadata)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderStoreJPEGMetadata(enc, Convert.ToInt32(store_jpeg_metadata));
			}

			public JxlEncoderStatus SetCodestreamLevel(int level)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderSetCodestreamLevel(enc, level);
			}

			public int GetRequiredCodestreamLevel()
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderGetRequiredCodestreamLevel(enc);
			}

			public JxlEncoderFrameSettingsWrapper FrameSettingsCreate()
			{
				return new JxlEncoderFrameSettingsWrapper(this, this.FrameSettingsCreate(null));
			}

			public JxlEncoderFrameSettings* FrameSettingsCreate(JxlEncoderFrameSettings* source)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderFrameSettingsCreate(enc, source);
			}

			[Obsolete]
			public JxlEncoderFrameSettings* JxlEncoderOptionsCreate(JxlEncoderFrameSettings* A_1)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderOptionsCreate(this.enc, A_1);
			}

			public JxlEncoderStatus AddJPEGFrame(JxlEncoderFrameSettingsWrapper frame_settings, byte* buffer, int size)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderAddJPEGFrame(frame_settings.frame_settings, buffer, (size_t)size);
			}

			public JxlEncoderStatus AddJPEGFrame(JxlEncoderFrameSettingsWrapper frame_settings, byte[] buffer)
			{
				CheckIfDisposed();
				fixed (byte* pBuffer = buffer)
				{
					return UnsafeNativeJxl.JxlEncoderAddJPEGFrame(frame_settings.frame_settings, pBuffer, (size_t)buffer.Length);
				}
			}


			public JxlEncoderStatus AddImageFrame(JxlEncoderFrameSettingsWrapper frame_settings, ref JxlPixelFormat pixel_format, void* buffer, int size)
			{
				CheckIfDisposed();
				fixed (JxlPixelFormat* pPixelFormat = &pixel_format)
				{
					return UnsafeNativeJxl.JxlEncoderAddImageFrame(frame_settings.frame_settings, pPixelFormat, buffer, (size_t)size);
				}
			}

			public JxlEncoderStatus SetExtraChannelBuffer(JxlEncoderFrameSettingsWrapper frame_settings, ref JxlPixelFormat pixel_format, void* buffer, int size, int index)
			{
				CheckIfDisposed();
				fixed (JxlPixelFormat* pPixelFormat = &pixel_format)
				{
					return UnsafeNativeJxl.JxlEncoderSetExtraChannelBuffer(frame_settings.frame_settings, pPixelFormat, buffer, (size_t)size, (uint)index);
				}
			}
		}

		internal class JxlEncoderFrameSettingsWrapper
		{
			WeakReference _parent;
			internal JxlEncoderFrameSettings* frame_settings;

			public JxlEncoderWrapper Parent
			{
				get
				{
					return (JxlEncoderWrapper)_parent.Target;
				}
				private set
				{
					this._parent = new WeakReference(value);
				}
			}

			internal JxlEncoderFrameSettingsWrapper(JxlEncoderWrapper parent, JxlEncoderFrameSettings* frameSettings)
			{
				this.Parent = parent;
				this.frame_settings = frameSettings;
			}

			public JxlEncoderFrameSettingsWrapper Clone()
			{
				var parent = this.Parent;
				if (parent == null) return null;
				return new JxlEncoderFrameSettingsWrapper(parent, parent.FrameSettingsCreate(this.frame_settings));
			}

			[DebuggerStepThrough()]
			void CheckIfDisposed()
			{
				var parent = this.Parent;
				if (parent == null) throw new ObjectDisposedException(nameof(parent));
				if (Parent.IsDisposed)
				{
					throw new ObjectDisposedException(nameof(parent));
				}
			}

			public JxlEncoderStatus SetFrameHeader(ref JxlFrameHeader frame_header)
			{
				CheckIfDisposed();
				fixed (JxlFrameHeader* pFrameHeader = &frame_header)
				{
					return UnsafeNativeJxl.JxlEncoderSetFrameHeader(frame_settings, pFrameHeader);
				}
			}

			public JxlEncoderStatus SetExtraChannelBlendInfo(int index, ref JxlBlendInfo blend_info)
			{
				CheckIfDisposed();
				fixed (JxlBlendInfo* pBlendInfo = &blend_info)
				{
					return UnsafeNativeJxl.JxlEncoderSetExtraChannelBlendInfo(frame_settings, (size_t)index, pBlendInfo);
				}
			}

			public JxlEncoderStatus SetFrameName(byte* frame_name)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderSetFrameName(frame_settings, frame_name);
			}

			public JxlEncoderStatus SetFrameName(string frame_name)
			{
				int byteCount = Encoding.UTF8.GetByteCount(frame_name);
				byte[] bytes = new byte[byteCount + 1];
				Encoding.UTF8.GetBytes(frame_name, 0, frame_name.Length, bytes, 0);
				fixed (byte* pBytes = bytes)
				{
					return UnsafeNativeJxl.JxlEncoderSetFrameName(frame_settings, pBytes);
				}
			}

			public JxlEncoderStatus SetOption(JxlEncoderFrameSettingId option, long value)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderFrameSettingsSetOption(frame_settings, option, value);
			}

			public JxlEncoderStatus SetFloatOption(JxlEncoderFrameSettingId option, float value)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderFrameSettingsSetFloatOption(frame_settings, option, value);
			}

			public JxlEncoderStatus SetFrameLossless(bool lossless)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderSetFrameLossless(frame_settings, Convert.ToInt32(lossless));
			}

			[Obsolete]
			public JxlEncoderStatus OptionsSetLossless(bool lossless)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderOptionsSetLossless(frame_settings, Convert.ToInt32(lossless));
			}

			[Obsolete]
			public JxlEncoderStatus SetEffort(int effort)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderOptionsSetEffort(frame_settings, effort);
			}

			[Obsolete]
			public JxlEncoderStatus SetDecodingSpeed(int tier)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderOptionsSetDecodingSpeed(frame_settings, tier);
			}

			public JxlEncoderStatus SetFrameDistance(float distance)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderSetFrameDistance(frame_settings, distance);
			}

			[Obsolete]
			public JxlEncoderStatus SetDistance(float distance)
			{
				CheckIfDisposed();
				return UnsafeNativeJxl.JxlEncoderOptionsSetDistance(frame_settings, distance);
			}
		}
	}
}
