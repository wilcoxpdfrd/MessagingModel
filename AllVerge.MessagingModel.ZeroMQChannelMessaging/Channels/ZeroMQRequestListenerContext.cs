using System;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQRequestListenerContext
    {
        public ZeroMQRequestListenerContext(ZeroMQRequestListener requestListener, ZeroMQListenerRequest request)
        {
            this.Listener = requestListener;
            this.Request = request.SetContext(this);
            this.Response = new ZeroMQListenerResponse(this);
        }

        public ZeroMQRequestListener Listener { get; }

        public ZeroMQListenerRequest Request { get; }
        
        public ZeroMQListenerResponse Response { get; }

        internal void Abort()
        {
            this.Response.Abort();

            this.Request.Close();
        }

        internal void Close()
        {
            try
            {
                this.Response.Close();
            }
            finally
            {
                this.Request.Close();
            }
        }
    }
}