using AllVerge.Core.ServiceModel.Description.Adapters;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.Core.ServiceModel.Description.Wsdl
{
    public static class Wsdl11
    {
        public static void TryRegisterAdapters()
        {
            DescriptionAdapterFactory.TryRegister(DocumentType.WSDL11, p => new Wsdl11Reader(p), s => new Wsdl11Writer(s));
        }
    }
}
