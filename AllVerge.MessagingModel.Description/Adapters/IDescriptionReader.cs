using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    using AllVerge.MessagingModel.Description.Model;

    public interface IDescriptionReader
    {
        // ToDo: Rewrite all ReadDescription implementations: when canReadFromCache then 
        // interpolate descriptionLocaator as cache path url and use that url if in cache ... 
        // Otherwise download and save to cache after downloading ...
        // Already implemented in Raml10Reader ... follow that.
        ProtocolDescription ReadDescription(String descriptionLocator, bool canReadFromCache = true);
    }
}
