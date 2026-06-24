using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [XmlInclude(typeof(invalidates_t))]
    [XmlInclude(typeof(ttl_t))]
    public partial class if_t
    {
    }
}
