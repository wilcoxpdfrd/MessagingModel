using System;
using System.Runtime.Serialization;

namespace AllVerge.MessagingModel.MessagingApplication
{
    [Serializable]
    public class DependencyNotFoundException : InvalidOperationException
    {
        public DependencyNotFoundException()
        {
        }

        public DependencyNotFoundException(string message) : base(message)
        {
        }

        public DependencyNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DependencyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}