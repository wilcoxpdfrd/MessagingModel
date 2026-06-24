using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    class UriTemplateFilterTable<FilterData> : IMessageFilterTable<FilterData>
    {
        Dictionary<MessageFilter, FilterData> filters;
        Dictionary<MessageFilter, FilterData> wildCardFilters;

        private Uri serviceAddress;

        public UriTemplateFilterTable()
        {
            Init();
        }

        public UriTemplateFilterTable(Uri serviceAddress, bool addTrailingSlashToBaseAddress)
        {
            this.serviceAddress = NormalizeServiceAddress(serviceAddress, addTrailingSlashToBaseAddress);

            this.Init();
        }

        private static Uri NormalizeServiceAddress(Uri serviceAddress, bool addTrailingSlashToBaseAddress)
        {
            UriBuilder ub = new UriBuilder(serviceAddress);
            if (addTrailingSlashToBaseAddress && !ub.Path.EndsWith("/", StringComparison.Ordinal))
            {
                ub.Path = ub.Path + "/";
            }
            return ub.Uri;
        }

        void Init()
        {
            this.filters = new Dictionary<MessageFilter, FilterData>();
            this.wildCardFilters = new Dictionary<MessageFilter, FilterData>();
        }

        public FilterData this[MessageFilter filter]
        {
            get
            {
                return this.filters[filter];
            }
            set
            {
                if (this.filters.ContainsKey(filter))
                {
                    this.filters[filter] = value;
                }
                else
                {
                    this.Add(filter, value);
                }
            }
        }

        public int Count
        {
            get
            {
                return this.filters.Count;
            }
        }

        [DataMember(IsRequired = true)]
        Entry[] Entries
        {
            get
            {
                Entry[] entries = new Entry[Count];
                int i = 0;
                foreach (KeyValuePair<MessageFilter, FilterData> item in filters)
                    entries[i++] = new Entry(item.Key, item.Value);

                return entries;
            }
            set
            {
                Init();

                for (int i = 0; i < value.Length; ++i)
                    Add(value[i].filter, value[i].data);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<MessageFilter> Keys
        {
            get
            {
                return this.filters.Keys;
            }
        }

        public ICollection<FilterData> Values
        {
            get
            {
                return this.filters.Values;
            }
        }

        public void Add(UriTemplateFilter filter, FilterData data)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("filter");
            }

            if (filter is WildcardTemplateFilter)

                this.wildCardFilters.Add(filter, data);

            else
            
                this.filters.Add(filter, data);
        }

        public void Add(MessageFilter filter, FilterData data)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("filter");
            }

            Add((UriTemplateFilter)filter, data);
        }

        public void Add(KeyValuePair<MessageFilter, FilterData> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this.filters.Clear();
            this.wildCardFilters.Clear();
        }

        public bool Contains(KeyValuePair<MessageFilter, FilterData> item)
        {
            return ((ICollection<KeyValuePair<MessageFilter, FilterData>>)this.filters).Contains(item);
        }

        public bool ContainsKey(MessageFilter filter)
        {
            return this.filters.ContainsKey(filter);
        }

        public void CopyTo(KeyValuePair<MessageFilter, FilterData>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<MessageFilter, FilterData>>)this.filters).CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<MessageFilter, FilterData>> GetEnumerator()
        {
            return ((ICollection<KeyValuePair<MessageFilter, FilterData>>)this.filters).GetEnumerator();
        }

        MessageFilter InnerMatch(Message message)
        {
            foreach (MessageFilter filter in this.filters.Keys)
            {
                if (filter.Match(message))

                    return filter;
            }

            if (this.wildCardFilters.Count > 1)
            {
                Collection<MessageFilter> matches = new Collection<MessageFilter>(new List<MessageFilter>(this.wildCardFilters.Keys));
                throw TraceUtility.ThrowHelperError(new MultipleFilterMatchesException(PublicSR.Format(PublicSR.FilterMultipleMatches), null, matches), message);
            }

            return this.wildCardFilters.FirstOrDefault().Key;
        }

        void InnerMatch(Message message, ICollection<MessageFilter> results)
        {
            foreach (MessageFilter filter in this.filters.Keys)
            {
                if (filter.Match(message))

                    results.Add(filter);
            }

            this.wildCardFilters.Aggregate(results, (r, f) => { r.Add(f.Key); return r; });
        }

        void InnerMatchData(Message message, ICollection<FilterData> results)
        {
            foreach (MessageFilter filter in this.filters.Keys)
            {
                if (filter.Match(message))

                    results.Add(this.filters[filter]);
            }

            this.wildCardFilters.Aggregate(results, (r, f) => { r.Add(f.Value); return r; });
        }

        public bool GetMatchingValue(Message message, out FilterData data)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
            }

            MessageFilter f = InnerMatch(message);
            if (f == null)
            {
                data = default(FilterData);
                return false;
            }

            data = this.filters[f];
            return true;
        }

        public bool GetMatchingValue(MessageBuffer messageBuffer, out FilterData data)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("messageBuffer");
            }

            MessageFilter f = null;
            Message msg = messageBuffer.CreateMessage();
            try
            {
                f = InnerMatch(msg);
            }
            finally
            {
                msg.Close();
            }

            if (f == null)
            {
                data = default(FilterData);
                return false;
            }

            data = this.filters[f];
            return true;
        }

        public bool GetMatchingFilter(Message message, out MessageFilter filter)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
            }

            filter = InnerMatch(message);
            return filter != null;
        }

        public bool GetMatchingFilter(MessageBuffer messageBuffer, out MessageFilter filter)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("messageBuffer");
            }

            Message msg = messageBuffer.CreateMessage();
            try
            {
                filter = InnerMatch(msg);
                return filter != null;
            }
            finally
            {
                msg.Close();
            }
        }

        public bool GetMatchingFilters(Message message, ICollection<MessageFilter> results)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("results");
            }

            int count = results.Count;
            InnerMatch(message, results);
            return count != results.Count;
        }

        public bool GetMatchingFilters(MessageBuffer messageBuffer, ICollection<MessageFilter> results)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("messageBuffer");
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("results");
            }

            Message msg = messageBuffer.CreateMessage();
            try
            {
                int count = results.Count;
                InnerMatch(msg, results);
                return count != results.Count;
            }
            finally
            {
                msg.Close();
            }
        }

        public bool GetMatchingValues(Message message, ICollection<FilterData> results)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("results");
            }

            int count = results.Count;
            InnerMatchData(message, results);
            return count != results.Count;
        }

        public bool GetMatchingValues(MessageBuffer messageBuffer, ICollection<FilterData> results)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("messageBuffer");
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("results");
            }

            Message msg = messageBuffer.CreateMessage();
            try
            {
                int count = results.Count;
                InnerMatchData(msg, results);
                return count != results.Count;
            }
            finally
            {
                msg.Close();
            }
        }

        public bool Remove(UriTemplateFilter filter)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("filter");
            }

            if (this.wildCardFilters.ContainsKey(filter))

                return this.wildCardFilters.Remove(filter);

            else

                return this.filters.Remove(filter);
        }

        public bool Remove(MessageFilter filter)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("filter");
            }

            UriTemplateFilter aFilter = filter as UriTemplateFilter;
            if (aFilter != null)
            {
                return Remove(aFilter);
            }
            return false;
        }

        public bool Remove(KeyValuePair<MessageFilter, FilterData> item)
        {
            if (((ICollection<KeyValuePair<MessageFilter, FilterData>>)this.filters).Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        public bool TryGetValue(MessageFilter filter, out FilterData data)
        {
            return this.filters.TryGetValue(filter, out data);
        }

        [DataContract]
        class Entry
        {
            [DataMember(IsRequired = true)]
            internal MessageFilter filter;

            [DataMember(IsRequired = true)]
            internal FilterData data;

            internal Entry(MessageFilter f, FilterData d)
            {
                filter = f;
                data = d;
            }
        }
    }
}
