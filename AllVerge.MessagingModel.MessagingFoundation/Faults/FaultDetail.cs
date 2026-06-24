using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

using System.ServiceModel;

namespace AllVerge.MessagingModel.MessagingFoundation.Faults
{
    /// <summary>
    /// Transmits the details of a fault message.
    /// </summary>
    [DataContract(Namespace = FaultExceptionsConstants.Namespace)]
    public class FaultDetail
    {
        [DataContract(Namespace = FaultExceptionsConstants.Namespace)]
        public class DetailCode
        {
            [DataMember(Name = "Name")]
            private string name;
            [DataMember(Name = "Namespace")]
            private string @namespace;

            public DetailCode(string name, string @namespace)
            {
                this.name = name;
                this.@namespace = @namespace;
            }

            public DetailCode(FaultCode faultCode)
            {
                this.name = faultCode.Name;
                this.@namespace = faultCode.Namespace;
            }

            public DetailCode(XmlQualifiedName faultCode)
            {
                this.name = faultCode.Name;
                this.@namespace = faultCode.Namespace;
            }

            public DetailCode(Exception faultCode)
            {
                Type faultType = faultCode.GetType();

                this.name = faultType.Name;
                this.@namespace = faultType.Namespace;
            }

            public string Name
            {
                get { return name; }
            }

            public string Namespace
            {
                get { return @namespace; }
            }

            public static implicit operator DetailCode(FaultCode faultCode)
            {
                return new DetailCode(faultCode);
            }

            public static implicit operator DetailCode(XmlQualifiedName faultCode)
            {
                return new DetailCode(faultCode);
            }

            public static implicit operator DetailCode(Exception faultCode)
            {
                return new DetailCode(faultCode);
            }
        }

        /// <summary>
        /// Constant DetailCode when an error code is not understood.
        /// </summary>
        public static readonly DetailCode NOT_UNDERSTOOD_DETAIL_CODE = new DetailCode("NOT-UNDERSTOOD", "urn:allverge.com");

        /// <summary>
        /// Constant message when an error code is not understood.
        /// </summary>
        public const string NOT_UNDERSTOOD_DETAIL_CODE_MESSAGE = "Error code not provided or not understood.";

        /// <summary>
        /// Constant DetailCode when when errors are aggregated as inner details.
        /// </summary>
        public static readonly DetailCode AGGREGATED_INNER_DETAILS_DETAIL_CODE = new DetailCode("AGGREGATED-INNER-DETAILS", "urn:allverge.com");

        /// <summary>
        /// Constant message when errors are aggregated as inner details.
        /// </summary>
        public const string AGGREGATED_INNER_DETAILS_DETAIL_CODE_MESSAGE = "See InnerDetails.";

        /// <summary>
        /// Constant value when an instruction is not available.
        /// </summary>
        public const string INSTRUCTION_NOT_AVAILABLE = "No instruction available.";

        [DataMember(Name= "DetailCode")]
        private DetailCode detailCode;
        [DataMember(Name="Message")]
        private string message;
        [DataMember(Name = "Instruction")]
        private string instruction;
        [DataMember(Name="InnerDetails")]
        private FaultDetail[] innerDetails;
        [DataMember(Name = "Tags")]
        private Tag[] tags;

