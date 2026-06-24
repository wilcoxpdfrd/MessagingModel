using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public class MarkupAttributes : KeyedCollection<String, MarkupAttribute>
    {
        protected override string GetKeyForItem(MarkupAttribute item)
        {
            return item.Name;
        }

        public MarkupAttributes Clone()
        {
            return this.Aggregate(new MarkupAttributes(), (c, a) => { c.Add(a.Clone()); return c; });
        }
    }
}
