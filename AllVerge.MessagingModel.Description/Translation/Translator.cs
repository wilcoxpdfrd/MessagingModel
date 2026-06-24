using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using System.Net;
using System.Text.RegularExpressions;
using AllVerge.MessagingModel.Description.Model;

namespace AllVerge.MessagingModel.Description.Translation
{
    internal class Translator : ITranslator
    {
        private String groupUri;
        private String dispatchAction;
        private String messageStyle;
        private Connector dispatchConnector;
        private Connection dispatchConnection;
        private Interaction dispatchInteraction;
        private String dispatchTargetNamespace;

        //XmlDictionaryReader reader;
        //XmlDictionaryWriter writer;

        public Translator(String groupUri, String dispatchAction, String messageStyle)
        {
            if (!DescriptionUtils.TryGetManagedDesriptionObjectsForDispatchAction(groupUri, dispatchAction, messageStyle, out this.dispatchConnector, out this.dispatchConnection, out this.dispatchInteraction, out this.dispatchTargetNamespace))

                throw new InvalidOperationException("No managed description found for message action.");
            
            this.groupUri = groupUri;
            this.dispatchAction = dispatchAction;
            this.messageStyle = messageStyle;
        }

        public string GroupUri
        {
            get
            {
                return groupUri;
            }
        }

        public string DispatchAction
        {
            get
            {
                return dispatchAction;
            }
        }

        public string MessageStyle
        {
            get
            {
                return messageStyle;
            }
        }

        public Connection DispatchConnection
        {
            get
            {
                return dispatchConnection;
            }
        }

        public Interaction DispatchInteraction
        {
            get
            {
                return dispatchInteraction;
            }
        }

        public Message ValidateAndFormatInputMessage(Message requestMessage, out String accepts)
        {
            return requestMessage.ValidateAndFormatInputMessage(dispatchAction, messageStyle, dispatchConnection, dispatchInteraction, dispatchTargetNamespace, out accepts);
        }

        public Message ValidateAndFormatOuputMessage(Message replyMessage, String accepts)
        {
            return replyMessage.ValidateAndFormatOuputMessage(dispatchAction, messageStyle, dispatchConnection, dispatchInteraction, dispatchTargetNamespace, accepts);
        }
    }
}