        /// <summary>
        /// Initializes a new "Not Understood" instance of the <see cref="FaultDetail"/> class.
        /// <param name="notUnderstoodError">The error entity that is not understood.</param>
        /// </summary>
        public FaultDetail(Object notUnderstoodError) :
            this(NOT_UNDERSTOOD_DETAIL_CODE, NOT_UNDERSTOOD_DETAIL_CODE_MESSAGE, INSTRUCTION_NOT_AVAILABLE, new FaultDetail[0], new Tag("Not-Understood-Error", notUnderstoodError))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDetail"/> class.
        /// </summary>
        /// <param name="detailCode">A code name for an understood service error.</param>
        /// <param name="message">A description of the service error.</param>
        /// <param name="tags">Zero or more identifiers or additional data associated with the service error.</param>
        public FaultDetail(DetailCode detailCode, string message, params Tag[] tags) :
            this(detailCode, message, INSTRUCTION_NOT_AVAILABLE, new FaultDetail[0], tags)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDetail"/> class.
        /// </summary>
        /// <param name="detailCode">A code name for an understood service error.</param>
        /// <param name="message">A description of the service error.</param>
        /// <param name="innerDetails">Zero or more inner fault details.</param>
        /// <param name="tags">Zero or more identifiers or additional data associated with the service error.</param>
        public FaultDetail(DetailCode detailCode, string message, FaultDetail[] innerDetails, params Tag[] tags) :
            this(detailCode, message, INSTRUCTION_NOT_AVAILABLE, innerDetails, tags)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDetail"/> class.
        /// </summary>
        /// <param name="detailCode">A code name for an understood service error.</param>
        /// <param name="message">A description of the service error.</param>
        /// <param name="instruction">An instruction for resolving the service error.</param>
        /// <param name="tags">Zero or more identifiers or additional data associated with the service error.</param>
        public FaultDetail(DetailCode detailCode, string message, string instruction, params Tag[] tags) :
            this(detailCode, message, instruction, new FaultDetail[0], tags)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDetail"/> class.
        /// </summary>
        /// <param name="detailCode">A code name for an understood service error.</param>
        /// <param name="message">A description of the service error.</param>
        /// <param name="instruction">An instruction for resolving the service error.</param>
        /// <param name="innerDetails">Zero or more inner fault details.</param>
        /// <param name="data">Zero or more identifiers or additional data associated with the service error.</param>
        public FaultDetail(DetailCode detailCode, string message, string instruction, FaultDetail[] innerDetails, params Tag[] data)
        {
            this.detailCode = detailCode;
            this.message = message;
            this.instruction = instruction;
            this.innerDetails = innerDetails;

            if (MessagingInteractionContext.Current != null)
            {
                List<Tag> tags = new List<Tag>();

                int index;

                for (index = 0; index < data.Length; index++)
                {
                    tags.Add(data[index]);
                }

                if (MessagingInteractionContext.Current.MessageId != null)

                    tags.Add(new FaultDetail.Tag("MessageRequestID", MessagingInteractionContext.Current.MessageId.ToString()));
                
                if (MessagingInteractionContext.Current.RelatedTo != null)

                    tags.Add(new FaultDetail.Tag("RelatedTo", MessagingInteractionContext.Current.RelatedTo.ToString()));

                this.tags = data.ToArray();
            }
            else
                this.tags = data;
        }

        /// <summary>
        /// An code name for the fault.
        /// </summary>
        public DetailCode Code
        {
            get { return this.detailCode; }
        }

        /// <summary>
        /// Fault message.
        /// </summary>
        public string Message
        {
            get { return this.message; }
        }

        /// <summary>
        /// Any additional details or instructions to help diagnose or resolve the fault.
        /// </summary>
        public string Instruction
        {
            get { return this.instruction; }
        }

        /// <summary>
        /// Zero or more inner <see cref="FaultDetail"/>.
        /// </summary>
        public FaultDetail[] InnerDetails
        {
            get { return this.innerDetails; }
        }

        /// <summary>
        /// One or more identifying tags that specify the fault.
        /// </summary>
        public Tag[] Tags
        {
            get { return this.tags; }
        }

        /// <summary>
        /// Returns a text representation of the <see cref="FaultDetail"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format("Fault Detail - Fault ID:  {0}, Message: {1}, Instruction: {2}, Tags: {{3}}", this.Code, this.Message, this.Instruction, String.Join(", ", this.Tags.Select(t => t.ToString()).ToArray())));

            if (this.InnerDetails != null && this.InnerDetails.Length > 0)
            {
                foreach (FaultDetail innerDetail in this.InnerDetails)
                {
                    sb.AppendLine();
                    sb.Append("=>");
                    sb.Append(innerDetail.ToString());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// A structure for holding an identifying tag.
        /// </summary>
        [DataContract(Namespace = FaultExceptionsConstants.Namespace)]
        public struct Tag
        {
            [DataMember(Name = "Name")]
            private string name;
            [DataMember(Name = "Data")]
            private Object data;

            /// <summary>
            /// The tag name.
            /// </summary>
            public string Name
            {
                get
                {
                    return name;
                }
            }

            /// <summary>
            /// The tag specifications.
            /// </summary>
            public object Data
            {
                get
                {
                    return data;
                }
            }

            /// <summary>
            /// Creates an instance of a <see cref="Tag"/>.
            /// </summary>
            /// <param name="name">The tag name.</param>
            /// <param name="data">The tag specifications (must be a value type or System.String).</param>
            public Tag(string name, Object data)
            {
                this.name = name;

                Type dataType = data.GetType();

                if (!(dataType.IsValueType || dataType == typeof(String)))

                    throw new ArgumentException("Must be value type or string.", "data");

                this.data = data;
            }

            /// <summary>
            /// Returns a text representation of the <see cref="Tag"/>.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return String.Format("Tag - Name:  {0}, Data: {1}", this.Name, this.Data.ToString());
            }
        }
    }
}
