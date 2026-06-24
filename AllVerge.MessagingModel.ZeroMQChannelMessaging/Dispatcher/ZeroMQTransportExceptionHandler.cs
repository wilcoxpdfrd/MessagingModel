using System;
using System.Collections.Generic;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Dispatcher
{
    class ZeroMQTransportExceptionHandler : ExceptionHandler
    {
        public override bool HandleException(Exception exception)
        {
            if (exception is TaskCanceledException)
                return false;
            return true;
        }
    }
}
