using AllVerge.Core.ServiceModel.Description.Adapters;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.Core.ServiceModel.Description.Wadl.v200902
{
    public static class Wadl200902
    {
        public static void TryRegisterAdapters()
        {
            DescriptionAdapterFactory.TryRegister(DocumentType.WADL200902, p => new Wadl200902Reader(p), s => new Wadl200902Writer(s));
        }
    }
}
