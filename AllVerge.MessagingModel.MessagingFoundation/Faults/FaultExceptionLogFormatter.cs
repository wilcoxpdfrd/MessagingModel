using System;
using System.Collections.Generic;
using System.ServiceModel;
using AllVerge.SystemPrimitives.Exceptions;
using AllVerge.SystemPrimitives.Logging;

namespace AllVerge.MessagingModel.MessagingFoundation.Faults
{
    /// <summary>
    /// 
    /// </summary>
    public class FaultExceptionLogFormatterFactory : ObjectLogFormatterFactory<FaultException>
    {
        protected override IObjectLogFormatter<FaultException> GetLogFormatterInstance<ObjectType>(string loggerName)
        {
            return new FaultExceptionLogFormatter(loggerName);
        }
    }

    /// <summary>
    /// Maps a <see cref="FaultException"/> to an array of <see cref="LogEvent"/>.
    /// </summary>
    public class FaultExceptionLogFormatter : ExceptionLogFormatter<FaultException>
    {
        public FaultExceptionLogFormatter(string loggerName) : base(loggerName) { }

        protected override void FillLogEvents(List<LogEvent> logEvents, FaultException exception)
        {
            base.FillLogEvents(logEvents, exception);

            if (exception is FaultException<FaultDetail>)
            {
                FaultException<FaultDetail> faultDetailException = (FaultException<FaultDetail>)exception;

                AddLogEvent(faultDetailException.Detail, logEvents);

                foreach (FaultDetail faultDetail in faultDetailException.Detail.InnerDetails)

                    AddLogEvent(faultDetail, logEvents);
            }
        }

        protected override LogEvent GetLogEvent(Exception exception, bool isInnerException)
        {
            LogEvent logEvent = base.GetLogEvent(exception, isInnerException);

            if (exception is FaultException)
            {
                FaultException faultException = (FaultException)exception;

                logEvent.Properties["Action"] = faultException.Action;

                logEvent.Properties["Code"] = faultException.Code.GetCodeString();

                logEvent.Properties["Reason"] = faultException.Reason;
            }

            return logEvent;
        }

        protected void AddLogEvent(FaultDetail faultDetail, List<LogEvent> logEvents)
        {
            LogEvent logEvent = new LogEvent(LoggerName, LoggerType.Exception, Severity.ERROR, faultDetail.Message);

            logEvent.Properties["Code"] = faultDetail.Code;

            logEvent.Properties["Instruction"] = faultDetail.Instruction;

            foreach (FaultDetail.Tag tag in faultDetail.Tags)

                logEvent.Properties.Add("TagName: " + tag.Name, tag.Data);

            logEvents.Add(logEvent);
        }
    }
}
