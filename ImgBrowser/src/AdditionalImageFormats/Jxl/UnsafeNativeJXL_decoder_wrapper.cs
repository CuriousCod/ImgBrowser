using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
	using JxlImageOutCallback = UIntPtr;


	internal unsafe partial class UnsafeNativeJxl
	{
		internal class JxlDecoderWrapper : IDisposable
		{
			JxlDecoder* dec;
			void* parallelRunner;

			byte[] input;
			byte* pInput;
			GCHandle inputGcHandle;

			byte[] jpegOutput;
			byte* pJpegOutput;
			GCHandle jpegOutputGcHandle;

			byte[] boxBuffer;
			byte* pBoxBuffer;
			GCHandle boxBufferGcHandle;

			public JxlDecoderWrapper()
			{
				dec = JxlDecoderCreate(null);
				parallelRunner = JxlThreadParallelRunnerCreate(null, (size_t)Environment.ProcessorCount);
				JxlDecoderSetParallelRunner(dec, JxlThreadParallelRunner, parallelRunner);
			}
			~JxlDecoderWrapper()
			{
				Dispose();
			}
			public void Dispose()
			{
				if (dec != null)
				{
					ReleaseInput();
					ReleaseJPEGBuffer();
					ReleaseBoxBuffer();
					JxlDecoderDestroy(dec);
					dec = null;
					GC.SuppressFinalize(this);
				}
				if (parallelRunner != null)
				{
					JxlThreadParallelRunnerDestroy(parallelRunner);
					parallelRunner = null;
				}
			}
			[DebuggerStepThrough()]
			private void CheckIfDisposed()
			{
				if (dec == null) throw new ObjectDisposedException(nameof(dec));
			}

			public void Reset()
			{
				CheckIfDisposed();
				ReleaseInput();
				ReleaseJPEGBuffer();
				ReleaseBoxBuffer();
				JxlDecoderReset(dec);
				JxlDecoderSetParallelRunner(dec, JxlThreadParallelRunner, parallelRunner);
			}
			public void Rewind()
			{
				CheckIfDisposed();
				JxlDecoderRewind(dec);
			}
			public void SkipFrames(int amount)
			{
				CheckIfDisposed();
				JxlDecoderSkipFrames(dec, (size_t)amount);
			}
			public JxlDecoderStatus SkipCurrentFrame()
			{
				return JxlDecoderSkipCurrentFrame(dec);
			}

			[Obsolete]
			public JxlDecoderStatus GetDefaultPixelFormat(out JxlPixelFormat format)
			{
				CheckIfDisposed();
				fixed (JxlPixelFormat* pFormat = &format)
				{
					return JxlDecoderDefaultPixelFormat(dec, pFormat);
				}
			}
			private JxlDecoderStatus SetParallelRunner(JxlParallelRunner parallel_runner, void* parallel_runner_opaque)
			{
				CheckIfDisposed();
				return JxlDecoderSetParallelRunner(dec, parallel_runner, parallel_runner_opaque);
			}
			public int GetSizeHintBasicInfo()
			{
				CheckIfDisposed();
				return (int)JxlDecoderSizeHintBasicInfo(dec);
			}
			public JxlDecoderStatus SubscribeEvents(int events_wanted)
			{
				CheckIfDisposed();
				return JxlDecoderSubscribeEvents(dec, events_wanted);
			}

			public JxlDecoderStatus SetKeepOrientation(bool keep_orientation)
			{
				CheckIfDisposed();
				return JxlDecoderSetKeepOrientation(dec, Convert.ToInt32(keep_orientation));
			}

			public JxlDecoderStatus SetUnpremultiplyAlpha(bool unpremul_alpha)
			{
				CheckIfDisposed();
				return JxlDecoderSetUnpremultiplyAlpha(dec, Convert.ToInt32(unpremul_alpha));
			}


			public JxlDecoderStatus SetRenderSpotcolors(bool render_spotcolors)
			{
				CheckIfDisposed();
				return JxlDecoderSetRenderSpotcolors(dec, Convert.ToInt32(render_spotcolors));
			}

			public JxlDecoderStatus SetCoalescing(bool coalescing)
			{
				CheckIfDisposed();
				return JxlDecoderSetCoalescing(dec, Convert.ToInt32(coalescing));
			}

			public JxlDecoderStatus ProcessInput()
			{
				CheckIfDisposed();
				return JxlDecoderProcessInput(dec);
			}
			public JxlDecoderStatus SetInput([In] uint8_t* data, int size)
			{
				CheckIfDisposed();
				if (this.pInput != null)
				{
					return JxlDecoderStatus.JXL_DEC_ERROR;
				}
				this.pInput = data;
				return JxlDecoderSetInput(dec, data, (size_t)size);
			}
			public JxlDecoderStatus SetInput(byte[] data)
			{
				CheckIfDisposed();
				if (this.input == data) return JxlDecoderStatus.JXL_DEC_SUCCESS;

				ReleaseInput();
				if (data == null) return JxlDecoderStatus.JXL_DEC_SUCCESS;
				this.input = data;
				this.inputGcHandle = GCHandle.Alloc(this.input, GCHandleType.Pinned);
				this.pInput = (byte*)this.inputGcHandle.AddrOfPinnedObject();
				return JxlDecoderSetInput(dec, this.pInput, (size_t)this.input.Length);
			}

			public int ReleaseInput()
			{
				CheckIfDisposed();
				if (this.pInput == null)
				{
					return 0;
				}
				if (this.input != null)
				{
					this.input = null;
				}
				this.pInput = null;
				if (this.inputGcHandle.IsAllocated)
				{
					this.inputGcHandle.Free();
				}
				int result = (int)JxlDecoderReleaseInput(dec);
				return result;
			}

			public void CloseInput()
			{
				CheckIfDisposed();
				JxlDecoderCloseInput(dec);
			}


			public JxlDecoderStatus GetBasicInfo(out JxlBasicInfo info)
			{
				CheckIfDisposed();
				fixed (JxlBasicInfo* pInfo = &info)
				{
					return JxlDecoderGetBasicInfo(dec, pInfo);
				}
			}
			public JxlDecoderStatus GetExtraChannelInfo(int index, out JxlExtraChannelInfo info)
			{
				CheckIfDisposed();
				fixed (JxlExtraChannelInfo* pInfo = &info)
				{
					return JxlDecoderGetExtraChannelInfo(dec, (size_t)index, pInfo);
				}
			}

			//private static int strlen_s(byte* pBytes, int bufferSize)
			//{
			//	int i;
			//	for (i = 0; i < bufferSize; i++)
			//	{
			//		if (pBytes[i] == 0) break;
			//	}
			//	return i;
			//}

			public JxlDecoderStatus GetExtraChannelName(int index, out string name)
			{
				CheckIfDisposed();
				JxlDecoderStatus status = JxlDecoderStatus.JXL_DEC_ERROR;
				name = "";
				JxlExtraChannelInfo info;
				status = GetExtraChannelInfo(index, out info);
				if (status == JxlDecoderStatus.JXL_DEC_SUCCESS)
				{
					int bufferSize = (int)info.name_length + 1;
					byte[] buffer = new byte[bufferSize];
					fixed (byte* pBuffer = buffer)
					{
						status = JxlDecoderGetExtraChannelName(dec, (size_t)index, pBuffer, (size_t)bufferSize);
						if (status == JxlDecoderStatus.JXL_DEC_SUCCESS)
						{
							name = Encoding.UTF8.GetString(buffer, 0, bufferSize - 1);
						}
					}
				}
				return status;
			}

			public JxlDecoderStatus GetColorAsEncodedProfile([In] JxlPixelFormat* format, JxlColorProfileTarget target, out JxlColorEncoding color_encoding)
			{
				CheckIfDisposed();
				fixed (JxlColorEncoding* pColor_encoding = &color_encoding)
				{
					return JxlDecoderGetColorAsEncodedProfile(dec, format, target, pColor_encoding);
				}
			}
			public JxlDecoderStatus GetICCProfileSize([In] JxlPixelFormat* format, JxlColorProfileTarget target, out int size)
			{
				CheckIfDisposed();
				size_t _size;
				var result = JxlDecoderGetICCProfileSize(dec, format, target, &_size);
				size = (int)_size;
				return result;
			}
			public JxlDecoderStatus GetColorAsICCProfile([In] JxlPixelFormat* format, JxlColorProfileTarget target, out byte[] icc_profile)
			{
				CheckIfDisposed();
				var status = GetICCProfileSize(format, target, out int size);
				if (status != JxlDecoderStatus.JXL_DEC_SUCCESS)
				{
					icc_profile = null;
					return status;
				}
				icc_profile = new byte[size];
				fixed (byte* pIccProfile = icc_profile)
				{
					return JxlDecoderGetColorAsICCProfile(dec, format, target, pIccProfile, (size_t)size);
				}
			}
			public JxlDecoderStatus SetPreferredColorProfile([In] JxlColorEncoding* color_encoding)
			{
				CheckIfDisposed();
				return JxlDecoderSetPreferredColorProfile(dec, color_encoding);
			}
			public JxlDecoderStatus GetPreviewOutBufferSize([In] JxlPixelFormat* format, out int size)
			{
				CheckIfDisposed();
				size_t _size;
				var result = JxlDecoderPreviewOutBufferSize(dec, format, &_size);
				size = (int)_size;
				return result;
			}
			public JxlDecoderStatus SetPreviewOutBuffer([In] JxlPixelFormat* format, void* buffer, int size)
			{
				CheckIfDisposed();
				return JxlDecoderSetPreviewOutBuffer(dec, format, buffer, (size_t)size);
			}

			public JxlDecoderStatus GetFrameHeader(out JxlFrameHeader header)
			{
				CheckIfDisposed();
				fixed (JxlFrameHeader* pHeader = &header)
				{
					return JxlDecoderGetFrameHeader(dec, pHeader);
				}
			}

			public JxlDecoderStatus GetFrameHeaderAndName(out JxlFrameHeader header, out string name)
			{
				CheckIfDisposed();
				name = "";
				fixed (JxlFrameHeader* pHeader = &header)
				{
					var status = JxlDecoderGetFrameHeader(dec, pHeader);
					if (status == JxlDecoderStatus.JXL_DEC_SUCCESS && header.name_length > 0)
					{
						int bufferSize = (int)header.name_length + 1;
						byte[] buffer = new byte[bufferSize];
						fixed (byte* pBuffer = buffer)
						{
							status = JxlDecoderGetFrameName(dec, pBuffer, (size_t)bufferSize);
							if (status == JxlDecoderStatus.JXL_DEC_SUCCESS)
							{
								name = Encoding.UTF8.GetString(buffer, 0, bufferSize - 1);
							}
						}
					}
					return status;
				}
			}

			public JxlDecoderStatus GetFrameName(out string name)
			{
				CheckIfDisposed();
				name = "";
				JxlDecoderStatus status = JxlDecoderStatus.JXL_DEC_ERROR;
				JxlFrameHeader frameHeader;
				status = GetFrameHeader(out frameHeader);
				if (status == JxlDecoderStatus.JXL_DEC_SUCCESS)
				{
					int bufferSize = (int)frameHeader.name_length + 1;
					byte[] buffer = new byte[bufferSize];
					fixed (byte* pBuffer = buffer)
					{
						status = JxlDecoderGetFrameName(dec, pBuffer, (size_t)bufferSize);
						if (status == JxlDecoderStatus.JXL_DEC_SUCCESS)
						{
							name = Encoding.UTF8.GetString(buffer, 0, bufferSize - 1);
						}
					}
				}
				return status;
			}

			public JxlDecoderStatus GetExtraChannelBlendInfo(int index, out JxlBlendInfo blend_info)
			{
				CheckIfDisposed();
				fixed (JxlBlendInfo* pBlendInfo = &blend_info)
				{
					return JxlDecoderGetExtraChannelBlendInfo(dec, (size_t)index, pBlendInfo);
				}
			}

			[Obsolete]
			public JxlDecoderStatus GetDCOutBufferSize([In] JxlPixelFormat* format, out int size)
			{
				CheckIfDisposed();
				size_t _size;
				var result = JxlDecoderDCOutBufferSize(dec, format, &_size);
				size = (int)_size;
				return result;
			}
			[Obsolete]
			public JxlDecoderStatus SetDCOutBuffer([In] JxlPixelFormat* format, void* buffer, int size)
			{
				CheckIfDisposed();
				return JxlDecoderSetDCOutBuffer(dec, format, buffer, (size_t)size);
			}

			public JxlDecoderStatus GetImageOutBufferSize([In] JxlPixelFormat* format, out int size)
			{
				CheckIfDisposed();
				size_t _size;
				var result = JxlDecoderImageOutBufferSize(dec, format, &_size);
				size = (int)_size;
				return result;
			}
			public JxlDecoderStatus SetImageOutBuffer([In] JxlPixelFormat* format, void* buffer, int size)
			{
				CheckIfDisposed();
				return JxlDecoderSetImageOutBuffer(dec, format, buffer, (size_t)size);
			}

			public JxlDecoderStatus SetImageOutCallback([In] ref JxlPixelFormat format, JxlImageOutCallback callback, void* opaque)
			{
				CheckIfDisposed();
				fixed (JxlPixelFormat *pFormat = &format)
				{
					return JxlDecoderSetImageOutCallback(dec, pFormat, callback, opaque);
				}
			}
			public JxlDecoderStatus GetExtraChannelBufferSize([In] ref JxlPixelFormat format, out int size, int index)
			{
				CheckIfDisposed();
				size_t _size;
				JxlDecoderStatus status;
				fixed (JxlPixelFormat* pFormat = &format)
				{
					status = JxlDecoderExtraChannelBufferSize(dec, pFormat, &_size, (uint32_t)index);
				}
				size = (int)_size;
				return status;
			}
			public JxlDecoderStatus SetExtraChannelBuffer([In] ref JxlPixelFormat format, void* buffer, int size, uint32_t index)
			{
				CheckIfDisposed();
				fixed (JxlPixelFormat* pFormat = &format)
				{
					return JxlDecoderSetExtraChannelBuffer(dec, pFormat, buffer, (size_t)size, index);
				}
			}
			public JxlDecoderStatus SetJPEGBuffer(uint8_t* data, int size)
			{
				CheckIfDisposed();
				if (this.pJpegOutput != null)
				{
					return JxlDecoderStatus.JXL_DEC_ERROR;
				}
				this.pJpegOutput = data;
				return JxlDecoderSetJPEGBuffer(dec, data, (size_t)size);
			}

			public JxlDecoderStatus SetJPEGBuffer(byte[] data, int outputPosition = 0)
			{
				CheckIfDisposed();
				ReleaseJPEGBuffer();
				if (data == null)
				{
					return JxlDecoderStatus.JXL_DEC_SUCCESS;
				}
				if (outputPosition < 0 || outputPosition >= data.Length)
				{
					return JxlDecoderStatus.JXL_DEC_ERROR;
				}
				this.jpegOutput = data;
				this.jpegOutputGcHandle = GCHandle.Alloc(this.jpegOutput, GCHandleType.Pinned);
				this.pJpegOutput = (byte*)this.jpegOutputGcHandle.AddrOfPinnedObject();
				return JxlDecoderSetJPEGBuffer(dec, this.pJpegOutput + outputPosition, (size_t)(this.jpegOutput.Length - outputPosition));
			}

			public int ReleaseJPEGBuffer()
			{
				CheckIfDisposed();
				if (this.pJpegOutput == null)
				{
					return 0;
				}
				if (this.jpegOutput != null)
				{
					this.jpegOutput = null;
				}
				this.pJpegOutput = null;
				if (this.jpegOutputGcHandle.IsAllocated)
				{
					this.jpegOutputGcHandle.Free();
				}
				int result = (int)JxlDecoderReleaseJPEGBuffer(dec);
				return result;
			}

			public JxlDecoderStatus SetBoxBuffer(uint8_t* data, int size)
			{
				CheckIfDisposed();
				return JxlDecoderSetBoxBuffer(dec, data, (size_t)size);
			}
			public JxlDecoderStatus SetBoxBuffer(byte[] data)
			{
				CheckIfDisposed();
				ReleaseBoxBuffer();
				if (data == null) return JxlDecoderStatus.JXL_DEC_SUCCESS;
				this.boxBuffer = data;
				this.boxBufferGcHandle = GCHandle.Alloc(this.boxBuffer, GCHandleType.Pinned);
				this.pBoxBuffer = (byte*)this.boxBufferGcHandle.AddrOfPinnedObject();
				return JxlDecoderSetBoxBuffer(dec, pBoxBuffer, (size_t)this.boxBuffer.Length);
			}
			public int ReleaseBoxBuffer()
			{
				CheckIfDisposed();
				if (this.pBoxBuffer == null)
				{
					return 0;
				}
				if (this.boxBuffer != null)
				{
					this.boxBuffer = null;
				}
				this.pBoxBuffer = null;
				if (this.boxBufferGcHandle.IsAllocated)
				{
					this.boxBufferGcHandle.Free();
				}
				int result = (int)JxlDecoderReleaseBoxBuffer(dec);
				return result;
			}
			public JxlDecoderStatus SetDecompressBoxes(bool decompress)
			{
				CheckIfDisposed();
				return JxlDecoderSetDecompressBoxes(dec, (JXL_BOOL)Convert.ToInt32(decompress));
			}
			public JxlDecoderStatus GetBoxType(out string boxType, bool decompressed)
			{
				CheckIfDisposed();
				byte[] buffer = new byte[4];
				fixed (byte* pBuffer = buffer)
				{
					var status = JxlDecoderGetBoxType(dec, pBuffer, (JXL_BOOL)Convert.ToInt32(decompressed));
					int len = 0;
					for (len = 0; len < buffer.Length; len++)
					{
						if (buffer[len] == 0) break;
					}
					boxType = Encoding.UTF8.GetString(buffer, 0, len);
					return status;
				}
			}
			public JxlDecoderStatus GetBoxSizeRaw(out uint64_t size)
			{
				CheckIfDisposed();
				fixed (uint64_t* pSize = &size)
				{
					return JxlDecoderGetBoxSizeRaw(dec, pSize);
				}
			}

			public JxlDecoderStatus SetProgressiveDetail(JxlProgressiveDetail detail)
			{
				CheckIfDisposed();
				return JxlDecoderSetProgressiveDetail(dec, detail);
			}

			public int GetIntendedDownsamplingRatio()
			{
				CheckIfDisposed();
				return (int)JxlDecoderGetIntendedDownsamplingRatio(dec);
			}

			public JxlDecoderStatus FlushImage()
			{
				CheckIfDisposed();
				return JxlDecoderFlushImage(dec);
			}
		}
	}
}
