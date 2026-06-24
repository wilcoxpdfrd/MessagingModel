using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public static class ProtocolMessagingContextReceiverExtensions
    {
        public static IServiceCollection AddProtocolMessagingContextReceiver<ProtocolMessagingContextReceiver, MessageContext>(this IServiceCollection services) 
            where ProtocolMessagingContextReceiver: class, IMessagingContextReceiver<MessageContext>
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IMessagingContextReceiver<MessageContext>, ProtocolMessagingContextReceiver>();

            services.TryAddSingleton<IMessagingContextAccessor<MessageContext>, MessagingContextAccessor<MessageContext>>();

            return services;
        }

        public static IServiceCollection AddProtocolMessagingContextReceiver<ProtocolMessagingContextReceiver, ProtocolContext, MessageContext>(this IServiceCollection services)
            where ProtocolMessagingContextReceiver : class, IMessagingContextReceiver<ProtocolContext, MessageContext>
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IMessagingContextReceiver<ProtocolContext, MessageContext>, ProtocolMessagingContextReceiver>();

            services.TryAddSingleton<IMessagingContextAccessor<MessageContext>, MessagingContextAccessor<MessageContext>>();

            return services;
        }

        public static IServiceCollection AddProtocolMessagingContextReceiver<MessageContext>(this IServiceCollection services, Type protocolMessagingContextReceiverType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(typeof(IMessagingContextReceiver<MessageContext>), protocolMessagingContextReceiverType);

            services.TryAddSingleton<IMessagingContextAccessor<MessageContext>, MessagingContextAccessor<MessageContext>>();

            return services;
        }

        public static IServiceCollection AddProtocolMessagingContextReceiver<ProtocolContext, MessageContext>(this IServiceCollection services, Type protocolMessagingContextReceiverType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(typeof(IMessagingContextReceiver<ProtocolContext, MessageContext>), protocolMessagingContextReceiverType);

            services.TryAddSingleton<IMessagingContextAccessor<MessageContext>, MessagingContextAccessor<MessageContext>>();

            return services;
        }

        public static IServiceCollection AddProtocolMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext>(this IServiceCollection services, Type protocolMessagingContextReceiverType)
            where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(typeof(IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext>), protocolMessagingContextReceiverType);

            services.TryAddSingleton<IMessagingContextAccessor<MessageContext>, MessagingContextAccessor<MessageContext>>();

            return services;
        }
    }
}
