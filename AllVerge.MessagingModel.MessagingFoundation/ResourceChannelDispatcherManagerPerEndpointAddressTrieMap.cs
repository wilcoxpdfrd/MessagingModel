using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.SystemPrimitives.Net;

    public class ResourceChannelDispatcherManagerPerEndpointAddressTrieMap : 
        UriTrieKeyedDictionary<ResourceChannelDispatcherManager>
    {
        public ResourceChannelDispatcherManagerPerEndpointAddressTrieMap() : base() { }
    }
}
