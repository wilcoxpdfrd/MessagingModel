using System;
using System.Collections.Generic;
using System.Linq;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class UriTemplateTables<Data> : Dictionary<String, UriTemplateTable>
    {
        private Uri serviceAddress;
        private bool addTrailingSlashToBaseAddress;

        public UriTemplateTables(Uri serviceAddress, bool addTrailingSlashToBaseAddress)
        {
            this.serviceAddress = serviceAddress;
            this.addTrailingSlashToBaseAddress = addTrailingSlashToBaseAddress;
        }

        public bool HasItems => this.Values.Any(t => t.KeyValuePairs.Count > 0);

        public void Add(String method, UriTemplate uriTemplate, Data data)
        {
            if (!this.ContainsKey(method))

                this.Add(method, new UriTemplateTable(this.serviceAddress, this.addTrailingSlashToBaseAddress));

            this[method].KeyValuePairs.Add(new KeyValuePair<UriTemplate, Object>(uriTemplate, data));
        }

        public void MakeReadOnly(bool allowDuplicateEquivalentUriTemplates)
        {
            foreach (UriTemplateTable uriTemplateTable in this.Values)

                uriTemplateTable.MakeReadOnly(allowDuplicateEquivalentUriTemplates);
        }
    }
}
