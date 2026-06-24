using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AllVerge.Core.Model;
using AllVerge.Core.Model.JsonSchema;
using AllVerge.Core.Model.JsonSchema.Adapters;
using AllVerge.Core.Model.LexicalTypes;
using AllVerge.Core.Model.SwaggerTypes;
using AllVerge.Core.ServiceModel.Description.Model;

namespace AllVerge.Core.ServiceModel.Description.Adapters
{
    public static class ReferenceManagerExtensions
    {
        public static void WriteKnownReferencesNodes<T>(this IReferenceManager<T> references, XmlDictionaryWriter writer) where T : IQualifiable
        {
            writer.WriteStartElement(references.ReferencesKind);

            writer.WriteAttributeString("type", "object");

            foreach (T referenceable in references.References)
            {
                if (referenceable is Potential)
                {
                    Potential potential = (Potential)(Object)referenceable;

                    potential.WritePotentialNode(WriteContext.Type, false, JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI, SwaggerDataTypes.VERSION_2_0_URI, null, writer);
                }
                else
                {
                    throw new NotImplementedException(referenceable.GetType().ToString());
                }
            }

            writer.WriteEndElement();
        }
    }
}
