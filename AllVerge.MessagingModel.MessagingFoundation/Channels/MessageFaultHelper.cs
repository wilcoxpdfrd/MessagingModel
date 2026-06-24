using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;


namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;

    public static class MessageFaultHelper
    {
        static readonly Type MessageFaultType = typeof(MessageFault);
        static readonly MethodInfo OnWriteStartDetailMethodInfo = MessageFaultType.GetMethod("OnWriteStartDetail", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo OnWriteDetailContentsMethodInfo = MessageFaultType.GetMethod("OnWriteDetailContents", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void WriteTo(this MessageFault fault, XmlWriter writer, EnvelopeVersion version)
        {
            fault.WriteTo(XmlDictionaryWriter.CreateDictionaryWriter(writer), version);
        }

        public static void WriteTo(this MessageFault fault, XmlDictionaryWriter writer, EnvelopeVersion version)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (version == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("version");
            }
            if (version == EnvelopeVersion.Soap12)
            {
                fault.WriteTo12(writer);
                return;
            }
            if (version == EnvelopeVersion.Soap11)
            {
                fault.WriteTo11(writer);
                return;
            }
            if (version == EnvelopeVersion.None)
            {
                fault.WriteToNone(writer);
                return;
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.Format(PublicSR.EnvelopeVersionUnknown, new object[]
            {
                version.ToString()
            })));
        }

        private static void WriteToNone(this MessageFault fault, XmlDictionaryWriter writer)
        {
            if (writer.GetType().FullName == "System.Runtime.Serialization.Json.XmlJsonWriter")
                fault.WriteTo12JsonDriver(writer);
            else
                fault.WriteTo12Driver(writer, EnvelopeVersion.None);
        }

        private static void WriteTo12JsonDriver(this MessageFault fault, XmlDictionaryWriter writer)
        {
            XmlDictionaryString versionXDSNamespace = null;

            writer.WriteStartElement("root", null);
            writer.WriteAttributeString("type", "object");

            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.MessageDictionary, "Fault"), versionXDSNamespace);
            writer.WriteAttributeString("type", "object");

            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultCode"), versionXDSNamespace);
            writer.WriteAttributeString("type", "object");

            fault.WriteFaultCode12JsonDriver(writer, fault.Code);
            
            writer.WriteEndElement();
            
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultReason"), versionXDSNamespace);
            writer.WriteAttributeString("type", "object");

            FaultReason reason = fault.Reason;
            FaultReasonText faultReasonText = reason.GetMatchingTranslation();
            
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultText"), versionXDSNamespace);
            writer.WriteAttributeString("type", "object");

            writer.WriteStartElement("item", "item");
            writer.WriteAttributeString("xmlns", "lang", "http://www.w3.org/2000/xmlns/", "http://www.w3.org/XML/1998/namespace");
            writer.WriteAttributeString("item", "lang:" + faultReasonText.XmlLang);
            writer.WriteString(faultReasonText.Text);
            
            writer.WriteEndElement();
            
            writer.WriteEndElement();
            
            if (fault.Node.Length > 0)
            {
                writer.WriteElementString(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultNode"), versionXDSNamespace, fault.Node);
            }
            if (fault.Actor.Length > 0)
            {
                writer.WriteElementString(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultRole"), versionXDSNamespace, fault.Actor);
            }
            if (fault.HasDetail)
            {
                fault.OnWriteJsonDetail(writer);
            }

            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        private static void WriteTo12Driver(this MessageFault fault, XmlDictionaryWriter writer, EnvelopeVersion version)
        {
            XmlDictionaryString versionXDSNamespace = version?.DictionaryNamespace;

            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.MessageDictionary, "Fault"), versionXDSNamespace);
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultCode"), versionXDSNamespace);
            fault.WriteFaultCode12Driver(writer, fault.Code, version);
            writer.WriteEndElement();
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultReason"), versionXDSNamespace);
            FaultReason reason = fault.Reason;
            FaultReasonText faultReasonText = reason.GetMatchingTranslation();
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultText"), versionXDSNamespace);
            writer.WriteAttributeString("xml", "lang", "http://www.w3.org/XML/1998/namespace", faultReasonText.XmlLang);
            writer.WriteString(faultReasonText.Text);
            writer.WriteEndElement();
            writer.WriteEndElement();
            if (fault.Node.Length > 0)
            {
                writer.WriteElementString(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultNode"), versionXDSNamespace, fault.Node);
            }
            if (fault.Actor.Length > 0)
            {
                writer.WriteElementString(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultRole"), versionXDSNamespace, fault.Actor);
            }
            if (fault.HasDetail)
            {
                fault.OnWriteDetail(writer, version);
            }
            writer.WriteEndElement();
        }

        private static void WriteFaultCode12JsonDriver(this MessageFault fault, XmlDictionaryWriter writer, FaultCode faultCode)
        {
            XmlDictionaryString versionXDSNamespace = null;

            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultValue"), versionXDSNamespace);
            writer.WriteAttributeString("type", "object");

            string localName;
            if (faultCode.IsSenderFault)
            {
                localName = "Sender";
            }
            else if (faultCode.IsReceiverFault)
            {
                localName = "Receiver";
            }
            else
            {
                localName = faultCode.Name;
            }
            string @namespace;
            if (faultCode.IsPredefinedFault)
            {
                @namespace = EnvelopeVersion.Soap12.Namespace;
            }
            else
            {
                @namespace = faultCode.Namespace;
            }
            if (@namespace != null && writer.LookupPrefix(@namespace) == null)
            {
                writer.WriteStartElement("item", "item");
                writer.WriteAttributeString("xmlns", "codens", "http://www.w3.org/2000/xmlns/", @namespace);
                writer.WriteAttributeString("item", "codens:FaultCode");
                writer.WriteString(localName);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            
            if (faultCode.SubCode != null)
            {
                writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultSubcode"), versionXDSNamespace);
                writer.WriteAttributeString("type", "object");

                fault.WriteFaultCode12JsonDriver(writer, faultCode.SubCode);
                
                writer.WriteEndElement();
            }
        }

        private static void WriteFaultCode12Driver(this MessageFault fault, XmlDictionaryWriter writer, FaultCode faultCode, EnvelopeVersion version)
        {
            XmlDictionaryString versionXDSNamespace = version?.DictionaryNamespace;

            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultValue"), versionXDSNamespace);
            string localName;
            if (faultCode.IsSenderFault)
            {
                localName = "Sender";
            }
            else if (faultCode.IsReceiverFault)
            {
                localName = "Receiver";
            }
            else
            {
                localName = faultCode.Name;
            }
            string @namespace;
            if (faultCode.IsPredefinedFault)
            {
                @namespace = version?.Namespace;
            }
            else
            {
                @namespace = faultCode.Namespace;
            }
            if (@namespace != null && writer.LookupPrefix(@namespace) == null)
            {
                writer.WriteAttributeString("xmlns", "a", "http://www.w3.org/2000/xmlns/", @namespace);
            }
            writer.WriteQualifiedName(localName, @namespace);
            writer.WriteEndElement();
            if (faultCode.SubCode != null)
            {
                writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultSubcode"), versionXDSNamespace);
                fault.WriteFaultCode12Driver(writer, faultCode.SubCode, version);
                writer.WriteEndElement();
            }
        }

        private static void WriteTo12(this MessageFault fault, XmlDictionaryWriter writer)
        {
            fault.WriteTo12Driver(writer, EnvelopeVersion.Soap12);
        }

        private static void WriteTo11(this MessageFault fault, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.MessageDictionary, "Fault"), PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "Namespace"));
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "FaultCode"), PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "FaultNamespace"));
            FaultCode faultCode = fault.Code;
            if (faultCode.SubCode != null)
            {
                faultCode = faultCode.SubCode;
            }
            string localName;
            if (faultCode.IsSenderFault)
            {
                localName = "Client";
            }
            else if (faultCode.IsReceiverFault)
            {
                localName = "Server";
            }
            else
            {
                localName = faultCode.Name;
            }
            string text;
            if (faultCode.IsPredefinedFault)
            {
                text = "http://schemas.xmlsoap.org/soap/envelope/";
            }
            else
            {
                text = faultCode.Namespace;
            }
            if (writer.LookupPrefix(text) == null)
            {
                writer.WriteAttributeString("xmlns", "a", "http://www.w3.org/2000/xmlns/", text);
            }
            writer.WriteQualifiedName(localName, text);
            writer.WriteEndElement();
            FaultReasonText faultReasonText = fault.Reason.GetMatchingTranslation();
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "FaultString"), PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "FaultNamespace"));
            if (faultReasonText.XmlLang.Length > 0)
            {
                writer.WriteAttributeString("xml", "lang", "http://www.w3.org/XML/1998/namespace", faultReasonText.XmlLang);
            }
            writer.WriteString(faultReasonText.Text);
            writer.WriteEndElement();
            if (fault.Actor.Length > 0)
            {
                writer.WriteElementString(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "FaultActor"), PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message11Dictionary, "FaultNamespace"), fault.Actor);
            }
            if (fault.HasDetail)
            {
                fault.OnWriteDetail(writer, EnvelopeVersion.Soap11);
            }
            writer.WriteEndElement();
        }

        private static void OnWriteJsonDetail(this MessageFault fault, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(PublicXD.GetDictionaryString(PublicXD.Dictionaries.Message12Dictionary, "FaultDetail"), null);
            writer.WriteAttributeString("type", "object");
            OnWriteDetailContentsMethodInfo.Invoke(fault, new Object[] { writer });
            writer.WriteEndElement();
        }

        private static void OnWriteDetail(this MessageFault fault, XmlDictionaryWriter writer, EnvelopeVersion version)
        {
            OnWriteStartDetailMethodInfo.Invoke(fault, new object[] { writer, version });
            OnWriteDetailContentsMethodInfo.Invoke(fault, new Object[] { writer });
            writer.WriteEndElement();
        }

        public static MessageFault CreateFault(XmlReader reader, int maxBufferSize)
        {
            if (reader is XmlDictionaryReader)
        
                return CreateFault(reader as XmlDictionaryReader, maxBufferSize);
            
            return CreateFault(XmlDictionaryReader.CreateDictionaryReader(reader), maxBufferSize);
        }

        public static MessageFault CreateFault(XmlDictionaryReader reader, int maxBufferSize)
        {
            MessageFault fault;
            if (reader.NamespaceURI == EnvelopeVersion.Soap12.Namespace)
            {
                fault = ReceivedFault.CreateFault12(reader, maxBufferSize);
            }
            else if (reader.NamespaceURI == EnvelopeVersion.Soap11.Namespace)
            {
                fault = ReceivedFault.CreateFault11(reader, maxBufferSize);
            }
            else if (reader.NamespaceURI == EnvelopeVersion.None.Namespace)
            {
                fault = ReceivedFault.CreateFaultNone(reader, maxBufferSize);
            }
            else
            {
                throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.Format(PublicSR.EnvelopeVersionUnknown, reader.NamespaceURI)), null);
            }
            return fault;
        }

        /// <summary>Returns a new <see cref="MessageFault" /> object that uses the specified <see cref="FaultCode" /> and fault reason.</summary>
        /// <returns>A new <see cref="MessageFault" />.</returns>
        /// <param name="code">The fault code for the fault message.</param>
        /// <param name="reason">The reason for the fault.</param>
        public static MessageFault CreateFault(FaultCode code, string reason)
        {
            return MessageFaultHelper.CreateFault(code, new FaultReason(reason));
        }

        /// <summary>Returns a new <see cref="MessageFault" /> object that uses the specified <see cref="FaultCode" /> and <see cref="FaultReason" /> objects.</summary>
        /// <returns>A new <see cref="MessageFault" /> object.</returns>
        /// <param name="code">The fault code for the fault message.</param>
        /// <param name="reason">The reason for the fault.</param>
        public static MessageFault CreateFault(FaultCode code, FaultReason reason)
        {
            return MessageFaultHelper.CreateFault(code, reason, null, null, "", "");
        }

        /// <summary>Returns a new <see cref="MessageFault" /> object that uses the specified <see cref="FaultCode" />, <see cref="FaultReason" />, and detail object.</summary>
        /// <returns>A new <see cref="MessageFault" /> object.</returns>
        /// <param name="code">The fault code for the fault message.</param>
        /// <param name="reason">The reason for the fault.</param>
        /// <param name="detail">The fault detail object.</param>
        public static MessageFault CreateFault(FaultCode code, FaultReason reason, object detail)
        {
            return MessageFaultHelper.CreateFault(code, reason, detail, new DataContractSerializer(GetDetailType(detail)), "", "");
        }

        public static MessageFault CreateFault(this Exception e, RootFaultCode faultCode, MessageVersion messageVersion, bool useInnerException = false)
        {
            if (e.InnerException != null && useInnerException)

                e = e.InnerException;

            return CreateFault(
                faultCode.WrapFaultCode(messageVersion.Envelope),
                new FaultReason(e.Message),
                new ExceptionDetail(e));
        }

        public static MessageFault CreateFault(this RootFaultCode faultCode, MessageVersion messageVersion)
        {
            return CreateFault(
                faultCode.WrapFaultCode(messageVersion.Envelope),
                faultCode.Reason);
        }

        /// <summary>
        /// <summary>Returns a new <see cref="MessageFault" /> object with a "receiver" <see cref="FaultCode" />, populated with details from <paramref name="e"/>.</summary>
        /// </summary>
        /// <param name="e">The original exception.</param>
        /// <param name="subCode">The sub-code of the receiver code.</param>
        /// <param name="messageVersion">The version of the fault.</param>
        /// <param name="useInnerException">Use the inner exception to populate the fault details.</param>
        /// <returns></returns>
        public static MessageFault CreateReceiverFault(this Exception e, FaultCode subCode, MessageVersion messageVersion, bool useInnerException = false)
        {
            if (useInnerException && e.InnerException != null)
            
                e = e.InnerException;
            
            return CreateFault(
                messageVersion.Envelope.CreateReceiverFaultCode(subCode),
                new FaultReason(e.Message), 
                new ExceptionDetail(e));
        }

        /// <summary>
        /// <summary>Returns a new <see cref="MessageFault" /> object with a "sender" <see cref="FaultCode" />, populated with details from <paramref name="e"/>.</summary>
        /// </summary>
        /// <param name="e">The original exception.</param>
        /// <param name="subCode">The sub-code of the receiver code.</param>
        /// <param name="messageVersion">The version of the fault.</param>
        /// <param name="useInnerException">Use the inner exception to populate the fault details.</param>
        /// <returns></returns>
        public static MessageFault CreateSenderFault(this Exception e, FaultCode subCode, MessageVersion messageVersion, bool useInnerException = false)
        {
            if (useInnerException && e.InnerException != null)
                
                e = e.InnerException;
            
            return CreateFault(
                messageVersion.Envelope.CreateSenderFaultCode(subCode),
                new FaultReason(e.Message), 
                new ExceptionDetail(e));
        }


        public static Message CreateFaultMessage(this Exception e, RootFaultCode faultCode, MessageVersion messageVersion, string faultAction = null, bool useInnerException = false)
        {
            if (e.InnerException != null && useInnerException)

                e = e.InnerException;

            return CreateMessage(
                messageVersion,
                CreateFault(
                    faultCode.WrapFaultCode(messageVersion.Envelope),
                    new FaultReason(e.Message),
                    new ExceptionDetail(e)), 
                faultAction ?? messageVersion.Addressing.DefaultFaultAction);
        }

        public static Message CreateFaultMessage(this RootFaultCode faultCode, MessageVersion messageVersion, string faultAction = null)
        {
            if (messageVersion == null)

                messageVersion = MessageVersion.Default;

            return CreateMessage(
                messageVersion,
                CreateFault(
                    faultCode.WrapFaultCode(messageVersion.Envelope),
                    faultCode.Reason), 
                faultAction ?? messageVersion.Addressing.DefaultFaultAction);
        }


        /// <summary>Creates a message that contains a SOAP fault, a version and an action.</summary>
        /// <returns>A <see cref="Message" /> object for the message created. </returns>
        /// <param name="version">A <see cref="MessageVersion" /> object that specifies the SOAP version to use for the message.</param>
        /// <param name="fault">A <see cref="MessageFault" /> object that represents a SOAP fault. </param>
        /// <param name="action">A description of how the message should be processed. </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="Version" />, <paramref name="fault" /> or <paramref name="action" /> is null. </exception>
        public static Message CreateMessage(MessageVersion version, MessageFault fault, string action)
        {
            if (fault == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(fault)));
            }

            if (version == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(version)));
            }

            return Message.CreateMessage(version, action, new FaultBodyWriter(fault, version.Envelope));
        }

        /// <summary>Returns a new <see cref="MessageFault" /> object that uses the specified <see cref="FaultCode" />, <see cref="FaultReason" />, and detail object.</summary>
        /// <returns>A new <see cref="MessageFault" /> object.</returns>
        /// <param name="code">The fault code for the fault message.</param>
        /// <param name="reason">The reason for the fault.</param>
        /// <param name="detail">The fault detail object.</param>
        public static MessageFault CreateFault(FaultCode code, FaultReason reason, ExceptionDetail detail)
        {
            DataContractSerializerSettings settings = new DataContractSerializerSettings()
            {
                IgnoreExtensionDataObject = false,
                PreserveObjectReferences = false,
                MaxItemsInObjectGraph = int.MaxValue
            };

            return MessageFaultHelper.CreateFault(code, reason, detail, new DataContractSerializer(GetDetailType(detail), settings), "", "");
        }

        private static MessageFault CreateFault(FaultCode code, FaultReason reason, object detail, XmlObjectSerializer serializer, string actor, string node)
        {
            if (code == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(code));
            }
            if (reason == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(reason));
            }
            if (actor == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(actor));
            }
            if (node == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(node));
            }
            return new DispatchObjectSerializerFault(code, reason, detail, serializer, actor, node);
        }

        private static Type GetDetailType(object detail)
        {
            return (detail == null) ? typeof(object) : detail.GetType();
        }
    }
}
