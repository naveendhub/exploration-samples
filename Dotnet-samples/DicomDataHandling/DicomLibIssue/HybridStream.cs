// Copyright Koninklijke Philips N.V. 2022

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DicomLibIssue
{
    /// <summary>
    /// Stream implementation that inteligently switches from Memory Stream to temp File backed Stream
    /// Stream starts with memory backed till it reaches the configured maxMemSize, after that a 
    /// temporary file is created on the disk and all content from memory is copied to temp file .
    /// Upon disposing the stream temporary file is deleted
    /// Note: Inspired from ASP.NET Core FileBufferingReadStream
    /// (Microsoft.AspNetCore.WebUtilities.FileBufferingReadStream)
    /// </summary>
    /// 
    public sealed class HybridStream : Stream {
        const int defaultMemorySize = 1024 * 1024;


        private Stream stream = null;
        private readonly string tempDirectory;

        #region Properties Implementation

        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        internal int MaxThreshold {
            get;
            set;
        }

        private string filePath;
        internal string FilePath { get => filePath; }

        /// <summary>
        /// Can read from the stream or not
        /// </summary>
        public override bool CanRead {
            get {
                ThrowIfObjectDisposed();

                return stream.CanRead;
            }
        }

        /// <summary>
        /// Can write to the stream or not
        /// </summary>
        public override bool CanWrite {
            get {
                ThrowIfObjectDisposed();

                return stream.CanWrite;
            }
        }

        /// <summary>
        /// Can Seek the stream or not
        /// </summary>
        public override bool CanSeek {
            get {
                ThrowIfObjectDisposed();

                return stream.CanSeek;
            }
        }

        /// <summary>
        /// Length of the Stream
        /// </summary>
        public override long Length {
            get {
                ThrowIfObjectDisposed();

                return stream.Length;
            }
        }

        /// <summary>
        /// Position
        /// </summary>
        public override long Position {
            get {
                ThrowIfObjectDisposed();

                return stream.Position;
            }

            set {
                ThrowIfObjectDisposed();

                stream.Position = value;
            }
        }

        /// <summary>
        /// Can timeout
        /// </summary>
        public override bool CanTimeout {
            get {
                ThrowIfObjectDisposed();

                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Read Timeout
        /// </summary>
        public override int ReadTimeout {
            get {
                ThrowIfObjectDisposed();

                throw new NotSupportedException();
            }

            set {
                ThrowIfObjectDisposed();

                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Write timeout
        /// </summary>
        public override int WriteTimeout {
            get {
                ThrowIfObjectDisposed();

                throw new NotSupportedException();
            }

            set {
                ThrowIfObjectDisposed();

                throw new NotSupportedException();
            }
        }

        #endregion

        /// <summary>
        /// ctor
        /// </summary>
        public HybridStream()
            : this(defaultMemorySize) {

        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="memSize">Maximum bytes store to memory, before switching over to temporary file.</param>
        public HybridStream(int memSize) {
            MaxThreshold = memSize;

            stream = new MemoryStream();
            tempDirectory = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
            if (!Directory.Exists(tempDirectory)) {
                throw new DirectoryNotFoundException(tempDirectory);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            if (this.IsDisposed) {
                return;
            }

            this.IsDisposed = true;

            if (stream != null) {
                stream.Dispose();
                stream = null;
            }

            base.Dispose(disposing);
        }

        private void ThrowIfObjectDisposed() {
            if (this.IsDisposed) {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }


        #region Override methods

        /// <summary>
        /// Seek the stream
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) {
            ThrowIfObjectDisposed();

            return stream.Seek(offset, origin);
        }

        /// <summary>
        /// Set length
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) {
            ThrowIfObjectDisposed();

            stream.SetLength(value);
        }

        /// <summary>
        ///  Flush Synchronously
        /// </summary>
        public override void Flush() {
            ThrowIfObjectDisposed();

            stream.Flush();
        }

        /// <summary>
        /// Flush Asynchronously
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task FlushAsync(CancellationToken cancellationToken) {
            ThrowIfObjectDisposed();

            return stream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Read byte
        /// </summary>
        /// <returns></returns>
        public override int ReadByte() {
            ThrowIfObjectDisposed();

            return stream.ReadByte();
        }

        /// <summary>
        /// Read stream Synchronously
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override int Read(byte[] buffer, int offset, int count) {
            ThrowIfObjectDisposed();

            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"Argument '{nameof(offset)}' value must be >= 0.");
            }

            if (offset > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"Argument '{nameof(offset)}'" +
                    $" value exceeds the maximum length of argument '{nameof(buffer)}'.");
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count),
                $"Argument '{nameof(count)}' value must be >= 0.");
            }

            if (offset + count > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"Argument '{nameof(offset)} + {nameof(count)}'" +
                    $" value exceeds the maximum length of argument '{nameof(buffer)}'.");
            }


            return stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Read stream Asynchronously
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            ThrowIfObjectDisposed();

            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Write a byte to the stream
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value) {
            ThrowIfObjectDisposed();

            stream.Write(new[] { value }, 0, 1);
        }

        /// <summary>
        /// Write the bytes buffer to stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override void Write(byte[] buffer, int offset, int count) {
            ThrowIfObjectDisposed();

            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"Argument '{nameof(offset)}' value must be >= 0.");
            }

            if (offset > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"Argument '{nameof(offset)}' value exceeds the maximum length of argument '{nameof(buffer)}'.");
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"Argument '{nameof(count)}' value must be >= 0.");
            }

            if (offset + count > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"Argument '{nameof(offset)} + {nameof(count)}'" +
                    $" value exceeds the maximum length of argument '{nameof(buffer)}'.");
            }


            // Switch from memory to temporary file backed stream.
            if (stream is MemoryStream && (stream.Position + count) > MaxThreshold) {
                filePath = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}" + ".tmp");
                var fs = new FileStream(
                    FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 4096, FileOptions.DeleteOnClose);

                stream.Position = 0;
                stream.CopyTo(fs);
                stream.Dispose();
                stream = fs;
            }

            stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Write Asynchronously
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            ThrowIfObjectDisposed();

            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Copy to Synchronously
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="bufferSize"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override void CopyTo(Stream destination, int bufferSize) {
            ThrowIfObjectDisposed();

            if (destination == null) {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException(nameof(bufferSize),
                    $"Argument '{nameof(bufferSize)}' value must be > 0.");
            }


            stream.CopyTo(destination, bufferSize);
        }

        /// <summary>
        /// Copy Asynchronously
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
            ThrowIfObjectDisposed();

            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        #endregion
    }
}
