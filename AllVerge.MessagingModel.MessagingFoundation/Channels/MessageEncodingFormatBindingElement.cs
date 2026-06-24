using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
	using global::System.ServiceModel;
	using global::System.ServiceModel.Channels;

	public sealed class MessageEncodingFormatBindingElement : MessageEncodingBindingElement
	{
        private class ResourceContentTypeMapper : IMessagingContentTypeMapper
        {
            public string GetContentTypeForTransferMessageEncoding(MessageEncodingFormat transferFormat, out MessageVersion messageVersion)
            {
				return transferFormat.CreateMessageContentType(out messageVersion).ToMediaTypePlusParameters();
            }

            public MessageEncodingFormat GetTransferMessageEncodingForContentType(string contentType, AddressingVersion addressingVersion = null)
            {
                MediaContentType mediaContentType;
				if (addressingVersion == AddressingVersion.None)
                    mediaContentType = new MediaContentType(contentType);
                else
                    mediaContentType = 
						new MediaContentType(
							contentType, 
							new (string, string, bool)[]
							{
								(MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false)
							}
						);

				return mediaContentType.TransferFormat;
            }
        }

        private int maxReadPoolSize;
		private int maxWritePoolSize;
		private XmlDictionaryReaderQuotas readerQuotas;
		private Encoding writeEncoding;
		private MessageVersion messageVersion;
		private MessageEncodingFormat messageFormat;
        private bool javascriptCallbackEnabled;
        private int maxSessionSize;
        private long maxReceivedMessageSize;
		private IMessagingContentTypeMapper contentTypeMapper;

		public MessageEncodingFormatBindingElement()
			: this(TextEncoderDefaults.Encoding)
		{
		}

		public MessageEncodingFormatBindingElement(Encoding writeEncoding)
		{
			if (writeEncoding == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writeEncoding");
			}
			TextEncoderDefaults.ValidateEncoding(writeEncoding);
			this.writeEncoding = writeEncoding;
			messageVersion = MessageVersion.Default;
			messageFormat = MessageEncodingFormat.Default;
			maxReadPoolSize = EncoderDefaults.MaxReadPoolSize;
			maxWritePoolSize = EncoderDefaults.MaxWritePoolSize;
			readerQuotas = new XmlDictionaryReaderQuotas();
			EncoderDefaults.ReaderQuotas.CopyTo(readerQuotas);
			maxSessionSize = BinaryEncoderDefaults.MaxSessionSize;
			maxReceivedMessageSize = TransportDefaults.MaxReceivedMessageSize;
			javascriptCallbackEnabled = false;
            contentTypeMapper = new ResourceContentTypeMapper();
        }

        private MessageEncodingFormatBindingElement(MessageEncodingFormatBindingElement elementToBeCloned)
			: base(elementToBeCloned)
		{
			writeEncoding = elementToBeCloned.writeEncoding;
			messageVersion = elementToBeCloned.MessageVersion;
			messageFormat = elementToBeCloned.Format;
			maxReadPoolSize = elementToBeCloned.maxReadPoolSize;
			maxWritePoolSize = elementToBeCloned.maxWritePoolSize;
			readerQuotas = new XmlDictionaryReaderQuotas();
			elementToBeCloned.readerQuotas.CopyTo(readerQuotas);
			maxSessionSize = elementToBeCloned.MaxSessionSize;
			maxReceivedMessageSize = elementToBeCloned.MaxReceivedMessageSize;
			javascriptCallbackEnabled = elementToBeCloned.JavascriptCallbackEnabled;
			contentTypeMapper = elementToBeCloned.contentTypeMapper;
		}

		public int MaxReadPoolSize
		{
			get
			{
				return maxReadPoolSize;
			}
			set
			{
				if (value <= 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", value, PublicSR.Format(PublicSR.ValueMustBePositive)));
				}
				maxReadPoolSize = value;
			}
		}

		public int MaxWritePoolSize
		{
			get
			{
				return maxWritePoolSize;
			}
			set
			{
				if (value <= 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", value, PublicSR.Format(PublicSR.ValueMustBePositive)));
				}
				maxWritePoolSize = value;
			}
		}

		public Encoding WriteEncoding
		{
			get
			{
				return writeEncoding;
			}
			set
			{
				if (value == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
				}
				TextEncoderDefaults.ValidateEncoding(value);
				writeEncoding = value;
			}
		}

		public override MessageVersion MessageVersion
		{
			get
			{
				return this.messageVersion;
			}
			set
			{
				this.messageVersion = value;
			}
		}

		public MessageEncodingFormat Format
		{
			get
			{
				return messageFormat;
			}
			set
			{
				messageFormat = value;
			}
		}

		public XmlDictionaryReaderQuotas ReaderQuotas => readerQuotas;

		public int MaxSessionSize
		{
			get
			{
				return maxSessionSize;
			}
			set
			{
				maxSessionSize = value;
			}
		}

		public long MaxReceivedMessageSize
		{
			get
			{
				return maxReceivedMessageSize;
			}
			set
			{
				maxReceivedMessageSize = value;
			}
		}

		public bool JavascriptCallbackEnabled
		{
			get
			{
				return javascriptCallbackEnabled;
			}
			set
			{
				javascriptCallbackEnabled = value;
			}
		}


		public IMessagingContentTypeMapper ContentTypeMapper
		{
			get
			{
				return contentTypeMapper;
			}
			set
			{
				contentTypeMapper = value;
			}
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("context"));
			}

			context.BindingParameters.Add(this);

			return base.CanBuildChannelFactory<TChannel>(context);
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("context"));
			}

			context.BindingParameters.Add(this);

			return base.BuildChannelFactory<TChannel>(context);
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		{
			return InternalBuildChannelListener<TChannel>(context);
		}

		public override bool CanBuildChannelListener<TChannel>(BindingContext context)
		{
			return InternalCanBuildChannelListener<TChannel>(context);
		}

		public override BindingElement Clone()
		{
			return new MessageEncodingFormatBindingElement(this);
		}

		public override MessageEncoderFactory CreateMessageEncoderFactory()
		{
			return new CompositeMessageEncodingFormatEncoderFactory(
				this.WriteEncoding,
				this.MaxReadPoolSize,
				this.MaxWritePoolSize,
				new XmlDictionaryReaderQuotas(),
				this.MaxSessionSize,
				this.MaxReceivedMessageSize,
				this.Format,
				this.JavascriptCallbackEnabled,
				this.ContentTypeMapper);
		}

		public override T GetProperty<T>(BindingContext context)
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (typeof(T) == typeof(XmlDictionaryReaderQuotas))
			{
				return (T)(object)readerQuotas;
			}
			return base.GetProperty<T>(context);
		}
	}
}
