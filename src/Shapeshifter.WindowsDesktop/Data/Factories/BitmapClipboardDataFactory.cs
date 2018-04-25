﻿namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System;
	using System.Runtime.InteropServices;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using Data.Interfaces;

    using Interfaces;

    using Native;

    using Services.Clipboard.Interfaces;
	using Shapeshifter.WindowsDesktop.Helpers;
	using Shapeshifter.WindowsDesktop.Services.Images.Interfaces;
	using static Shapeshifter.WindowsDesktop.Native.ImageNativeApi;

	class BitmapClipboardDataFactory: IBitmapClipboardDataFactory
    {
        readonly IDataSourceService dataSourceService;
		readonly IImagePersistenceService imagePersistenceService;

		public BitmapClipboardDataFactory(
            IDataSourceService dataSourceService,
			IImagePersistenceService imagePersistenceService)
        {
            this.dataSourceService = dataSourceService;
			this.imagePersistenceService = imagePersistenceService;
		}

		BitmapSource DIBV5ToBitmapSource(byte[] allBytes)
		{
			var fileHeaderLength = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
			var dibv5Bytes = new byte[allBytes.Length - fileHeaderLength];
			Array.Copy(allBytes, fileHeaderLength, dibv5Bytes, 0, dibv5Bytes.Length);

			var bmi = BinaryStructHelper.FromByteArray<BITMAPV5HEADER>(dibv5Bytes);
			var imageBytes = GetImageBytesFromAllBytes(dibv5Bytes, bmi);
			var stride = GetStrideFromBitmapHeader(bmi);

			var reversedImageBytes = new byte[imageBytes.Length];
			for (int pBuf = imageBytes.Length, pMap = 0; pBuf > 0; pMap += stride, pBuf -= stride)
				Array.Copy(imageBytes, pMap, reversedImageBytes, pBuf - stride, stride);

			var bmpSource = BitmapSource.Create(
				bmi.bV5Width, bmi.bV5Height,
				bmi.bV5XPelsPerMeter, bmi.bV5YPelsPerMeter,
				GetPixelFormatFromBitsPerPixel(bmi.bV5BitCount), null,
				reversedImageBytes, stride);

			return bmpSource;
		}

		static int GetStrideFromBitmapHeader(BITMAPV5HEADER bmi)
		{
			return (int)(bmi.bV5SizeImage / bmi.bV5Height);
		}

		static byte[] GetImageBytesFromAllBytes(byte[] bytes, BITMAPV5HEADER bmi)
		{
			var stride = GetStrideFromBitmapHeader(bmi);
			var offset = bmi.bV5Size + Marshal.SizeOf(typeof(BITMAPFILEHEADER)) + bmi.bV5ClrUsed * Marshal.SizeOf<RGBQUAD>();
			if (bmi.bV5Compression == (uint)BitmapCompressionMode.BI_BITFIELDS)
			{
				offset += 12;
			}

			var imageBytes = new byte[bmi.bV5SizeImage];
			Array.Copy(bytes, offset, imageBytes, 0, imageBytes.Length);

			return imageBytes;
		}

		PixelFormat GetPixelFormatFromBitsPerPixel(ushort bitsPerPixel)
		{
			using (CrossThreadLogContext.Add(nameof(bitsPerPixel), bitsPerPixel))
			{
				switch (bitsPerPixel)
				{
					case 2:
						return PixelFormats.BlackWhite;

					case 8:
						return PixelFormats.Gray8;

					case 16:
						return PixelFormats.Gray16;

					case 24:
						return PixelFormats.Bgr24;

					case 32:
						return PixelFormats.Bgra32;

					default:
						throw new InvalidOperationException("Could not recognize the pixel format.");
				}
			}
		}

		public IClipboardData BuildData(uint format, byte[] rawData)
        {
            if (!CanBuildData(format))
            {
                throw new InvalidOperationException("The given format is not supported.");
            }

            return new ClipboardImageData(dataSourceService)
            {
                RawData = rawData,
                RawFormat = format,
				Image = imagePersistenceService.ConvertBitmapSourceToByteArray(
					DIBV5ToBitmapSource(rawData))
            };
        }

        public bool CanBuildData(uint format)
        {
            return
                format == ClipboardNativeApi.CF_BITMAP;
        }
    }
}