using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AllVerge.MessagingModel
{
    public class MessagingException : Exception
    {
        public MessagingException() { }
        public MessagingException(string message) : base(message) { }
        public MessagingException(string message, Exception innerException) : base(message, innerException) { }
        protected MessagingException(SerializationInfo info, StreamingContext context) : base(info, context) { throw new PlatformNotSupportedException(); }
    }

    public class ServerTooBusyException : MessagingException
    {
        public ServerTooBusyException() { }
        public ServerTooBusyException(string message) : base(message) { }
        public ServerTooBusyException(string message, Exception innerException) : base(message, innerException) { }
        protected ServerTooBusyException(SerializationInfo info, StreamingContext context) : base(info, context) { throw new PlatformNotSupportedException(); }
    }
}
