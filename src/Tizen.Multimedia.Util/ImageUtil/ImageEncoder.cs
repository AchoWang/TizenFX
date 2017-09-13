/*
 * Copyright (c) 2016 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Interop.ImageUtil;
using Unmanaged = Interop.ImageUtil.Encode;

namespace Tizen.Multimedia.Util
{
    /// <summary>
    /// This is a base class for image encoders.
    /// </summary>
    public abstract class ImageEncoder : IDisposable
    {
        private ImageEncoderHandle _handle;

        private bool _hasResolution;

        internal ImageEncoder(ImageFormat format)
        {
            Unmanaged.Create(format, out _handle).ThrowIfFailed("Failed to create ImageEncoder");

            Debug.Assert(_handle != null);

            OutputFormat = format;
        }

        private ImageEncoderHandle Handle
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return _handle;
            }
        }

        /// <summary>
        /// Gets the image format of this encoder.
        /// </summary>
        public ImageFormat OutputFormat { get; }

        /// <summary>
        /// Sets the resolution of the output image.
        /// </summary>
        /// <param name="resolution">The target resolution.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The width of <paramref name="resolution"/> is less than or equal to zero.\n
        ///     - or -\n
        ///     The height of <paramref name="resolution"/> is less than or equal to zero.
        /// </exception>
        public void SetResolution(Size resolution)
        {
            if (resolution.Width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution.Width,
                    "The width of resolution can't be less than or equal to zero.");
            }
            if (resolution.Height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution.Height,
                    "The height of resolution can't be less than or equal to zero.");
            }

            Unmanaged.SetResolution(Handle, (uint)resolution.Width, (uint)resolution.Height).
                ThrowIfFailed("Failed to set the resolution");

            _hasResolution = true;
        }

        /// <summary>
        /// Sets the color-space of the output image.
        /// </summary>
        /// <param name="colorSpace">The target color-space.</param>
        /// <exception cref="ArgumentException"><paramref name="colorSpace"/> is invalid.</exception>
        /// <exception cref="NotSupportedException"><paramref name="colorSpace"/> is not supported by the encoder.</exception>
        /// <seealso cref="ImageUtil.GetSupportedColorSpaces(ImageFormat)"/>
        public void SetColorSpace(ColorSpace colorSpace)
        {
            ValidationUtil.ValidateEnum(typeof(ColorSpace), colorSpace, nameof(colorSpace));

            if (ImageUtil.GetSupportedColorSpaces(OutputFormat).Contains(colorSpace) == false)
            {
                throw new NotSupportedException($"{colorSpace.ToString()} is not supported for {OutputFormat}.");
            }

            Unmanaged.SetColorspace(Handle, colorSpace.ToImageColorSpace()).
                ThrowIfFailed("Failed to set the color space");
        }

        private Task Run(Stream outStream)
        {
            var tcs = new TaskCompletionSource<bool>();

            IntPtr outBuffer = IntPtr.Zero;
            Unmanaged.SetOutputBuffer(Handle, out outBuffer).ThrowIfFailed("Failed to initialize encoder");

            Task.Run(() =>
            {
                try
                {
                    ulong size = 0;
                    Unmanaged.Run(Handle, out size).ThrowIfFailed("Failed to encode given image");

                    byte[] buf = new byte[size];
                    Marshal.Copy(outBuffer, buf, 0, (int)size);
                    outStream.Write(buf, 0, (int)size);

                    tcs.TrySetResult(true);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
                finally
                {
                    Interop.Libc.Free(outBuffer);
                }
            });

            return tcs.Task;
        }

        internal Task EncodeAsync(Action<ImageEncoderHandle> settingInputAction, Stream outStream)
        {
            Debug.Assert(settingInputAction != null);

            if (outStream == null)
            {
                throw new ArgumentNullException(nameof(outStream));
            }

            if (outStream.CanWrite == false)
            {
                throw new ArgumentException("The stream is not writable.", nameof(outStream));
            }

            Initialize();

            settingInputAction(Handle);

            return Run(outStream);
        }

        /// <summary>
        /// Encodes an image from a raw image buffer to a specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="inputBuffer">The image buffer to encode.</param>
        /// <param name="outStream">The stream that the image is encoded to.</param>
        /// <returns>A task that represents the asynchronous encoding operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="inputBuffer"/> is null.\n
        ///     - or -\n
        ///     <paramref name="outStream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="inputBuffer"/> is an empty array.\n
        ///     - or -\n
        ///     <paramref name="outStream"/> is not writable.\n
        /// </exception>
        /// <exception cref="InvalidOperationException">The resolution is not set.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ImageEncoder"/> has already been disposed of.</exception>
        /// <seealso cref="SetResolution"/>
        public Task EncodeAsync(byte[] inputBuffer, Stream outStream)
        {
            if (inputBuffer == null)
            {
                throw new ArgumentNullException(nameof(inputBuffer));
            }

            if (inputBuffer.Length == 0)
            {
                throw new ArgumentException("buffer is empty.", nameof(inputBuffer));
            }

            return EncodeAsync(handle =>
            {
                Unmanaged.SetInputBuffer(handle, inputBuffer).
                        ThrowIfFailed("Failed to configure encoder; InputBuffer");
            }, outStream);
        }

        internal void Initialize()
        {
            Configure(Handle);

            if (_hasResolution == false)
            {
                throw new InvalidOperationException("Resolution is not set.");
            }
        }

        internal abstract void Configure(ImageEncoderHandle handle);

        #region IDisposable Support
        private bool _disposed = false;

        /// <summary>
        /// Releases the unmanaged resources used by the ImageEncoder.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != null)
                {
                    _handle.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the ImageEncoder.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    /// <summary>
    /// Provides the ability to encode the Bitmap (BMP) format images.
    /// </summary>
    public class BmpEncoder : ImageEncoder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BmpEncoder"/> class.
        /// </summary>
        /// <remarks><see cref="ImageEncoder.OutputFormat"/> will be the <see cref="ImageFormat.Bmp"/>.</remarks>
        public BmpEncoder() : base(ImageFormat.Bmp)
        {
        }

        internal override void Configure(ImageEncoderHandle handle)
        {
        }
    }

    /// <summary>
    /// Provides the ability to encode the Portable Network Graphics (PNG) format images.
    /// </summary>
    public class PngEncoder : ImageEncoder
    {
        /// <summary>
        /// A read-only field that represents the default value of <see cref="Compression"/>.
        /// </summary>
        public static readonly PngCompression DefaultCompression = PngCompression.Level6;

        private PngCompression? _compression;

        /// <summary>
        /// Initializes a new instance of the <see cref="PngEncoder"/> class.
        /// </summary>
        /// <remarks><see cref="ImageEncoder.OutputFormat"/> will be the <see cref="ImageFormat.Png"/>.</remarks>
        public PngEncoder() :
            base(ImageFormat.Png)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PngEncoder"/> class with <see cref="PngCompression"/>.
        /// </summary>
        /// <remarks><see cref="ImageEncoder.OutputFormat"/> will be the <see cref="ImageFormat.Png"/>.</remarks>
        /// <param name="compression">The compression level of the encoder.</param>
        /// <exception cref="ArgumentException"><paramref name="compression"/> is invalid.</exception>
        public PngEncoder(PngCompression compression) :
            base(ImageFormat.Png)
        {
            Compression = compression;
        }

        /// <summary>
        /// Gets or sets the compression level of the png image.
        /// </summary>
        /// <value>The compression level. The default is <see cref="PngCompression.Level6"/>.</value>
        /// <exception cref="ArgumentException"><paramref name="value"/> is invalid.</exception>
        public PngCompression Compression
        {
            get { return _compression ?? DefaultCompression; }
            set
            {
                ValidationUtil.ValidateEnum(typeof(PngCompression), value, nameof(Compression));

                _compression = value;
            }
        }

        internal override void Configure(ImageEncoderHandle handle)
        {
            if (_compression.HasValue)
            {
                Unmanaged.SetPngCompression(handle, _compression.Value).
                    ThrowIfFailed("Failed to configure encoder; PngCompression");
            }
        }
    }

    /// <summary>
    /// Provides the ability to encode the Joint Photographic Experts Group (JPEG) format images.
    /// </summary>
    public class JpegEncoder : ImageEncoder
    {
        /// <summary>
        /// A read-only field that represents the default value of <see cref="Quality"/>.
        /// </summary>
        public static readonly int DefaultQuality = 75;

        private int? _quality;

        /// <summary>
        /// Initializes a new instance of the <see cref="JpegEncoder"/> class.
        /// </summary>
        /// <remarks><see cref="ImageEncoder.OutputFormat"/> will be the <see cref="ImageFormat.Jpeg"/>.</remarks>
        public JpegEncoder() : base(ImageFormat.Jpeg)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="JpegEncoder"/> class with initial quality value.
        /// </summary>
        /// <param name="quality">The quality for JPEG image encoding; from 1(lowest quality) to 100(highest quality).</param>
        /// <remarks><see cref="ImageEncoder.OutputFormat"/> will be the <see cref="ImageFormat.Jpeg"/>.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="quality"/> is less than or equal to 0.\n
        ///     - or -\n
        ///     <paramref name="quality"/> is greater than 100.
        /// </exception>
        public JpegEncoder(int quality) :
            base(ImageFormat.Jpeg)
        {
            Quality = quality;
        }

        /// <summary>
        /// Gets or sets the quality of the encoded image.
        /// </summary>
        /// <value>
        /// The quality of the output image. The default is 75.\n
        /// Valid value is from 1(lowest quality) to 100(highest quality).
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value"/> is less than or equal to 0.\n
        ///     - or -\n
        ///     <paramref name="value"/> is greater than 100.
        /// </exception>
        public int Quality
        {
            get { return _quality ?? DefaultQuality; }
            set
            {
                if (value <= 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(Quality), value,
                        "Valid range is from 1 to 100, inclusive.");
                }
                _quality = value;
            }
        }

        internal override void Configure(ImageEncoderHandle handle)
        {
            if (_quality.HasValue)
            {
                Unmanaged.SetQuality(handle, _quality.Value).
                    ThrowIfFailed("Failed to configure encoder; Quality");
            }
        }
    }

    /// <summary>
    /// Provides the ability to encode the Graphics Interchange Format (GIF) format images.
    /// </summary>
    public class GifEncoder : ImageEncoder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GifEncoder"/> class.
        /// </summary>
        /// <remarks><see cref="ImageEncoder.OutputFormat"/> will be the <see cref="ImageFormat.Gif"/>.</remarks>
        public GifEncoder() : base(ImageFormat.Gif)
        {
        }

        internal override void Configure(ImageEncoderHandle handle)
        {
        }

        /// <summary>
        /// Encodes a Graphics Interchange Format (GIF) image from multiple raw image buffers to a specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="frames">The image frames to encode.</param>
        /// <param name="outStream">The stream that the image is encoded to.</param>
        /// <returns>A task that represents the asynchronous encoding operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="frames"/> is null.\n
        ///     - or -\n
        ///     <paramref name="outStream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="frames"/> has no element(empty).\n
        ///     - or -\n
        ///     <paramref name="outStream"/> is not writable.\n
        /// </exception>
        /// <exception cref="InvalidOperationException">The resolution is not set.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ImageEncoder"/> has already been disposed of.</exception>
        /// <seealso cref="ImageEncoder.SetResolution"/>
        public Task EncodeAsync(IEnumerable<GifFrame> frames, Stream outStream)
        {
            if (frames == null)
            {
                throw new ArgumentNullException(nameof(frames));
            }

            if (frames.Count() == 0)
            {
                throw new ArgumentException("frames is a empty collection", nameof(frames));
            }

            return EncodeAsync(handle =>
            {
                foreach (GifFrame frame in frames)
                {
                    if (frame == null)
                    {
                        throw new ArgumentNullException(nameof(frames));
                    }
                    Unmanaged.SetInputBuffer(handle, frame.Buffer).
                        ThrowIfFailed("Failed to configure encoder; Buffer");

                    Unmanaged.SetGifFrameDelayTime(handle, (ulong)frame.Delay).
                        ThrowIfFailed("Failed to configure encoder; Delay");
                }
            }, outStream);
        }
    }

}
