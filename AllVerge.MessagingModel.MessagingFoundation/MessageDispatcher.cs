
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Logging;
    using AllVerge.SystemPrimitives.Threading.Tasks;

    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;

    using AllVerge.MessagingModel.MessagingApplication;

    public abstract class MessageDispatcher<MessagingContext, MessageType> : 
        IMessageDispatcher<MessagingContext, MessageType> where MessageType : class
    {
        Logger logger;

        protected MessageDispatcher()
        {
        }

        private IDispatcherRuntime dispatcherRuntime;

        public void Init(IDispatcherRuntime dispatchRuntime)
        {
            this.dispatcherRuntime = dispatchRuntime;
        }

        /// <summary>
        /// Gets an instance of the <see cref="Logging.Logger"/> with which to perform logging.
        /// </summary>
        protected virtual Logger Logger
        {
            get
            {
                if (this.logger == null)
                {
                    this.logger = Logger.GetInstance(this.GetType());

                    this.logger.Log(LoggerType.Info, Severity.TRACE, 1, "Logger initialized.");
                }
                return this.logger;
            }
        }

        public Task<MessageType> DispatchMessageAsync(IMessagingContext<MessagingContext> messagingContext, MessageType incomingMessage)
        {
            Logger.Log(LoggerType.Info, Severity.TRACE, $"{nameof(DispatchMessageAsync)}; TraceIdentifier: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");

            if (this.TryPrepareIncomingMessageEventArgs(messagingContext, incomingMessage, out IncomingMessageEventArgs incomingMessageEventArgs, out Exception prepareIncomingMessageEventArgsException))
            {
                messagingContext.AddAuthenticationChangeListener(incomingMessageEventArgs.OnAuthenticationChange);

                return this.DispatchOperationAsync(messagingContext, incomingMessageEventArgs).ContinueWith(t =>
                {
                    OutgoingMessageEventArgs outgoingMessageEventArgs = t.Result as OutgoingMessageEventArgs;

                    if (this.TryPrepareOutgoingMessage(outgoingMessageEventArgs, out MessageType outgoingMessage, out Exception prepareOutgoingMessageException))
                    {
                        messagingContext.RemoveAuthenticationChangeListener(outgoingMessageEventArgs.OnAuthenticationChange);
                    }
                    else
                    {
                        Logger.Log(prepareIncomingMessageEventArgsException);

                        outgoingMessage = this.PrepareOutgoingFaultMessage(incomingMessage, prepareIncomingMessageEventArgsException);

                        messagingContext.RemoveAuthenticationChangeListener(incomingMessageEventArgs.OnAuthenticationChange);
                    }

                    MessagingInteractionContext.ClearCurrentMessageContext();

                    return outgoingMessage;
                });
            }
            else
            {
                Logger.Log(prepareIncomingMessageEventArgsException);

                return Task.FromResult(this.PrepareOutgoingFaultMessage(incomingMessage, prepareIncomingMessageEventArgsException));
            }
        }

        protected abstract bool TryPrepareIncomingMessageEventArgs(IMessagingContext<MessagingContext> messagingContext, MessageType incomingMessage, out IncomingMessageEventArgs incomingMessageEventArgs, out Exception prepareIncomingMessageEventArgsException);

        /// <summary>
        /// This method should not fault!
        /// Any exception should be handled and an <see cref="OutgoingMessageEventArgs"/> instance reflectng the exception prepared and returned.
        /// </summary>
        /// <param name="incomingMessageEventArgs"></param>
        /// <returns></returns>
        private Task<OutgoingMessageEventArgs> DispatchOperationAsync(IMessagingContext<MessagingContext> messagingContext, IncomingMessageEventArgs incomingMessageEventArgs)
        {
            Uri replyTo = incomingMessageEventArgs.ReplyTo?.Uri;
            UniqueId replyRelatesTo = incomingMessageEventArgs.RequestId;

            String operationName = this.dispatcherRuntime.DispatchOperationName;

            Task<OutgoingMessageEventArgs> dispatchOperationTask;
            
            if (operationName == null)
            {
                this.Logger.Log(LoggerType.Info, Severity.TRACE, $"{nameof(DispatchOperationAsync)}: no matching operation found for message; TraceIdentifier: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");

                Message outgoingMessage =
                    MessageFaultHelper.CreateMessage(
                        incomingMessageEventArgs.Version,
                        MessageFaultHelper.CreateFault(
                            FaultCodes.ClientErrorCode.NotFound.WrapFaultCode(incomingMessageEventArgs.Version.Envelope),
                            "Operation not found."),
                        incomingMessageEventArgs.Headers.GetResponseFaultAction()).
                    TrySetTo(replyTo).
                    TrySetRelatesTo(replyRelatesTo);

                using (outgoingMessage)
                {
                    dispatchOperationTask = OutgoingMessageEventArgs
                        .Create(incomingMessageEventArgs, outgoingMessage)
                        .GetCompletedTask(this.dispatcherRuntime);
                }
            }
            else if (operationName == String.Empty)
            {
                Uri redirectUri;

                Message outgoingMessage;

                if (incomingMessageEventArgs.Properties.TryGetProperty(IncomingMessageEventArgs.DispatchOperationRedirectUriPropertyName, out redirectUri))
                {
                    this.Logger.Log(LoggerType.Info, Severity.TRACE, $"{nameof(DispatchOperationAsync)}: redirecting message; TraceIdentifier: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");

                    outgoingMessage =
                        MessageFaultHelper.CreateMessage(
                            incomingMessageEventArgs.Version,
                            MessageFaultHelper.CreateFault(
                                FaultCodes.ClientRedirectionCode.Redirect.WrapFaultCode(incomingMessageEventArgs.Version.Envelope),
                                redirectUri.AbsoluteUri),
                            incomingMessageEventArgs.Headers.GetResponseFaultAction()).
                        TrySetTo(replyTo).
                        TrySetRelatesTo(replyRelatesTo);
                }
                else
                {
                    this.Logger.Log(LoggerType.Info, Severity.TRACE, $"{nameof(DispatchOperationAsync)}: faulting on redirect message (no redirect url); TraceIdentifier: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");

                    outgoingMessage =
                        MessageFaultHelper.CreateMessage(
                            incomingMessageEventArgs.Version,
                            MessageFaultHelper.CreateFault(
                                FaultCodes.ServerErrorCode.ServiceFaulted.WrapFaultCode(incomingMessageEventArgs.Version.Envelope),
                                "Redirect required but no redirect Uri was found."),
                            incomingMessageEventArgs.Headers.GetResponseFaultAction()).
                        TrySetTo(replyTo).
                        TrySetRelatesTo(replyRelatesTo);
                }

                using (outgoingMessage)
                {
                    dispatchOperationTask = OutgoingMessageEventArgs.Create(incomingMessageEventArgs, outgoingMessage).GetCompletedTask(this.dispatcherRuntime);
                }
            }
            else if (operationName == "HelpPageInvoke")
            {
                this.Logger.Log(LoggerType.Info, Severity.TRACE, $"{nameof(DispatchOperationAsync)}: no help page dispatcher available; TraceIdentifier: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");

                Message outgoingMessage =
                    MessageFaultHelper.CreateMessage(
                        incomingMessageEventArgs.Version,
                        MessageFaultHelper.CreateFault(
                            FaultCodes.ServerErrorCode.ServiceNotImplemented.WrapFaultCode(incomingMessageEventArgs.Version.Envelope),
                            "Help page operation not implemented."),
                        incomingMessageEventArgs.Headers.GetResponseFaultAction()).
                    TrySetTo(replyTo).
                    TrySetRelatesTo(replyRelatesTo);

                using (outgoingMessage)
                {
                    dispatchOperationTask = OutgoingMessageEventArgs.Create(incomingMessageEventArgs, outgoingMessage).GetCompletedTask(this.dispatcherRuntime);
                }
            }
            else
            {
                ServiceContainer services = new ServiceContainer(messagingContext.Services);

                services.AddService<IMessagingContext<MessagingContext>>(messagingContext);

                MessagingInteractionContext.SetCurrentMessageContext(services, incomingMessageEventArgs);

                dispatchOperationTask = this.DispatchOperationAsync(messagingContext, incomingMessageEventArgs, dispatcherRuntime, operationName, replyTo, replyRelatesTo);
            }

            TaskCompletionSource<OutgoingMessageEventArgs> outgoingMessageEventCompletion =
                new TaskCompletionSource<OutgoingMessageEventArgs>();

            dispatchOperationTask.ContinueWith((t, s) =>
            {
                OutgoingMessageEventArgs outgoingMessageEventArgs;

                outgoingMessageEventArgs = t.Result;

                outgoingMessageEventArgs.InspectOutgoingMessage();

                MessagingInteractionContext.SetCurrentMessageContext(outgoingMessageEventArgs);

                IMessagingDispatcher messageDispatcher = MessagingInteractionContext.Current.Dispatcher;

                if (messageDispatcher != null)
                {
                    messageDispatcher.OnDispatchOutgoingMessage(this, MessagingInteractionContext.Current.OutgoingMessageEventArgs);
                }

                ((TaskCompletionSource<OutgoingMessageEventArgs>)s).SetResult(MessagingInteractionContext.Current.OutgoingMessageEventArgs);
            }, 
            outgoingMessageEventCompletion);

            return outgoingMessageEventCompletion.Task;
        }

        /// <summary>
        ///  This method should not fault!
        /// Any exception should be handled and an <see cref="OutgoingMessageEventArgs"/> instance reflectng the exception prepared and returned.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <param name="incomingMessageEventArgs"></param>
        /// <param name="dispatcherRuntime"></param>
        /// <param name="operationName"></param>
        /// <param name="replyTo"></param>
        /// <param name="replyRelatesTo"></param>
        /// <returns></returns>
        private Task<OutgoingMessageEventArgs> DispatchOperationAsync(IMessagingContext<MessagingContext> messagingContext, IncomingMessageEventArgs incomingMessageEventArgs, IDispatcherRuntime dispatcherRuntime, String operationName, Uri replyTo, UniqueId replyRelatesTo)
        {
            Logger.Log(LoggerType.Info, Severity.TRACE, $"{nameof(DispatchOperationAsync)}: dispatching to {operationName}; TraceIdentifier: {incomingMessageEventArgs.TraceIdentifier}");

            Object[] inputs = dispatcherRuntime.AllocateInvokerInputs();

            Object dispatcher;

            using (Message incomingMessage = incomingMessageEventArgs.IncomingMessage.CreateMessage())
            {
                dispatcherRuntime.DeserializeRequest(incomingMessage, inputs);

                dispatcher = dispatcherRuntime.GetInstance(incomingMessage);
            }

            Dictionary<IParameterInspector, Object> correlationStates = new Dictionary<IParameterInspector, object>();

            foreach (IParameterInspector parameterInspector in dispatcherRuntime.ParameterInspectors)

                correlationStates.Add(parameterInspector, parameterInspector.BeforeCall(operationName, inputs));

            if (dispatcher is IMessagingDispatcher)
            {
                MessagingInteractionContext.Current.Dispatcher = (IMessagingDispatcher)dispatcher;

                (dispatcher as IMessagingDispatcher).OnReceivedIncomingMessage(this, incomingMessageEventArgs);
            }

            TaskCompletionSource<OutgoingMessageEventArgs> invokeDispatcherCompletion =
                new TaskCompletionSource<OutgoingMessageEventArgs>();

            dispatcherRuntime.InvokeAsync(dispatcher, inputs)
                .ContinueWith(async (dispacherTask, dispatcherState) =>
                {
                    Message outgoingMessage;

                    if (dispacherTask.IsFaulted)
                    {
                        Logger.Log(dispacherTask.Exception);

                        outgoingMessage = this.PrepareFaultMessage(incomingMessageEventArgs.Version, dispacherTask.Exception);
                    }
                    else
                    {
                        Object[] outputs = (dispacherTask.AsyncState as IList<Object>).ToArray();

                        if (dispatcherRuntime.ShouldSerializeReply)
                        {
                            foreach (IParameterInspector parameterInspector in dispatcherRuntime.ParameterInspectors)

                                parameterInspector.AfterCall(operationName, outputs, dispacherTask.Result, correlationStates[parameterInspector]);

                            outgoingMessage = dispatcherRuntime.SerializeReply(incomingMessageEventArgs.Version, outputs, dispacherTask.Result);
                        }
                        else
                        {
                            Object output = await dispatcherRuntime.GetCallbackResonseAsync(dispatcher);

                            if (!dispatcherRuntime.TryFormatCallbackMessage(output.ToEnumerable(outputs).ToArray(), out outgoingMessage))

                                outgoingMessage = new NullMessage();
                        }
                    }

                    outgoingMessage
                        .TrySetTo(replyTo)
                        .TrySetRelatesTo(replyRelatesTo);

                    using (outgoingMessage)
                    {
                        OutgoingMessageEventArgs outgoingMessageEventArgs;
                        try
                        {
                            outgoingMessageEventArgs =
                                OutgoingMessageEventArgs.Create(incomingMessageEventArgs, outgoingMessage);

                        }
                        catch (Exception e)
                        {
                            throw e;
                        }

                        ((TaskCompletionSource<OutgoingMessageEventArgs>)dispatcherState).SetResult(outgoingMessageEventArgs);
                    }
                }, invokeDispatcherCompletion);

            return invokeDispatcherCompletion.Task;
        }

        protected Message PrepareFaultMessage(MessageVersion messageVersion, Exception exception)
        {
            return Message.CreateMessage(
                messageVersion,
                this.dispatcherRuntime.SerializeFaultReply(exception.CreateFaultException(this.GetType()), out String action),
                action);
        }

        protected abstract bool TryPrepareOutgoingMessage(OutgoingMessageEventArgs outgoingMessageEventArgs, out MessageType outgoingMessage, out Exception prepareOutgoingMessageException);

        protected abstract MessageType PrepareOutgoingFaultMessage(MessageType incomingMessage, Exception exception);
    }
}
