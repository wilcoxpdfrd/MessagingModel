using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Faults
{
    public enum FaultBlame
    {
        Sender,
        Receiver
    }

    public abstract class RootFaultCode
    {
        protected RootFaultCode(FaultCode subCode, FaultReason faultReason)
        {
            Code = subCode;
            Reason = faultReason;
        }

        protected RootFaultCode(FaultCode subCode)
        {
            Code = subCode;
        }

        protected RootFaultCode()
        {
        }

        public virtual FaultCode WrapFaultCode()
        {
            return WrapFaultCode(null);
        }

        public abstract FaultCode WrapFaultCode(EnvelopeVersion envelopeVersion);

        public abstract FaultBlame FaultCodeType { get; }

        public FaultCode Code { get; }

        public FaultReason Reason { get; }

        public static RootFaultCode CreateReceiverFaultCode() { return new ReceiverFaultCode(); }

        public static RootFaultCode CreateReceiverFaultCode(FaultCode subCode) { return new ReceiverFaultCode(subCode); }

        public static RootFaultCode CreateReceiverFaultCode(FaultCode subCode, FaultReason faultReason) { return new ReceiverFaultCode(subCode, faultReason); }

        public static RootFaultCode CreateSenderFaultCode() { return new SenderFaultCode(); }

        public static RootFaultCode CreateSenderFaultCode(FaultCode subCode) { return new SenderFaultCode(subCode); }

        public static RootFaultCode CreateSenderFaultCode(FaultCode subCode, FaultReason faultReason) { return new SenderFaultCode(subCode, faultReason); }

        public static RootFaultCode Create(FaultBlame blame) { switch (blame) { case FaultBlame.Sender: return new SenderFaultCode(); case FaultBlame.Receiver: return new ReceiverFaultCode(); default: throw new ArgumentOutOfRangeException(nameof(blame), $"{blame}"); } }

        public static RootFaultCode Create(FaultBlame blame, FaultCode subCode) { switch (blame) { case FaultBlame.Sender: return new SenderFaultCode(subCode); case FaultBlame.Receiver: return new ReceiverFaultCode(subCode); default: throw new ArgumentOutOfRangeException(nameof(blame), $"{blame}"); } }

        public static RootFaultCode Create(FaultBlame blame, FaultCode subCode, FaultReason faultReason) { switch (blame) { case FaultBlame.Sender: return new SenderFaultCode(subCode, faultReason); case FaultBlame.Receiver: return new ReceiverFaultCode(subCode, faultReason); default: throw new ArgumentOutOfRangeException(nameof(blame), $"{blame}"); } }
    }

    public class ReceiverFaultCode : RootFaultCode
    {
        internal ReceiverFaultCode(FaultCode subCode, FaultReason faultReason) :
            base(subCode, faultReason)
        {
        }

        internal ReceiverFaultCode(FaultCode subCode) :
            base(subCode)
        {
        }

        internal ReceiverFaultCode() :
            base()
        {
        }

        public override FaultBlame FaultCodeType => FaultBlame.Receiver;

        public override FaultCode WrapFaultCode(EnvelopeVersion envelopeVersion)
        {
            if (envelopeVersion == EnvelopeVersion.Soap12)
            {
                return new FaultCode("Receiver", "http://www.w3.org/2003/05/soap-envelope", Code);
            }
            else if (envelopeVersion == EnvelopeVersion.Soap11)
            {
                return new FaultCode("Server", "http://schemas.xmlsoap.org/soap/envelope/", Code);
            }
            else if (envelopeVersion == EnvelopeVersion.None)
            {
                return new FaultCode("Receiver", "http://schemas.microsoft.com/ws/2005/05/envelope/none", Code);
            }
            else //if (envelopeVersion == null)
            {
                return new FaultCode("Receiver", Code);
            }
        }
    }

    public class SenderFaultCode : RootFaultCode
    {
        internal SenderFaultCode(FaultCode subCode, FaultReason faultReason) :
            base(subCode, faultReason)
        {

        }

        internal SenderFaultCode(FaultCode subCode) :
            base(subCode)
        {

        }

        internal SenderFaultCode() :
            base()
        {

        }

        public override FaultBlame FaultCodeType => FaultBlame.Sender;

        public override FaultCode WrapFaultCode(EnvelopeVersion envelopeVersion)
        {
            if (envelopeVersion == EnvelopeVersion.Soap12)
            {
                return new FaultCode("Sender", "http://www.w3.org/2003/05/soap-envelope", Code);
            }
            else if (envelopeVersion == EnvelopeVersion.Soap11)
            {
                return new FaultCode("Client", "http://schemas.xmlsoap.org/soap/envelope/", Code);
            }
            else if (envelopeVersion == EnvelopeVersion.None)
            {
                return new FaultCode("Sender", "http://schemas.microsoft.com/ws/2005/05/envelope/none", Code);
            }
            else
            {
                return new FaultCode("Sender", Code);
            }
        }
    }
}
