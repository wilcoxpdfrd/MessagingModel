using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Diagnostics
{
    public class LongIndicator : TypedIndicator
    {
        private long value;

        public LongIndicator(string categoryName, string instanceName, string name, string measurement, long value) : base(categoryName, instanceName, name, measurement)
        {
            this.value = value;
        }

        public long Value
        {
            get
            {
                return this.value;
            }
        }
    }
}
