using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.DataModel.Primitives.LexicalTypes;
    using AllVerge.MessagingModel.Description.Model;

    public class ReferenceManagers
    {
        public const string REFERENCES_KIND_DEFINITIONS = "definitions";
        public const string REFERENCES_KIND_PARAMETERS = "parameters";
        public const string REFERENCES_KIND_RESPONSES = "responses";

        public IReferenceManager<Potential> Definitions = new ReferenceManager<Potential>(ReferenceManagers.REFERENCES_KIND_DEFINITIONS);
        public IReferenceManager<InteractionMessage> Parameters = new ReferenceManager<InteractionMessage>(ReferenceManagers.REFERENCES_KIND_PARAMETERS);
        public IReferenceManager<InteractionMessage> Responses = new ReferenceManager<InteractionMessage>(ReferenceManagers.REFERENCES_KIND_RESPONSES);

        protected class ReferenceManager<T> : IReferenceManager<T> where T : IQualifiable
        {
            private Dictionary<String, T> references = new Dictionary<String, T>();
            private string referencesKind;

            public ReferenceManager(String referencesKind)
            {
                switch (referencesKind)
                {
                    case REFERENCES_KIND_DEFINITIONS:
                    case REFERENCES_KIND_PARAMETERS:
                    case REFERENCES_KIND_RESPONSES:

                        this.referencesKind = referencesKind;

                        break;

                    default:

                        throw new ArgumentException(nameof(referencesKind), "Invalid parameter value.");
                }
            }

            public string ReferencesKind
            {
                get
                {
                    return referencesKind;
                }
            }

            public IEnumerable<T> References
            {
                get
                {
                    return this.references.Values;
                }
            }

            public bool ContainsKey(string referenceKey)
            {
                return this.references.ContainsKey(referenceKey);
            }

            public void Add(string referenceKey, T referanceable)
            {
                this.references.Add(referenceKey, referanceable);
            }

            public bool TryGetReferencePath(T referenceable, out string referencePath)
            {
                return this.TryGetKnownReferencePath(referenceable, out referencePath);
            }
        }
    }
}
