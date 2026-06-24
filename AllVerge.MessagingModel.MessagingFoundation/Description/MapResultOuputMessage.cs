using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{

    public class MapResultOuputMessage : IExtension<InstanceContext>
    {
        private Func<object, EndpointAddress, UniqueId, Message> mapResult;

        public MapResultOuputMessage(Func<object, EndpointAddress, UniqueId, Message> mapResult)
        {
            this.mapResult = mapResult;
        }

        public Func<object, EndpointAddress, UniqueId, Message> Map => mapResult;

        public void Attach(InstanceContext owner)
        {
        }

        public void Detach(InstanceContext owner)
        {
        }
    }
}
