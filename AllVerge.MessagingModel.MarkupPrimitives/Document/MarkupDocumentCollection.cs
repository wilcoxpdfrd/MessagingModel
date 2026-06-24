using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public class MarkupDocumentCollection : KeyedCollection<Uri, MarkupDocument>
    {
        public static readonly MarkupDocumentCollection Empty = new MarkupDocumentCollection(true);
        private bool readOnly;

        public MarkupDocumentCollection() : this(false)
        {
        }

        private MarkupDocumentCollection(bool readOnly) : base()
        {
            this.readOnly = readOnly;
        }

        protected override void ClearItems()
        {
            if (this.readOnly)

                throw new InvalidOperationException("Collection is readonly.");

            base.ClearItems();
        }

        protected override void InsertItem(int index, MarkupDocument item)
        {
            if (this.readOnly)

                throw new InvalidOperationException("Collection is readonly.");

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (this.readOnly)

                throw new InvalidOperationException("Collection is readonly.");

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, MarkupDocument item)
        {
            if (this.readOnly)

                throw new InvalidOperationException("Collection is readonly.");

            base.SetItem(index, item);
        }

        protected override Uri GetKeyForItem(MarkupDocument item)
        {
            return item.Locator;
        }

        public static bool IsCached(String targetNamespace)
        {
            return MarkupExtensions.DataTreesCache.ContainsKey(targetNamespace);
        }

        public static MarkupDocumentCollection GetCached(String targetNamespace)
        {
            if (IsCached(targetNamespace))

                return MarkupExtensions.DataTreesCache[targetNamespace];

            return MarkupDocumentCollection.Empty;
        }
    }
}
