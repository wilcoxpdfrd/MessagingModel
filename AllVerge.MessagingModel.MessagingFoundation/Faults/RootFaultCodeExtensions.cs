using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Faults
{
    public static class RootFaultCodeExtensions
    {
        public static FaultException CreateFaultException(this RootFaultCode rootFaultCode, Type sourceType)
        {
            return rootFaultCode.CreateFaultException(MessageVersion.Default.Envelope, sourceType);
        }

        public static FaultException CreateFaultException(this RootFaultCode rootFaultCode, FaultReason faultReason, Type sourceType)
        {
            return rootFaultCode.CreateFaultException(faultReason, MessageVersion.Default.Envelope, sourceType);
        }

        public static FaultException CreateFaultException(this RootFaultCode rootFaultCode, EnvelopeVersion envelopeVersion, Type sourceType)
        {
            return rootFaultCode.WrapFaultCode(envelopeVersion).CreateFaultException(rootFaultCode.Reason, sourceType);
        }

        public static FaultException CreateFaultException(this RootFaultCode rootFaultCode, FaultReason faultReason, EnvelopeVersion envelopeVersion, Type sourceType)
        {
            return rootFaultCode.WrapFaultCode(envelopeVersion).CreateFaultException(faultReason, sourceType);
        }

        public static FaultException<TDetail> CreateFaultException<TDetail>(this RootFaultCode rootFaultCode, TDetail faultDetail, Type sourceType)
        {
            return rootFaultCode.CreateFaultException(faultDetail, MessageVersion.Default.Envelope, sourceType);
        }

        public static FaultException<TDetail> CreateFaultException<TDetail>(this RootFaultCode rootFaultCode, FaultReason faultReason, TDetail faultDetail, Type sourceType)
        {
            return rootFaultCode.CreateFaultException(faultReason, faultDetail, MessageVersion.Default.Envelope, sourceType);
        }

        public static FaultException<TDetail> CreateFaultException<TDetail>(this RootFaultCode rootFaultCode, TDetail faultDetail, EnvelopeVersion envelopeVersion, Type sourceType)
        {
            return rootFaultCode.WrapFaultCode(envelopeVersion).CreateDetailFaultException(rootFaultCode.Reason, faultDetail, sourceType);
        }

        public static FaultException<TDetail> CreateFaultException<TDetail>(this RootFaultCode rootFaultCode, FaultReason faultReason, TDetail faultDetail, EnvelopeVersion envelopeVersion, Type sourceType)
        {
            return rootFaultCode.WrapFaultCode(envelopeVersion).CreateDetailFaultException(faultReason, faultDetail, sourceType);
        }
    }
}
