using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IMessageContent
    {
    }

    public interface IMessageContent<MessageContent> : IMessageContent where MessageContent : class, IMessageContent
    {
        MessageContent Content { get; }
    }
}
