using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ByteBuffer
    {
        private List<Section> _sections;
        private byte[] _buffer;
        private int _offset;
        private Stream _bufferedOutputStream;
        private BufferState _bufferState;
        private BufferedStream _writer;

        private enum BufferState
        {
            Created,
            Writing,
            Reading,
        }

        internal struct Section
        {
            private int _size;

            public Section(int offset, int size)
            {
                Offset = offset;
                _size = size;
            }

            public int Offset { get; }

            public int Size
            {
                get { return _size; }
            }
        }

        public ByteBuffer(int maxBufferSize)
        {
            if (maxBufferSize < 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(maxBufferSize), maxBufferSize,
                                                    PublicSR.ValueMustBeNonNegative));
            }

            int initialBufferSize = Math.Min(512, maxBufferSize);
            _bufferedOutputStream = BufferManager.CreateBufferManagerOutputStream(PublicSR.XmlBufferQuotaExceeded, initialBufferSize, maxBufferSize);
            _sections = new List<Section>(1);
        }

        public int BufferSize
        {
            get
            {
                Fx.Assert(_bufferState == BufferState.Reading, "Buffer size shuold only be retrieved during Reading state");
                return _buffer.Length;
            }
        }

        public int SectionCount
        {
            get { return _sections.Count; }
        }

        public BufferedStream OpenSection()
        {
            if (_bufferState != BufferState.Created)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            }

            _bufferState = BufferState.Writing;
            if (_writer != null)
            {
                // We always want to Dispose of the writer now; previously, writers could be reassigned 
                // to a new stream, with a new dictionary and session. 
                var thisWriter = _writer;
                thisWriter.Dispose();
                _writer = null;
            }
            _writer = new BufferedStream(_bufferedOutputStream);
            return _writer;
        }

        public void CloseSection()
        {
            if (_bufferState != BufferState.Writing)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            }

            _writer.Dispose();
            _writer = null;
            _bufferState = BufferState.Created;
            int size = (int)_bufferedOutputStream.Length - _offset;
            _sections.Add(new Section(_offset, size));
            _offset += size;
        }

        public void Close()
        {
            if (_bufferState != BufferState.Created)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            }

            _bufferState = BufferState.Reading;
            int bufferSize;
            _buffer = _bufferedOutputStream.ToArray(out bufferSize);
            _writer = null;
            _bufferedOutputStream = null;
        }

        private Exception CreateInvalidStateException()
        {
            return new InvalidOperationException(PublicSR.XmlBufferInInvalidState);
        }

        public Stream GetSection(int sectionIndex)
        {
            if (_bufferState != BufferState.Reading)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidStateException());
            }

            Section section = _sections[sectionIndex];

            return new MemoryStream(_buffer, section.Offset, section.Size, false);
        }
    }
}
