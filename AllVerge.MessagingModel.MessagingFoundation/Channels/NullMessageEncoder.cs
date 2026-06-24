//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using System.IO;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    internal class NullMessageEncoder : MessageEncoder
    {
        public override string ContentType => null;

        public override string MediaType => null;

        public override MessageVersion MessageVersion => MessageVersion.None;

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            return new NullMessage();
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            return new NullMessage();
        }

        public override void WriteMessage(Message message, Stream stream)
        {
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            return new ArraySegment<byte>(Array.Empty<byte>());
        }
    }
}