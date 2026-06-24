using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    internal class EndpointOperationFilterProvider : EndpointFilterProvider
    {
        private bool unAddressedActionSpecified;
        private IEnumerable<MessageFilter> messageFilters;

        public EndpointOperationFilterProvider(bool unAddressedActionSpecified, IEnumerable<MessageFilter> messageFilters, params string[] initiatingActions) : base(initiatingActions)
        {
            this.unAddressedActionSpecified = unAddressedActionSpecified;
            this.messageFilters = messageFilters;
        }

        public override MessageFilter CreateFilter(out int priority)
        {
            if (this.unAddressedActionSpecified)

                // add an empty initiating action ...

                base.InitiatingActions.Add(string.Empty);

            MessageFilter endpointFilter = base.CreateFilter(out priority);

            if (this.messageFilters.Count() == 0)

                return endpointFilter;

            List<string> actions = new List<string>();
            List<MessageFilter> filters = new List<MessageFilter>();

            if (endpointFilter is ActionMessageFilter)

                actions.AddRange((endpointFilter as ActionMessageFilter).Actions);

            foreach (MessageFilter messageFilter in messageFilters)

                if (messageFilter is ActionMessageFilter)

                    (messageFilter as ActionMessageFilter).Actions.Aggregate(actions, (c, a) =>
                    {
                        if (!c.Contains(a))

                            c.Add(a);

                        return c;
                    });
                else

                    filters.Add(messageFilter);

            if (actions.Count == 0 && filters.Count == 0)

                return new MatchNoneMessageFilter();

            if (actions.Any(a => a == MessageHeaders.WildcardAction) ||
                filters.Any(f =>
                {
                    if (f is MatchAllMessageFilter)

                        return true;

                    if (f is UriTemplateFilter && (f as UriTemplateFilter).Action == $"{MessageHeaders.WildcardAction}/{MessageHeaders.WildcardAction}")

                        return true;

                    return false;

                }))
            {
                priority = 0;

                return new MatchAllMessageFilter();
            }

            if (actions.Count > 0)
            {
                filters.Add(new ActionMessageFilter(actions.ToArray()));
            }

            return new OperationMessageFilter(filters);
        }
    }
}