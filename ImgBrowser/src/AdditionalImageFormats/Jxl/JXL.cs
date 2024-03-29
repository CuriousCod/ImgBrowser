﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace JxlSharp
{
	public static class JXL
	{
		internal static int GetBytesPerPixel(PixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case PixelFormat.Format16bppArgb1555:
				case PixelFormat.Format16bppGrayScale:
				case PixelFormat.Format16bppRgb555:
				case PixelFormat.Format16bppRgb565:
					return 2;
				case PixelFormat.Format64bppArgb:
				case PixelFormat.Format64bppPArgb:
					return 8;
				case PixelFormat.Format48bppRgb:
					return 6;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
				case PixelFormat.Format32bppRgb:
					return 4;
				case PixelFormat.Format24bppRgb:
					return 3;
				case PixelFormat.Format8bppIndexed:
					return 1;
				case PixelFormat.Format1bppIndexed:
				case PixelFormat.Format4bppIndexed:
					throw new NotSupportedException();
				default:
					throw new NotSupportedException();
			}
		}

		public static Bitmap LoadImage(string fileName)
		{
			return LoadImage(File.ReadAllBytes(fileName));
		}

		public static JxlBasicInfo GetBasicInfo(byte[] data, out bool canTranscodeToJpeg)
		{
			using (var jxlDecoder = new JxlDecoder())
			{
				JxlBasicInfo basicInfo = null;
				canTranscodeToJpeg = false;
				jxlDecoder.SetInput(data);
				jxlDecoder.SubscribeEvents(JxlDecoderStatus.BasicInfo | JxlDecoderStatus.JpegReconstruction | JxlDecoderStatus.Frame);

				while (true)
				{
					var status = jxlDecoder.ProcessInput();

					switch (status)
					{
						case JxlDecoderStatus.BasicInfo:
						{
							status = jxlDecoder.GetBasicInfo(out basicInfo);
							if (status != JxlDecoderStatus.Success)
							{
								return null;
							}

							break;
						}
						case JxlDecoderStatus.JpegReconstruction:
							canTranscodeToJpeg = true;
							break;
						case JxlDecoderStatus.Frame:
							return basicInfo;
						case JxlDecoderStatus.Success:
							return basicInfo;
						default:
						{
							if (status >= JxlDecoderStatus.Error && status < JxlDecoderStatus.BasicInfo)
							{
								return null;
							}

							if (status < JxlDecoderStatus.BasicInfo)
							{
								return basicInfo;
							}

							break;
						}
					}
				}
			}
		}

		private static void BgrSwap(int width, int height, int bytesPerPixel, IntPtr scan0, int stride)
		{
			unsafe
			{
				if (bytesPerPixel == 3)
				{
					for (int y = 0; y < height; y++)
					{
						byte* p = (byte*)scan0 + stride * y;
						for (int x = 0; x < width; x++)
						{
							byte r = p[2];
							byte b = p[0];
							p[0] = r;
							p[2] = b;
							p += 3;
						}
					}
				}
				else if (bytesPerPixel == 4)
				{
					for (int y = 0; y < height; y++)
					{
						byte* p = (byte*)scan0 + stride * y;
						for (int x = 0; x < width; x++)
						{
							byte r = p[2];
							byte b = p[0];
							p[0] = r;
							p[2] = b;
							p += 4;
						}
					}
				}
			}
		}

		private static void BgrSwap(BitmapData bitmapData)
		{
			int bytesPerPixel = 4;
			switch (bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
				case PixelFormat.Format32bppRgb:
					bytesPerPixel = 4;
					break;
				case PixelFormat.Format24bppRgb:
					bytesPerPixel = 3;
					break;
				default:
					return;
			}
			BgrSwap(bitmapData.Width, bitmapData.Height, bytesPerPixel, bitmapData.Scan0, bitmapData.Stride);
		}
		
		private static void BgrSwap(Bitmap bitmap)
		{
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
			try
			{
				BgrSwap(bitmapData);
			}
			finally
			{
				bitmap.UnlockBits(bitmapData);
			}
		}
		
		private static void SetGrayscalePalette(Bitmap bitmap)
		{
			var palette = bitmap.Palette;
			for (int i = 0; i < 256; i++)
			{
				palette.Entries[i] = Color.FromArgb(i, i, i);
			}
			bitmap.Palette = palette;
		}

		public static bool LoadImageIntoBitmap(byte[] data, BitmapData bitmapData)
		{
			return LoadImageIntoMemory(data, bitmapData.Width, bitmapData.Height, GetBytesPerPixel(bitmapData.PixelFormat), bitmapData.Scan0, bitmapData.Stride, true);
		}

		public static bool LoadImageIntoBitmap(byte[] data, Bitmap bitmap)
		{
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
			if (bitmapData.Stride < 0)
			{
				throw new NotSupportedException("Stride can not be negative");
			}
			try
			{
				bool okay = LoadImageIntoBitmap(data, bitmapData);
				if (okay)
				{
					if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						SetGrayscalePalette(bitmap);
					}
				}
				return okay;
			}
			finally
			{
				bitmap.UnlockBits(bitmapData);
			}
		}

		public static bool LoadImageIntoMemory(byte[] data, int width, int height, int bytesPerPixel, IntPtr scan0, int stride, bool doBgrSwap)
		{
			if (stride < 0) throw new NotSupportedException("Stride can not be negative");
			if (bytesPerPixel < 0 || bytesPerPixel > 4) throw new NotSupportedException("bytesPerPixel must be between 1 and 4");

			JxlBasicInfo basicInfo;
			using (var jxlDecoder = new JxlDecoder())
			{
				jxlDecoder.SetInput(data);
				jxlDecoder.SubscribeEvents(JxlDecoderStatus.BasicInfo | JxlDecoderStatus.Frame | JxlDecoderStatus.FullImage);
				while (true)
				{
					var status = jxlDecoder.ProcessInput();
					switch (status)
					{
						case JxlDecoderStatus.BasicInfo:
						{
							status = jxlDecoder.GetBasicInfo(out basicInfo);
							if (status == JxlDecoderStatus.Success)
							{
								if (width != basicInfo.Width || height != basicInfo.Height)
								{
									return false;
								}
							}
							else
							{
								return false;
							}

							break;
						}
						case JxlDecoderStatus.Frame:
						{
							//PixelFormat bitmapPixelFormat = PixelFormat.Format32bppArgb;
							JxlPixelFormat pixelFormat = new JxlPixelFormat();
							pixelFormat.DataType = JxlDataType.UInt8;
							pixelFormat.Endianness = JxlEndianness.NativeEndian;
							pixelFormat.NumChannels = bytesPerPixel;

							pixelFormat.Align = stride;
							status = jxlDecoder.SetImageOutBuffer(pixelFormat, scan0, stride * height);
							if (status != JxlDecoderStatus.Success)
							{
								return false;
							}
							status = jxlDecoder.ProcessInput();
							if (status > JxlDecoderStatus.Success && status < JxlDecoderStatus.BasicInfo)
							{
								return false;
							}
							if (doBgrSwap && bytesPerPixel >= 3)
							{
								BgrSwap(width, height, bytesPerPixel, scan0, stride);
							}
							return true;
						}
						case JxlDecoderStatus.FullImage:
							0.GetHashCode();
							break;
						case JxlDecoderStatus.Success:
							return true;
						default:
						{
							if (status > JxlDecoderStatus.Success && status < JxlDecoderStatus.BasicInfo)
							{
								return false;
							}

							break;
						}
					}
				}
			}
		}

		public static PixelFormat SuggestPixelFormat(JxlBasicInfo basicInfo)
		{
			bool isColor = basicInfo.NumColorChannels > 1;
			bool hasAlpha = basicInfo.AlphaBits > 0;
			PixelFormat bitmapPixelFormat = PixelFormat.Format32bppArgb;
			if (isColor)
			{
				bitmapPixelFormat = hasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
			}
			else
			{
				bitmapPixelFormat = hasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format8bppIndexed;
			}
			return bitmapPixelFormat;
		}
		private static Bitmap CreateBlankBitmap(JxlBasicInfo basicInfo)
		{
			PixelFormat bitmapPixelFormat = SuggestPixelFormat(basicInfo);
			Bitmap bitmap = new Bitmap(basicInfo.Width, basicInfo.Height, bitmapPixelFormat);
			return bitmap;
		}

		public static Bitmap LoadImage(byte[] data)
		{
			Bitmap bitmap = null;
			JxlBasicInfo basicInfo = GetBasicInfo(data, out _);
			if (basicInfo == null)
			{
				return null;
			}
			bitmap = CreateBlankBitmap(basicInfo);
			if (!LoadImageIntoBitmap(data, bitmap))
			{
				if (bitmap != null)
				{
					bitmap.Dispose();
				}
				return null;
			}
			return bitmap;
		}

		public static byte[] TranscodeJxlToJpeg(byte[] jxlBytes)
		{
			byte[] buffer = new byte[0];
			int outputPosition = 0;
			//byte[] buffer = new byte[1024 * 1024];
			using (var jxlDecoder = new JxlDecoder())
			{
				jxlDecoder.SetInput(jxlBytes);
				JxlBasicInfo basicInfo = null;
				bool canTranscodeToJpeg = false;
				jxlDecoder.SubscribeEvents(JxlDecoderStatus.BasicInfo | JxlDecoderStatus.JpegReconstruction | JxlDecoderStatus.Frame | JxlDecoderStatus.FullImage);

				while (true)
				{
					var status = jxlDecoder.ProcessInput();

					if (status == JxlDecoderStatus.BasicInfo)
					{
						status = jxlDecoder.GetBasicInfo(out basicInfo);
					}
					else if (status == JxlDecoderStatus.JpegReconstruction)
					{
						canTranscodeToJpeg = true;
						buffer = new byte[1024 * 1024];
						jxlDecoder.SetJPEGBuffer(buffer, outputPosition);
					}
					else if (status == JxlDecoderStatus.JpegNeedMoreOutput)
					{
						outputPosition += buffer.Length - jxlDecoder.ReleaseJPEGBuffer();
						byte[] nextBuffer = new byte[buffer.Length * 4];
						if (outputPosition > 0)
						{
							Array.Copy(buffer, 0, nextBuffer, 0, outputPosition);
						}
						buffer = nextBuffer;
						jxlDecoder.SetJPEGBuffer(buffer, outputPosition);
					}
					else if (status == JxlDecoderStatus.Frame)
					{
						//if (!canTranscodeToJpeg)
						//{
						//	return null;
						//}
					}
					else if (status == JxlDecoderStatus.Success)
					{
						outputPosition += buffer.Length - jxlDecoder.ReleaseJPEGBuffer();
						byte[] jpegBytes;
						if (buffer.Length == outputPosition)
						{
							jpegBytes = buffer;
						}
						else
						{
							jpegBytes = new byte[outputPosition];
							Array.Copy(buffer, 0, jpegBytes, 0, outputPosition);
						}

						jxlDecoder.Reset();

						return jpegBytes;
					}
					else if (status == JxlDecoderStatus.NeedImageOutBuffer)
					{
						return null;
					}
					else if (status >= JxlDecoderStatus.Error && status < JxlDecoderStatus.BasicInfo)
					{
						return null;
					}
					else if (status < JxlDecoderStatus.BasicInfo)
					{
						return null;
					}
				}
			}
		}

		public static byte[] TranscodeJpegToJxl(byte[] jpegBytes)
		{
			MemoryStream ms = new MemoryStream();
			JxlEncoderStatus status;
			using (var encoder = new JxlEncoder(ms))
			{
				status = encoder.StoreJPEGMetadata(true);
				status = encoder.AddJPEGFrame(encoder.FrameSettings, jpegBytes);
				encoder.CloseFrames();
				encoder.CloseInput();
				status = encoder.ProcessOutput();
				if (status == JxlEncoderStatus.Success)
				{
					return ms.ToArray();
				}
				return null;
			}
		}

		private static void CreateBasicInfo(Bitmap bitmap, out JxlBasicInfo basicInfo, out JxlPixelFormat pixelFormat, out JxlColorEncoding colorEncoding)
		{
			basicInfo = new JxlBasicInfo();
			pixelFormat = new JxlPixelFormat();
			pixelFormat.DataType = JxlDataType.UInt8;
			pixelFormat.Endianness = JxlEndianness.NativeEndian;
			if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb)
			{
				basicInfo.AlphaBits = 8;
				basicInfo.NumColorChannels = 3;
				basicInfo.NumExtraChannels = 1;
				if (bitmap.PixelFormat == PixelFormat.Format32bppPArgb)
				{
					basicInfo.AlphaPremultiplied = true;
				}
				pixelFormat.NumChannels = 4;
			}
			else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
			{
				basicInfo.NumColorChannels = 1;
				pixelFormat.NumChannels = 1;
			}
			else
			{
				basicInfo.NumColorChannels = 3;
				pixelFormat.NumChannels = 3;
			}
			basicInfo.BitsPerSample = 8;
			basicInfo.IntrinsicWidth = bitmap.Width;
			basicInfo.IntrinsicHeight = bitmap.Height;
			basicInfo.Width = bitmap.Width;
			basicInfo.Height = bitmap.Height;
			colorEncoding = new JxlColorEncoding();
			bool isGray = basicInfo.NumColorChannels == 1;
			colorEncoding.SetToSRGB(isGray);

		}

		private static byte[] CopyBitmapAndBgrSwap(Bitmap bitmap, bool hasAlpha)
		{
			if (hasAlpha)
			{
				byte[] newBytes = new byte[bitmap.Width * bitmap.Height * 4];
				BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				try
				{
					unsafe
					{
						fixed (byte* pBytes = newBytes)
						{
							for (int y = 0; y < bitmap.Height; y++)
							{
								byte* src = (byte*)bitmapData.Scan0 + bitmapData.Stride * y;
								byte* dest = pBytes + bitmap.Width * 4 * y;
								for (int x = 0; x < bitmap.Width; x++)
								{
									dest[0] = src[2];
									dest[1] = src[1];
									dest[2] = src[0];
									dest[3] = src[3];
									dest += 4;
									src += 4;
								}
							}
						}
					}
				}
				finally
				{
					bitmap.UnlockBits(bitmapData);
				}
				return newBytes;
			}
			else
			{
				byte[] newBytes = new byte[bitmap.Width * bitmap.Height * 3];
				BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				try
				{
					unsafe
					{
						fixed (byte* pBytes = newBytes)
						{
							for (int y = 0; y < bitmap.Height; y++)
							{
								byte* src = (byte*)bitmapData.Scan0 + bitmapData.Stride * y;
								byte* dest = pBytes + bitmap.Width * 3 * y;
								for (int x = 0; x < bitmap.Width; x++)
								{
									dest[0] = src[2];
									dest[1] = src[1];
									dest[2] = src[0];
									dest += 3;
									src += 3;
								}
							}
						}
					}
				}
				finally
				{
					bitmap.UnlockBits(bitmapData);
				}
				return newBytes;
			}
		}

		public static byte[] EncodeJxl(Bitmap bitmap, JxlLossyMode lossyMode, float frameDistance, IDictionary<JxlEncoderFrameSettingId, long> settings)
		{
			JxlEncoderStatus status;
			MemoryStream ms = new MemoryStream();
			using (var encoder = new JxlEncoder(ms))
			{
				JxlBasicInfo basicInfo;
				JxlPixelFormat pixelFormat;
				JxlColorEncoding colorEncoding;
				CreateBasicInfo(bitmap, out basicInfo, out pixelFormat, out colorEncoding);
				bool hasAlpha = basicInfo.AlphaBits > 0;
				byte[] bitmapCopy = CopyBitmapAndBgrSwap(bitmap, hasAlpha);
				if (lossyMode != JxlLossyMode.Lossless)
				{
					basicInfo.UsesOriginalProfile = false;
				}
				status = encoder.SetBasicInfo(basicInfo);
				status = encoder.SetColorEncoding(colorEncoding);
				foreach (var pair in settings)
				{
					status = encoder.FrameSettings.SetOption(pair.Key, pair.Value);
				}
				if (lossyMode == JxlLossyMode.Lossless)
				{
					status = encoder.FrameSettings.SetFrameLossless(true);
					status = encoder.FrameSettings.SetFrameDistance(0);
					status = encoder.FrameSettings.SetOption(JxlEncoderFrameSettingId.Modular, 1); 
				}
				else
				{
					status = encoder.FrameSettings.SetFrameDistance(frameDistance);
					status = encoder.FrameSettings.SetFrameLossless(false);
					if (lossyMode == JxlLossyMode.Photo)
					{
						status = encoder.FrameSettings.SetOption(JxlEncoderFrameSettingId.Modular, 0);
					}
					else if (lossyMode == JxlLossyMode.Drawing)
					{
						status = encoder.FrameSettings.SetOption(JxlEncoderFrameSettingId.Modular, 1);
					}
				}
				status = encoder.AddImageFrame(encoder.FrameSettings, pixelFormat, bitmapCopy);
				encoder.CloseFrames();
				encoder.CloseInput();
				status = encoder.ProcessOutput();

				byte[] bytes = null;
				if (status == JxlEncoderStatus.Success)
				{
					bytes = ms.ToArray();
				}
				return bytes;
			}
		}
	}

	/// <summary>
	/// Lossless/Lossy Mode for JXL.EncodeJxl
	/// </summary>
	public enum JxlLossyMode
	{
		/// <summary>
		/// Lossless mode
		/// </summary>
		Lossless = 0,
		/// <summary>
		/// Automatic selection
		/// </summary>
		Default = 1,
		/// <summary>
		/// VarDCT mode (like JPEG)
		/// </summary>
		Photo = 2,
		/// <summary>
		/// Modular Mode for drawn images, not for things that have previously been saved as JPEG.
		/// </summary>
		Drawing = 3,
	}
}
