using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.RoutingPrimitives.Referrals
{
    using schemas.xmlsoap.org.ws._2001._10.referral;
    public class Referral
    {
        public uri_t GroupUri;
        public uri_t Action;
        public bool PrefixMatch;
    }
}
