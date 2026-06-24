using AllVerge.Core.ServiceModel.Description.Adapters;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.Core.ServiceModel.Description.Swagger
{
    public static class Swagger20
    {
        public static void TryRegisterAdapters()
        {
            DescriptionAdapterFactory.TryRegister(DocumentType.SWAGGER20, p => new Swagger20Reader(p), s => new Swagger20Writer(s));
        }
    }
}
