using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    internal class OperationMessageFilter : MessageFilter
    {
        private List<MessageFilter> innerFilters;

        public OperationMessageFilter(List<MessageFilter> innerFilters)
        {
            this.innerFilters = innerFilters;
        }

        public override bool Match(MessageBuffer buffer)
        {
            using (Message message = buffer.CreateMessage())
            {
                return this.Match(message);
            }
        }

        public override bool Match(Message message)
        {
            foreach (MessageFilter innerFilter in this.innerFilters)
            {
                if (innerFilter.Match(message))

                    return true;
            }

            return false;
        }

        protected override IMessageFilterTable<FilterData> CreateFilterTable<FilterData>()
        {
            throw new NotImplementedException("TBD");
        }
    }
}