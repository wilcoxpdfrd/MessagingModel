using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Faults
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    internal static class FaultExceptionExtensions
    {
        private static readonly MethodInfo CreateDetailedFaultOpenMI = typeof(FaultExceptionExtensions).GetMethods().First(m => m.Name == "CreateFault" && m.IsGenericMethodDefinition);
        public static readonly String DefaultMessage = PublicSR.SFxFaultReason;
        public static readonly FaultReason DefaultReason = new FaultReason(DefaultMessage);
        public static readonly FaultCode DefaultCode = new FaultCode("Sender");
        public static readonly String DefaultAction = null;

        public static MessageFault CreateFault(this FaultException faultException)
        {
            Type faultExceptionType = faultException.GetType();

            if (faultExceptionType.IsGenericType)
            {
                MethodInfo createDetailedFaultClosedMI = CreateDetailedFaultOpenMI.MakeGenericMethod(faultExceptionType.GetGenericArguments());

                return (MessageFault)createDetailedFaultClosedMI.Invoke(null, new[] { faultException });
            }

            return faultException.CreateMessageFault();
        }

        public static MessageFault CreateFault<TDetail>(this FaultException<TDetail> faultException)
        {
            return new XmlObjectSerializerFault(faultException.Code, faultException.Reason, faultException.Detail, new ReaderWriterAdaptiveDataContractSerializer(faultException.Detail == null ? typeof(object) : faultException.Detail.GetType(), int.MaxValue), "", "");
        }

        public static FaultException<FaultDetail> CreateFaultException<FaultDetail>(this FaultDetail faultDetail)
        {
            return faultDetail.CreateFaultException(DefaultCode, DefaultReason, DefaultAction);
        }

        public static FaultException<FaultDetail> CreateFaultException<FaultDetail>(this FaultDetail faultDetail, FaultCode code, FaultReason reason)
        {
            return faultDetail.CreateFaultException(code, reason, DefaultAction);
        }

        public static FaultException<FaultDetail> CreateFaultException<FaultDetail>(this FaultDetail faultDetail, FaultCode code, FaultReason reason, string action)
        {
            return new FaultException<FaultDetail>(faultDetail, reason, code, action);
        }

        public static FaultException CreateFaultException(this FaultReason reason, FaultCode code)
        {
            return reason.CreateFaultException(code, DefaultAction);
        }

        public static FaultException CreateFaultException(this FaultReason reason, FaultCode code, string action)
        {
            return new FaultException(reason, code, action);
        }

        public static FaultException CreateFaultException(this MessageFault fault)
        {
            if (fault == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(fault));
            }

            return new FaultException(GetReason(fault), EnsureCode(fault.Code), DefaultAction);
        }

        private static FaultReason GetReason(MessageFault fault)
        {
            if (fault == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(fault));
            }

            return fault.Reason;
        }

        private static FaultCode EnsureCode(FaultCode code)
        {
            if (code == null)
            {
                return DefaultCode;
            }
            return code;
        }

        public static bool IsEqualTo(this FaultCode code, FaultCode faultCode, bool drillDown = true)
        {
            if (faultCode == null)

                return false;

            if (code.Name == faultCode.Name && code.Namespace == faultCode.Namespace)
            {
                if (drillDown && code.SubCode != null)

                    return code.SubCode.IsEqualTo(faultCode.SubCode);

                return true;
            }

            return false;
        }
    }
}
