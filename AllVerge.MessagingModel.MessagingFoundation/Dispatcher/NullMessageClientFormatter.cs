using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatcher
{
    internal class NullMessageClientFormatter : IClientMessageFormatter
    {
        public object DeserializeReply(Message message, object[] parameters)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                new NotSupportedException(
                    PublicSR.Format(
                        PublicSR.SerializingReplyNotSupportedByFormatter, nameof(NullMessageClientFormatter))));
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            if (messageVersion != MessageVersion.None)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new NotSupportedException(
                        AMMMFR.Format(
                            AMMMFR.SerializingMessageVersionNotSupportedByFormatter, nameof(NullMessageClientFormatter), messageVersion)));

            if (parameters.Length != 0)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new NotSupportedException(
                        AMMMFR.Format(
                            AMMMFR.SerializingParametersNotSupportedByFormatter, nameof(NullMessageClientFormatter))));

            return new NullMessage();
        }
    }
}
