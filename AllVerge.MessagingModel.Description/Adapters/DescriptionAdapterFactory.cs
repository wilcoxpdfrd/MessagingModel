using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    public class DescriptionAdapterFactory
    {
        private static Dictionary<DocumentType, Func<String, IDescriptionReader>> readerFactories = new Dictionary<DocumentType, Func<string, IDescriptionReader>>();
        private static Dictionary<DocumentType, Func<String, IDescriptionWriter>> writerFactories = new Dictionary<DocumentType, Func<string, IDescriptionWriter>>();

        public static void TryRegister(DocumentType documentType, Func<String, IDescriptionReader> readerFactory, Func<String, IDescriptionWriter> writerFactory)
        {
            if (!readerFactories.ContainsKey(documentType))

                readerFactories.Add(documentType, readerFactory);

            if (!writerFactories.ContainsKey(documentType))

                writerFactories.Add(documentType, writerFactory);
        }

        public static IDescriptionReader GetDescriptionReader(DocumentType documentType)
        {
            if (readerFactories.ContainsKey(documentType))

                return readerFactories[documentType].Invoke(DescriptionConstants.DESCRIPTIONS_IMPORTS_PATH);

            else

                throw new NotImplementedException($"{documentType.ToString()} not implemented.");
        }

        public static IDescriptionWriter GetDescriptionWriter(DocumentType documentType, String descriptionExportsPath)
        {
            if (writerFactories.ContainsKey(documentType))

                return writerFactories[documentType].Invoke(descriptionExportsPath);

            else

                throw new NotImplementedException(documentType.ToString());
        }
    }
}
