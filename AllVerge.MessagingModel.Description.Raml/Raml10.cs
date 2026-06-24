using System;
using System.Collections.Generic;
using System.Text;

using AllVerge.Core.ServiceModel.Description.Adapters;

namespace AllVerge.Core.ServiceModel.Description.Raml
{
    public class Raml10
    {
        public static void TryRegisterAdapters()
        {
            DescriptionAdapterFactory.TryRegister(DocumentType.RAML10, p => new Raml10Reader(p), s => new Raml10Writer(s));
        }
    }
}
