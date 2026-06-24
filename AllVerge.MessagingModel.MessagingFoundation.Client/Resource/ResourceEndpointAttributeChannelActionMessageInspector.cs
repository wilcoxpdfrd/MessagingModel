using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Text.RegularExpressions;

namespace AllVerge.MessagingModel.MessagingFoundation.Client.Resource
{
    class ResourceEndpointAttributeChannelActionMessageInspector : IClientMessageInspector
    {
        private Dictionary<string, string> map;

        public ResourceEndpointAttributeChannelActionMessageInspector(Dictionary<string, string> map)
        {
            this.map = map;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            if (this.map.TryGetValue(request.Headers.Action, out String method))
            {
                request.Headers.Action += '_'+ method;
            }

            return null;
        }

        internal static bool TrySplitOutChannelActionMethod(String _action, out String action, out String method)
        {
            if (_action != null)
            {
                String[] s = _action.Split('_');

                if (s.Length > 1)
                {
                    action = String.Join("_", s, 0, s.Length - 1);
                    method = s[s.Length - 1];

                    return true;
                }
            }

            action = null;
            method = null;

            return false;
        }
    }
}
