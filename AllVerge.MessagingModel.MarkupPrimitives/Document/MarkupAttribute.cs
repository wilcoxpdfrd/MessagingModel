using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public class MarkupAttribute
    {
        private string attributeName;
        private string attributeValue;

        public MarkupAttribute(string attributeName, string attributeValue)
        {
            this.attributeName = attributeName;
            this.attributeValue = attributeValue;
        }

        public string Name
        {
            get
            {
                return attributeName;
            }
        }

        public string Value
        {
            get
            {
                return attributeValue;
            }
        }

        public MarkupAttribute Clone()
        {
            return new MarkupAttribute((String)this.Name.Clone(), (String)this.Value.Clone());
        }
    }
}
