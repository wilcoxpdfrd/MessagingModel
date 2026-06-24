using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Diagnostics
{
    public abstract class TypedIndicator : IIndicator
    {
        private string categoryName;
        private string instanceName;
        private string measurement;
        private string name;

        public TypedIndicator(string categoryName, string instanceName, string name, string measurement)
        {
            this.categoryName = categoryName;
            this.instanceName = instanceName;
            this.name = name;
            this.measurement = measurement;
        }

        public string CategoryName
        {
            get
            {
                return this.categoryName;
            }
        }

        public string InstanceName
        {
            get
            {
                return this.instanceName;
            }
        }

        public string Measurement
        {
            get
            {
                return this.measurement;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }


    }
}
