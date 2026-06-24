using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public class MarkupDocument
    {
        private Uri rootLocator;
        private MarkupNode rootNode;

        public MarkupDocument(Uri rootLocator, MarkupNode rootNode)
        {
            this.rootLocator = rootLocator;
            this.rootNode = rootNode;
        }

        public Uri Locator { get { return this.rootLocator; } }

        public MarkupNode RootNode { get { return this.rootNode; } }
    }
}
