//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    internal class NullMessageEncoderFactory : MessageEncoderFactory
    {
        public NullMessageEncoderFactory()
        {
            this.Encoder = new NullMessageEncoder();
        }

        public override MessageEncoder Encoder { get; }

        public override MessageVersion MessageVersion => this.Encoder.MessageVersion;
    }
}