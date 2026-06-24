using System;
using System.Collections.ObjectModel;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class UrlPrefixCollection : Collection<Uri>
    {
        public UrlPrefixCollection() : base() { }

        protected override void InsertItem(int index, Uri item)
        {
            if (!item.IsAbsoluteUri)

                throw new ArgumentException("Item must be an absolute Uri.");

            base.InsertItem(index, item);
        }
    }
}