using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class DispatchOperationAccessData
    {
        public String[] AllowedMethods { get; internal set; }

        public string[] AllowedHeaders { get; internal set; }


        public string AllowMethodsHeaderValue
        {
            get
            {
                if (this.AllowedMethods != null)
                {
                    int count = this.AllowedMethods.Length;
                    if (count > 0)
                    {
                        StringBuilder stringBuilder = new StringBuilder(this.AllowedMethods[0]);
                        for (int i = 1; i < count; i++)
                        {
                            stringBuilder.Append(", " + this.AllowedMethods[i]);
                        }
                        return stringBuilder.ToString();
                    }
                }
                return null;
            }
        }

        public string AllowHeadersHeaderValue
        {
            get
            {
                if (this.AllowedHeaders != null)
                {
                    int count = this.AllowedHeaders.Length;
                    if (count > 0)
                    {
                        StringBuilder stringBuilder = new StringBuilder(this.AllowedHeaders[0]);
                        for (int i = 1; i < count; i++)
                        {
                            stringBuilder.Append(", " + this.AllowedHeaders[i]);
                        }
                        return stringBuilder.ToString();
                    }
                }
                return null;
            }
        }

        public bool IsMethodAllowed(string method)
        {
            if (AllowedMethods.Length > 0)
            {
                return Array.IndexOf(AllowedMethods, method) != -1;
            }
            return false;
        }
    }
}
