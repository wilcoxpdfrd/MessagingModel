using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Markup.Xml.Schema
{
    using AllVerge.SystemPrimitives.Net;

    public class SchemaImport
    {
        private string @namespace;
        private string location;
        private string baseLocation;
        private Uri importsCachePathUri;

        public SchemaImport(string @namespace, string location, string baseLocation, Uri importsCachePathUri = null)
        {
            this.@namespace = @namespace;
            this.location = location;
            this.baseLocation = baseLocation;
            this.importsCachePathUri = importsCachePathUri;
        }

        public string Namespace
        {
            get
            {
                return @namespace;
            }
        }

        public string Location
        {
            get
            {
                return location;
            }
        }

        public string BaseLocation
        {
            get
            {
                return baseLocation;
            }
        }

        public bool HasLocation
        {
            get
            {
                return this.location != null;
            }
        }

        public Uri AbsoluteUri
        {
            get
            {
                Uri absoluteUri;

                if (UriUtils.TryCreateAbsoluteUri(this.location, this.baseLocation, out absoluteUri))

                    return absoluteUri;

                throw new UriFormatException("Cannot resolve path to imported schema.");
            }
        }

        public Uri ImportsCachePathUri
        {
            get
            {
                return importsCachePathUri;
            }
        }
    }
}
