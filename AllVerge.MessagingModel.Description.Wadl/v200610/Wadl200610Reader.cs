using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;

using AllVerge.Core.ServiceModel.Description.Model;
using AllVerge.Core.ServiceModel.Description.Adapters;

using research.sun.com.wadl._2006._10;

namespace AllVerge.Core.ServiceModel.Description.Wadl.v200610
{
    public class Wadl200610Reader : IDescriptionReader
    {
        private Uri descriptionImportsCachePathUri;

        public Wadl200610Reader(String descriptionImportsCachePath)
        {
            this.descriptionImportsCachePathUri = new Uri(descriptionImportsCachePath);
        }

        public ProtocolDescription ReadDescription(string descriptionLocator, bool canReadFromCache = true)
        {
            throw new NotImplementedException();
        }
    }
}
