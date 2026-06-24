using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions
{
    public struct ResourceActions
    {
        [Flags]
        public enum AllowedHalfDuplexMessages
        {
            None,
            Request,
            Response
        }

        public const string OPTIONS = "OPTIONS";
        public const string HEAD = "HEAD";
        public const string DELETE = "DELETE";
        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        //public const String CONNECT = "CONNECT";
        public const string TRACE = "TRACE";
        public const string PATCH = "PATCH";

        public static AllowedHalfDuplexMessages GetAllowedHalfDuplexMessages(string resourceAction)
        {
            switch (resourceAction.ToUpper())
            {
                case OPTIONS:
                    return AllowedHalfDuplexMessages.None;
                case POST:
                    return AllowedHalfDuplexMessages.Request | AllowedHalfDuplexMessages.Response;
                case PUT:
                    return AllowedHalfDuplexMessages.Request | AllowedHalfDuplexMessages.Response;
                case TRACE:
                    return AllowedHalfDuplexMessages.Request | AllowedHalfDuplexMessages.Response;
                case PATCH:
                    return AllowedHalfDuplexMessages.Request | AllowedHalfDuplexMessages.Response;
                case GET:
                    return AllowedHalfDuplexMessages.Response;
                case HEAD:
                    return  AllowedHalfDuplexMessages.None;
                case DELETE:
                    return AllowedHalfDuplexMessages.Response;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceAction));
            }
        }

        internal static void Validate(string resourceAction, OperationDescription operationDescription)
        {
            AllowedHalfDuplexMessages halfDuplexMessages = GetAllowedHalfDuplexMessages(resourceAction);

            if (operationDescription.Messages.Count == 0)
            {
                if (!halfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.None))
                
                    throw new InvalidOperationException(AMMMFR.ServiceOperationMustSpecifyAtLeastOneMessage);
            }
            else
            {
                if (operationDescription.IsInitiating)
                {
                    MessageDescription outputMessage = null;

                    if (operationDescription.IsOneWay)
                    {
                        outputMessage = operationDescription.Messages[0];
                    }
                    else
                    {
                        MessageDescription inputMessage = operationDescription.Messages[0];

                        if (inputMessage.Direction != MessageDirection.Input)

                            throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationUnexpectedMessage, inputMessage.Direction, MessageDirection.Input));

                        if (operationDescription.IsOneWay)

                            throw new InvalidOperationException(PublicSR.ServiceOperationsMarkedWithIsOneWayTrueMust0);

                        if (!halfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.Response))
                        {
                            if (inputMessage.Body.Parts.Count > 0 || inputMessage.Body.ReturnValue != null)

                                throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationsMarkedWithResourceActionAttributeMustNotSpecifyResponseMessage, resourceAction));
                        }

                        if (operationDescription.Messages.Count > 1)

                            outputMessage = operationDescription.Messages[1];

                        else

                            throw new InvalidOperationException(AMMMFR.ServiceOperationsMarkedWithIsInitiatingMustNotSpecifyRequestMessage);
                    }

                    if (outputMessage.Direction != MessageDirection.Output)

                        throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationUnexpectedMessage, outputMessage.Direction, MessageDirection.Output));

                    if (!halfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.Request) && outputMessage.Body.Parts.Count > 0)

                        throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationsMarkedWithResourceActionAttributeMustNotSpecifyRequestMessage, resourceAction));
                }
                else
                {
                    MessageDescription inputMessage = operationDescription.Messages[0];

                    if (inputMessage.Direction != MessageDirection.Input)

                        throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationUnexpectedMessage, inputMessage.Direction, MessageDirection.Input));

                    if (!halfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.Request))
                    {
                        if (inputMessage.Body.Parts.Count > 0)

                            throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationsMarkedWithResourceActionAttributeMustNotSpecifyRequestMessage, resourceAction));
                    }

                    if (operationDescription.Messages.Count > 1)
                    {
                        if (operationDescription.IsOneWay)

                            throw new InvalidOperationException(PublicSR.ServiceOperationsMarkedWithIsOneWayTrueMust0);

                        MessageDescription outputMessage = operationDescription.Messages[1];

                        if (outputMessage.Direction != MessageDirection.Output)

                            throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationUnexpectedMessage, outputMessage.Direction, MessageDirection.Output));

                        if (!halfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.Response) &&
                            outputMessage.Body.Parts.Count > 0 || outputMessage.Body.ReturnValue != null)

                            throw new InvalidOperationException(AMMMFR.Format(AMMMFR.ServiceOperationsMarkedWithResourceActionAttributeMustNotSpecifyResponseMessage, resourceAction));
                    }
                }
            }
        }
    }
}
