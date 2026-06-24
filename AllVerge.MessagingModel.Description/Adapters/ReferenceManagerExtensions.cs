using AllVerge.DataModel.Primitives;
using AllVerge.DataModel.Primitives.LexicalTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.Description.Adapters
{
    public static class ReferenceManagerExtensions
    {
        public static bool TryGetKnownReferencePath<T>(this IReferenceManager<T> references, T qualifiable, out String referencePath) where T : IQualifiable
        {
            String referenceKey;

            if (qualifiable is Potential)
            {
                Potential potential = (Potential)(Object)qualifiable;

                if (potential.LexicalType.IsQualified)
                {
                    referenceKey = potential.LexicalType.QualifiedName.LocalName;

                    referencePath = String.Format("#/{0}/{1}", references.ReferencesKind, referenceKey);

                    if (!references.ContainsKey(referenceKey))

                        references.Add(referenceKey, qualifiable);

                    return true;
                }
                else
                {
                    throw new NotImplementedException(qualifiable.GetType().ToString());
                }
            }

            // ToDo:  what about InteractionMessage?

            referencePath = null;

            return false;
        }
    }
}
