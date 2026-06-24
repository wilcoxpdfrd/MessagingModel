using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [XmlInclude(typeof(uri_t))]
    public partial class invalidates_t
    {
        public invalidates_t() { }

        public invalidates_t(params uri_t[] rid) 
        {
            this.rid = rid;
        }
    }
}
