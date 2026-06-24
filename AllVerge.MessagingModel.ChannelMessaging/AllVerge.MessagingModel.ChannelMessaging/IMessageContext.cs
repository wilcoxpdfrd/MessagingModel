using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using AllVerge.MessagingModel.MessagingApplication;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public interface IMessageContext : IDisposable, ICloneable
    {
        IDictionary<object, object> Items { get; set; }
    }
}
