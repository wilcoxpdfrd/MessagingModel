using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class WildcardTemplateFilter : UriTemplateFilter
    {
        public WildcardTemplateFilter(Uri baseAddress, String method, string name) : 
            base(new UriTemplate(UriTemplate.WildcardPath), baseAddress, method, name)
        {
        }
    }
}
