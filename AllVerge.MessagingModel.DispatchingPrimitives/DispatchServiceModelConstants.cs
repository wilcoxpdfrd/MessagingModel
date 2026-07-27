using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation;

namespace AllVerge.MessagingModel.DispatchingPrimitives
{
    /// <summary>
    /// Contains constants for services built using components provided by the <see cref="AllVerge.MessagingModel.DispatchPrimitives"/> namespace.
    /// </summary>
    public class DispatchServiceModelConstants
    {
        /// <summary>
        /// The namespace for the dispatch service contract.
        /// </summary>
        public const string DispatchServiceNamespace = MessagingModelConstants.Namespace + "Dispatch/";

        /// <summary>
        /// The property name for the message forwarded timestamp.
        /// </summary>
        public const string ForwardedDateProperty = "Forwarded-Date";

        /// <summary>
        /// The property name for the message origination timestamp.
        /// </summary>
        public const string DateProperty = "Date";

        /// <summary>
        /// The namespace for the dispatch service via virtual path.
        /// </summary>
        public const string DispatchServicePath = "dispatch/via/";
    }
}
