using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public static class MarkupExtensions
    {
        private static Dictionary<String, MarkupDocumentCollection> dataTreesCache = new Dictionary<string, MarkupDocumentCollection>();

        public static Dictionary<string, MarkupDocumentCollection> DataTreesCache
        {
            get
            {
                return dataTreesCache;
            }
        }

        public static bool IsDataTreeCachedIn(this Uri dataTreeLocator, String targetNamespace)
        {
            if (dataTreesCache.ContainsKey(targetNamespace))
            {
                MarkupDocumentCollection dataTrees = dataTreesCache[targetNamespace];

                return dataTrees.Contains(dataTreeLocator);
            }

            return false;
        }

        public static void PutDataTreesCache(this MarkupNode dataTreeRootNode, Uri dataTreeRootLocator, String targetNamespace)
        {
            new MarkupDocument(dataTreeRootLocator, dataTreeRootNode).PutDataTreesCache(targetNamespace);
        }

        private static void PutDataTreesCache(this MarkupDocument dataTree, String targetNamespace)
        {
            MarkupDocumentCollection dataTrees;

            if (!dataTreesCache.ContainsKey(targetNamespace))

                dataTreesCache.Add(targetNamespace, new MarkupDocumentCollection());

            dataTrees = dataTreesCache[targetNamespace];

            if (dataTrees.Contains(dataTree.Locator))

                dataTrees[dataTrees.IndexOf(dataTrees[dataTree.Locator])] = dataTree;

            else

                dataTrees.Add(dataTree);
        }
    }
}
