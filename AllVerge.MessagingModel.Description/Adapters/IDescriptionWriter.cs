using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    using AllVerge.MessagingModel.Description.Model;

    public interface IDescriptionWriter
    {
        void WriteDescription(ProtocolDescription description, String connectorNameOrIndex, String connectionNameOrIndex, String behaviorNameOrIndex, String hostName);
    }
}
