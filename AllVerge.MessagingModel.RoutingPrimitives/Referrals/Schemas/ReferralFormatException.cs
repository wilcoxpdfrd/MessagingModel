using System;
using System.Runtime.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    using AllVerge.PolicyPrimitives;

    [Serializable]
    internal class ReferralFormatException : Exception
    {
        public static readonly string BadRefIdValue = APPR.BadRefId;
        public static readonly string BadMatchCombination = APPR.BadMatchCombination;
        public static readonly string BadDescValue = APPR.BadDesc;
        public static readonly string BadExactValue = APPR.BadExact;
        public static readonly string BadPrefixValue = APPR.BadPrefix;
        public static readonly string BadViaValue = APPR.BadVia;
        public static readonly string NegativeTtlValue = APPR.NegativeTtl;
        public static readonly string BadTtlValue = APPR.BadTtl;
        public static readonly string BadTransport = APPR.BadTransport;
        public static readonly string BadRidValue = APPR.BadRid;
        public static readonly string SignedTtlValue = APPR.SignedTTL;
        public static readonly string ExactIsNotAbsoluteUri = APPR.ExactIsNotAbsoluteUri;
        public static readonly string PrefixIsNotAbsoluteUri = APPR.PrefixIsNotAbsoluteUri;
        public static readonly string ViaIsNotAbsoluteUri = APPR.ViaIsNotAbsoluteUri;
        public static readonly string MoreThanOneReferralHeaders = APPR.MoreThanOneReferralHeaders;
        public static readonly string BadRefAddrValue = APPR.BadRefAddr;
        public static readonly string BadCreatedValue = APPR.BadCreated;
        public static readonly string RefAddrIsNotAbsoluteUri = APPR.RefAddrIsNotAbsoluteUri;

        public ReferralFormatException() : base()
        {
        }

        public ReferralFormatException(string message) : base(message)
        {
        }

        public ReferralFormatException(uri_t refId, string message) : base(BuildMessage(refId, message))
        {
        }

        public ReferralFormatException(uri_t refId, string message, Exception innerException) : base(BuildMessage(refId, message), innerException)
        {
        }

        protected ReferralFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public static string BuildMessage(uri_t refid, string message)
        {
            if (refid != null)
            {
                return message + " The Referral Id is: " + refid.Value;
            }
            return message;
        }
    }
}