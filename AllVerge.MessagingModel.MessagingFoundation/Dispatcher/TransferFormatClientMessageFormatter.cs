//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.Globalization;
    using System.ServiceModel.Dispatcher;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using System.Linq;
    using System.Text;
    using AllVerge.SystemPrimitives.Net.Mime;
    using System.Net;
    using System.ServiceModel.Web;

    class TransferFormatClientMessageFormatter : IClientMessageFormatter
    {
        IClientMessageFormatter transferFormatFormatter;
        private MessageEncodingFormat? transferFormat;
        MessageEncodingFormat[] supportedFormats;
        private MessageEncodingFormat[] acceptTransferFormats;

        public TransferFormatClientMessageFormatter(IClientMessageFormatter transferFormatFormatter, params MessageEncodingFormat[] supportedFormats)
        {
            if (transferFormatFormatter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(transferFormatFormatter));
            }
            if (supportedFormats.Length == 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(supportedFormats));
            }
            this.transferFormatFormatter = transferFormatFormatter;
            this.supportedFormats = supportedFormats;
        }

        public TransferFormatClientMessageFormatter(IClientMessageFormatter transferFormatFormatter, MessageEncodingFormat? transferFormat, params MessageEncodingFormat[] supportedFormats) :
            this(transferFormatFormatter, supportedFormats)
        {
            this.transferFormat = transferFormat;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            if (message == null)
            {
                return null;
            }

            if (TransferFormatDispatcherMessageFormatter.TryGetTransferFormat(message, out MessageEncodingFormat format))
            {
                if (!supportedFormats.Contains(format))
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperWarning(
                        new InvalidOperationException(
                            AMMMFR.Format(
                                AMMMFR.UnrecognizedHttpMessageFormat, format, GetSupportedFormats())));
            }

            return this.transferFormatFormatter.DeserializeReply(message, parameters);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            Message message = this.transferFormatFormatter.SerializeRequest(messageVersion, parameters);
            if (message != null)
            {
                if (this.transferFormat != null)
                {
                    AddRequestContentTypeProperty(message, this.transferFormat.Value);
                }
                if (this.supportedFormats.Length > 0)
                {
                    AddAcceptProperty(message, this.supportedFormats);
                }
            }
            return message;
        }

        static void AddRequestContentTypeProperty(Message message, MessageEncodingFormat contentType)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(message));
            }
            message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object prop);
            HttpRequestMessageProperty httpProperty;
            if (prop != null)
            {
                httpProperty = (HttpRequestMessageProperty)prop;
            }
            else
            {
                httpProperty = new HttpRequestMessageProperty();
                message.Properties.Add(HttpRequestMessageProperty.Name, httpProperty);
            }
            if (string.IsNullOrEmpty(httpProperty.Headers[HttpRequestHeader.ContentType]))
            {
                httpProperty.Headers[HttpRequestHeader.ContentType] = contentType.CreateMessageContentType(out _).ToMediaTypePlusCharSet();
            }
        }

        private void AddAcceptProperty(Message message, MessageEncodingFormat[] accept)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
            }
            if (accept == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("accept");
            }
            if (accept.Length == 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("accept", "At least one accept value is required.");
            }
            message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object prop);
            HttpRequestMessageProperty httpProperty;
            if (prop != null)
            {
                httpProperty = (HttpRequestMessageProperty)prop;
            }
            else
            {
                httpProperty = new HttpRequestMessageProperty();
                message.Properties.Add(HttpRequestMessageProperty.Name, httpProperty);
            }
            if (string.IsNullOrEmpty(httpProperty.Headers[HttpRequestHeader.Accept]))
            {
                httpProperty.Headers[HttpRequestHeader.Accept] = String.Join(",", accept.Select(a => a.CreateMessageContentType(out _).ToString()).ToArray());
            }
        }

        MessageEncodingFormat[] GetSupportedFormats()
        {
            return this.supportedFormats;
        }
    }
}

