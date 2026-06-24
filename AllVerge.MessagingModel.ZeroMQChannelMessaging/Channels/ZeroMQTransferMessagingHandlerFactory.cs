using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Runtime;

using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.Core.ServiceModel.Transfer;
    using AllVerge.Core.ServiceModel.Transfer.Configuration;

    using System.ServiceModel;

    internal class ZeroMQTransferMessagingHandlerFactory 
    {
        static readonly Type delegateHandlerType = typeof(TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>);

        Type[] messageHandlers;
        ConstructorInfo[] delegatehandlerCtors;
        Func<IEnumerable<TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>>> delegateHandlersFactory;

        public ZeroMQTransferMessagingHandlerFactory(params Type[] delegatehandlerTypes)
        {
            if (delegatehandlerTypes == null)
            {
                throw FxTrace.Exception.ArgumentNull("handlers");
            }

            if (delegatehandlerTypes.Length == 0)
            {
                throw FxTrace.Exception.Argument("handlers", SR.InputTypeListEmptyError);
            }

            this.delegatehandlerCtors = new ConstructorInfo[delegatehandlerTypes.Length];
            for (int cnt = 0; cnt < delegatehandlerTypes.Length; cnt++)
            {
                Type delegatehandlerType = delegatehandlerTypes[cnt];
                if (delegatehandlerType == null)
                {
                    throw FxTrace.Exception.Argument(
                        string.Format(CultureInfo.InvariantCulture, $"{nameof(delegatehandlerTypes)}[<<{0}>>]", cnt),
                        SR.Format(SR.HttpMessageHandlerTypeNotSupported, "null", delegateHandlerType.Name));
                }

                if (!delegateHandlerType.IsAssignableFrom(delegatehandlerType) || delegatehandlerType.IsAbstract)
                {
                    throw FxTrace.Exception.Argument(
                        string.Format(CultureInfo.InvariantCulture, $"{nameof(delegatehandlerTypes)}[<<{0}>>]", cnt),
                        SR.Format(SR.HttpMessageHandlerTypeNotSupported, delegatehandlerType.Name, delegateHandlerType.Name));
                }

                ConstructorInfo ctorInfo = delegatehandlerType.GetConstructor(Type.EmptyTypes);
                if (ctorInfo == null)
                {
                    throw FxTrace.Exception.Argument(
                        string.Format(CultureInfo.InvariantCulture, $"{nameof(delegatehandlerTypes)}[<<{0}>>]", cnt),
                        SR.Format(SR.HttpMessageHandlerTypeNotSupported, delegatehandlerType.Name, delegateHandlerType.Name));
                }

                this.delegatehandlerCtors[cnt] = ctorInfo;
            }

            this.messageHandlers = delegatehandlerTypes;
        }

        public ZeroMQTransferMessagingHandlerFactory(Func<IEnumerable<TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>>> delegateHandlersFactory)
        {
            if (delegateHandlersFactory == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(delegateHandlersFactory));
            }

            this.delegateHandlersFactory = delegateHandlersFactory;
        }

        protected ZeroMQTransferMessagingHandlerFactory()
        {
        }

        internal TransferMessagingHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> Create(TransferMessagingHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> innerChannel)
        {
            if (innerChannel == null)
            {
                throw FxTrace.Exception.ArgumentNull("innerChannel");
            }

            return this.OnCreate(innerChannel);
        }

        internal static ZeroMQTransferMessagingHandlerFactory CreateFromConfigurationElement(TransferMessagingHandlerFactoryElement configElement)
        {
            Fx.Assert(configElement != null, "configElement should not be null.");

            if (!string.IsNullOrWhiteSpace(configElement.Type))
            {
                if (configElement.Handlers != null && configElement.Handlers.Count > 0)
                {
                    throw FxTrace.Exception.AsError(new ConfigurationErrorsException(SR.Format(SR.HttpMessageHandlerFactoryConfigInvalid_WithBothTypeAndHandlerList, ConfigurationStrings.MessageHandlerFactory, ConfigurationStrings.Type, ConfigurationStrings.Handlers)));
                }

                Type factoryType = TypeUtilities.GetTypeFromAssembliesInCurrentDomain(configElement.Type);
                if (factoryType == null)
                {
                    throw FxTrace.Exception.AsError(new ConfigurationErrorsException(SR.Format(SR.CanNotLoadTypeGotFromConfig, configElement.Type)));
                }

                if (!typeof(ZeroMQTransferMessagingHandlerFactory).IsAssignableFrom(factoryType) || factoryType.IsAbstract)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ConfigurationErrorsException(
                            SR.Format(
                                SR.WebSocketElementConfigInvalidHttpMessageHandlerFactoryType,
                                typeof(ZeroMQTransferMessagingHandlerFactory).Name,
                                factoryType,
                                typeof(ZeroMQTransferMessagingHandlerFactory).AssemblyQualifiedName
                            )
                        )
                    );
                }

                return Activator.CreateInstance(factoryType) as ZeroMQTransferMessagingHandlerFactory;
            }
            else
            {
                if (configElement.Handlers == null || configElement.Handlers.Count == 0)
                {
                    return null;
                }

                Type[] handlerTypes = new Type[configElement.Handlers.Count];
                for (int i = 0; i < configElement.Handlers.Count; i++)
                {
                    Type handlerType = TypeUtilities.GetTypeFromAssembliesInCurrentDomain(configElement.Handlers[i].Type);
                    if (handlerType == null)
                    {
                        throw FxTrace.Exception.AsError(new ConfigurationErrorsException(SR.Format(SR.CanNotLoadTypeGotFromConfig, configElement.Handlers[i].Type)));
                    }

                    handlerTypes[i] = handlerType;
                }

                try
                {
                    return new ZeroMQTransferMessagingHandlerFactory(handlerTypes);
                }
                catch (ArgumentException ex)
                {
                    throw FxTrace.Exception.AsError(new ConfigurationErrorsException(ex.Message, ex));
                }
            }
        }

        private TransferMessagingHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> OnCreate(TransferMessagingHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> innerChannel)
        {
            if (innerChannel == null)
            {
                throw FxTrace.Exception.ArgumentNull("innerChannel");
            }

            // Get handlers either by constructing types or by calling Func
            IEnumerable<TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>> handlerInstances = null;
            try
            {
                if (delegateHandlersFactory != null)
                {
                    handlerInstances = delegateHandlersFactory.Invoke();
                    if (handlerInstances != null)
                    {
                        foreach (TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> handler in handlerInstances)
                        {
                            if (handler == null)
                            {
                                throw FxTrace.Exception.Argument("handlers", SSR.Format(SSR.DelegatingHandlerArrayFromFuncContainsNullItem, delegateHandlerType.Name, GetFuncDetails(delegateHandlersFactory)));
                            }
                        }
                    }
                }
                else if (delegatehandlerCtors != null)
                {
                    TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>[] instances = new TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>[delegatehandlerCtors.Length];
                    for (int cnt = 0; cnt < delegatehandlerCtors.Length; cnt++)
                    {
                        instances[cnt] = (TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>)delegatehandlerCtors[cnt].Invoke(Array.Empty<Type>());
                    }

                    handlerInstances = instances;
                }
            }
            catch (TargetInvocationException targetInvocationException)
            {
                throw FxTrace.Exception.AsError(targetInvocationException);
            }

            // Wire handlers up
            TransferMessagingHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> pipeline = innerChannel;
            if (handlerInstances != null)
            {
                foreach (TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> handlerInstance in handlerInstances)
                {
                    if (handlerInstance.InnerHandler != null)
                    {
                        throw FxTrace.Exception.Argument("handlers", SSR.Format(SSR.DelegatingHandlerArrayHasNonNullInnerHandler, delegateHandlerType.Name, "InnerHandler", handlerInstance.GetType().Name));
                    }

                    handlerInstance.InnerHandler = pipeline;
                    pipeline = handlerInstance;
                }
            }

            return pipeline;
        }

        private static string GetFuncDetails(Func<IEnumerable<TransferMessagingDelegateHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>>> func)
        {
            Fx.Assert(func != null, "Func should not be null.");
            MethodInfo m = func.Method;
            Type t = m.DeclaringType;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", t.FullName, m.Name);
        }
    }
}