using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description
{
    public struct DescriptionConstants
    {
        public const string Namespace = "http://description.connect.allverge.com/";
        public const string ProtocolServiceName = "ProtocolDescriptionService";
        public const string DescriptionServiceName = "DescriptionService";

        public const string TEMP_URI = "http://tempuri.org";

        public static readonly string DESCRIPTIONS_CACHE_PATH = $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}{Path.DirectorySeparatorChar}AllVerge{Path.DirectorySeparatorChar}Descriptions{Path.DirectorySeparatorChar}";
        public static readonly string DESCRIPTIONS_SHARED_CACHE_PATH = $"{DESCRIPTIONS_CACHE_PATH}urn{Path.DirectorySeparatorChar}shared";
        public static readonly string DESCRIPTIONS_IMPORTS_PATH = $"{DESCRIPTIONS_CACHE_PATH}Imports{Path.DirectorySeparatorChar}";
        public static readonly string DESCRIPTIONS_EXPORTS_RELATIVE_PATH = $"Exports{Path.DirectorySeparatorChar}";
    }
}
