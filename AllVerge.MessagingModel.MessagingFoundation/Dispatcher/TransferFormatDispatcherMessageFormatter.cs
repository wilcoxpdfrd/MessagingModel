//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using AllVerge.MessagingModel.MessagingFoundation.Channels;
using System;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatcher
{
    internal class TransferFormatDispatcherMessageFormatter
    {
        internal static bool TryGetTransferFormat(Message message, out MessageEncodingFormat format)
        {
            message.Properties.TryGetValue(MessageEncodingFormatProperty.Name, out object prop);
            MessageEncodingFormatProperty formatProperty = prop as MessageEncodingFormatProperty;
            if (formatProperty == null)
            {
                format = MessageEncodingFormat.Default;
                return false;
            }
            format = formatProperty.Format;
            return true;
        }
    }
}