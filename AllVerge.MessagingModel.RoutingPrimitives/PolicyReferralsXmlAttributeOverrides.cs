using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.RoutingPrimitives
{
    using www.w3.org.ns.ws_policy;

    using schemas.xmlsoap.org.ws._2001._10.referral;
    
    using AllVerge.PolicyPrimitives;

    public class PolicyReferralsXmlAttributeOverrides : PolicyXmlAttributeOverrides
    {
        public PolicyReferralsXmlAttributeOverrides() : base()
        {
            this.TryAddItemsXmlElementAttribute(typeof(OperatorContentType), "Items", typeof(registration_t), ItemsChoiceType.Item.ToString());
        }
    }
}
