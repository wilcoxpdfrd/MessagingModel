using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public interface IMessagingContentTypeMapper
    {
        MessageEncodingFormat GetTransferMessageEncodingForContentType(string contentType, AddressingVersion addressingVersion = null);
        
        String GetContentTypeForTransferMessageEncoding(MessageEncodingFormat transferFormat, out MessageVersion messageVersion);
        
    }
}
