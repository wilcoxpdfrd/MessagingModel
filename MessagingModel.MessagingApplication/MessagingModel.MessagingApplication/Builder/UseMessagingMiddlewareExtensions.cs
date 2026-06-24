using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Builder
{
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;

    public static class UseMessagingMiddlewareExtensions
    {
        internal const string InvokeMethodName = "Invoke";

        internal const string InvokeAsyncMethodName = "InvokeAsync";

        private static readonly MethodInfo GetServiceInfo = typeof(UseMessagingMiddlewareExtensions).GetMethod("GetService", BindingFlags.Static | BindingFlags.NonPublic);

        public static IMessagingApplicationBuilder<MessageContext> UseMessagingApplication<TMiddleware, MessageContext>(this IMessagingApplicationBuilder<MessageContext> app, params object[] args)
        {
            return app.UseMessagingApplication<MessageContext>(typeof(TMiddleware), args);
        }

        public static IMessagingApplicationBuilder<ProtocolContext, MessageContext> UseMessagingApplication<TMiddleware, ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, params object[] args)
        {
            return app.UseMessagingApplication<ProtocolContext, MessageContext>(typeof(TMiddleware), args);
        }

        public static IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> UseMessagingApplication<TMiddleware, ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> app, params object[] args)
        {
            return app.UseMessagingApplication<ProtocolContext, MessageContext>(typeof(TMiddleware), args);
        }        
            
        public static IMessagingApplicationBuilder<MessageContext> UseMessagingApplication<MessageContext>(this IMessagingApplicationBuilder<MessageContext> app, Type middleware, params object[] args)
        {
            if (typeof(IMessagingMiddleware<MessageContext>).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                if (args.Length != 0)
                {
                    throw new NotSupportedException(Resources.FormatException_UseMiddlewareExplicitArgumentsNotSupported(typeof(IMessagingMiddleware<MessageContext>)));
                }
                return UseMessagingApplicationMiddlewareType<MessageContext>(app, middleware);
            }
            else if (typeof(IMiddleware).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatException_UseMessageMiddlewareMustImplementInterface(typeof(IMessagingMiddleware<MessageContext>).GetTypeInfo(), middleware.GetTypeInfo(), typeof(IMiddleware).GetTypeInfo()));
            }
            IServiceProvider applicationServices = app.ApplicationServices;
            return app.Use((MessagingContextMiddlewareDelegate<MessageContext> next) =>
            {
                MethodInfo[] methodInfos = (from m in middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                      where string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal) || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
                                      select m).ToArray();
                if (methodInfos.Length > 1)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddleMutlipleInvokes(InvokeMethodName, InvokeAsyncMethodName));
                }
                if (methodInfos.Length == 0)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoInvokeMethod(InvokeMethodName, InvokeAsyncMethodName, middleware));
                }
                MethodInfo invokeMethodInfo = methodInfos[0];
                if (!typeof(Task).IsAssignableFrom(invokeMethodInfo.ReturnType))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNonTaskReturnType(InvokeMethodName, InvokeAsyncMethodName, nameof(Task)));
                }
                ParameterInfo[] parameters = invokeMethodInfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(IMessagingContext<MessageContext>))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoParameters(InvokeMethodName, InvokeAsyncMethodName, nameof(IMessagingContext<MessageContext>)));
                }
                object[] argsArray = new object[args.Length + 1];
                argsArray[0] = next;
                Array.Copy(args, 0, argsArray, 1, args.Length);
                object instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, argsArray);
                if (parameters.Length == 1)
                {
                    return (MessagingContextMiddlewareDelegate<MessageContext>)invokeMethodInfo.CreateDelegate(typeof(MessagingContextMiddlewareDelegate<MessageContext>), instance);
                }
                Func<object, IMessagingContext<MessageContext>, IServiceProvider, Task> factory = Compile<object, MessageContext>(invokeMethodInfo, parameters);
                return (IMessagingContext<MessageContext> context) =>
                {
                    if (context.Services == null)

                        context.Services = applicationServices;

                    if (context.Services == null)
                    {
                        throw new InvalidOperationException(Resources.FormatException_UseMiddlewareIServiceProviderNotAvailable(nameof(IServiceProvider)));
                    }
                    return factory(instance, context, context.Services);
                };
            });
        }

        public static IMessagingApplicationBuilder<ProtocolContext, MessageContext> UseMessagingApplication<ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, Type middleware, params object[] args)
        {
            if (typeof(IMessagingMiddleware<MessageContext>).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                if (args.Length != 0)
                {
                    throw new NotSupportedException(Resources.FormatException_UseMiddlewareExplicitArgumentsNotSupported(typeof(IMessagingMiddleware<MessageContext>)));
                }
                return UseMessagingApplicationMiddlewareType<ProtocolContext, MessageContext>(app, middleware);
            }
            else if (typeof(IMiddleware).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatException_UseMessageMiddlewareMustImplementInterface(typeof(IMessagingMiddleware<MessageContext>).GetTypeInfo(), middleware.GetTypeInfo(), typeof(IMiddleware).GetTypeInfo()));
            }
            IServiceProvider applicationServices = app.ApplicationServices;
            return app.Use((MessagingContextMiddlewareDelegate<MessageContext> next) =>
            {
                MethodInfo[] array = (from m in middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                      where string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal) || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
                                      select m).ToArray();
                if (array.Length > 1)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddleMutlipleInvokes(InvokeMethodName, InvokeAsyncMethodName));
                }
                if (array.Length == 0)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoInvokeMethod(InvokeMethodName, InvokeAsyncMethodName, middleware));
                }
                MethodInfo methodInfo = array[0];
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNonTaskReturnType(InvokeMethodName, InvokeAsyncMethodName, nameof(Task)));
                }
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(IMessagingContext<MessageContext>))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoParameters(InvokeMethodName, InvokeAsyncMethodName, nameof(IMessagingContext<MessageContext>)));
                }
                object[] array2 = new object[args.Length + 1];
                array2[0] = next;
                Array.Copy(args, 0, array2, 1, args.Length);
                object instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, array2);
                if (parameters.Length == 1)
                {
                    return (MessagingContextMiddlewareDelegate<MessageContext>)methodInfo.CreateDelegate(typeof(MessagingContextMiddlewareDelegate<MessageContext>), instance);
                }
                Func<object, IMessagingContext<MessageContext>, IServiceProvider, Task> factory = Compile<object, MessageContext>(methodInfo, parameters);
                return (IMessagingContext<MessageContext> context) =>
                {
                    if (context.Services == null)

                        context.Services = applicationServices;

                    if (context.Services == null)
                    {
                        throw new InvalidOperationException(Resources.FormatException_UseMiddlewareIServiceProviderNotAvailable(nameof(IServiceProvider)));
                    }
                    return factory(instance, context, context.Services);
                };
            });
        }

        public static IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> UseMessagingApplication<ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> app, Type middleware, params object[] args)
        {
            if (typeof(IMessagingMiddleware<MessageContext>).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                if (args.Length != 0)
                {
                    throw new NotSupportedException(Resources.FormatException_UseMiddlewareExplicitArgumentsNotSupported(typeof(IMessagingMiddleware<MessageContext>)));
                }
                return UseMessagingApplicationMiddlewareType<ProtocolContext, MessageContext>(app, middleware);
            }
            else if (typeof(IMiddleware).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatException_UseMessageMiddlewareMustImplementInterface(typeof(IMessagingMiddleware<MessageContext>).GetTypeInfo(), middleware.GetTypeInfo(), typeof(IMiddleware).GetTypeInfo()));
            }
            IServiceProvider applicationServices = app.ApplicationServices;
            return app.Use((MessagingContextMiddlewareDelegate<MessageContext> next) =>
            {
                MethodInfo[] array = (from m in middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                      where string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal) || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
                                      select m).ToArray();
                if (array.Length > 1)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddleMutlipleInvokes(InvokeMethodName, InvokeAsyncMethodName));
                }
                if (array.Length == 0)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoInvokeMethod(InvokeMethodName, InvokeAsyncMethodName, middleware));
                }
                MethodInfo methodInfo = array[0];
                if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNonTaskReturnType(InvokeMethodName, InvokeAsyncMethodName, nameof(Task)));
                }
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(IMessagingContext<MessageContext>))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoParameters(InvokeMethodName, InvokeAsyncMethodName, nameof(IMessagingContext<MessageContext>)));
                }
                object[] array2 = new object[args.Length + 1];
                array2[0] = next;
                Array.Copy(args, 0, array2, 1, args.Length);
                object instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, array2);
                if (parameters.Length == 1)
                {
                    return (MessagingContextMiddlewareDelegate<MessageContext>)methodInfo.CreateDelegate(typeof(MessagingContextMiddlewareDelegate<MessageContext>), instance);
                }
                Func<object, IMessagingContext<MessageContext>, IServiceProvider, Task> factory = Compile<object, MessageContext>(methodInfo, parameters);
                return (IMessagingContext<MessageContext> context) =>
                {
                    if (context.Services == null)

                        context.Services = applicationServices;

                    if (context.Services == null)
                    {
                        throw new InvalidOperationException(Resources.FormatException_UseMiddlewareIServiceProviderNotAvailable(nameof(IServiceProvider)));
                    }
                    return factory(instance, context, context.Services);
                };
            });
        }
        
        private static IMessagingApplicationBuilder<MessageContext> UseMessagingApplicationMiddlewareType<MessageContext>(IMessagingApplicationBuilder<MessageContext> app, Type middlewareType)
        {
            return app.Use((MessagingContextMiddlewareDelegate<MessageContext> next) => async delegate (IMessagingContext<MessageContext> context)
            {
                IMessagingMiddlewareFactory<MessageContext> middlewareFactory = (IMessagingMiddlewareFactory<MessageContext>)context.Services.GetService<IMiddlewareFactory>();
                if (middlewareFactory == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMessagingMiddlewareFactory<MessageContext>)));
                }
                IMessagingMiddleware<MessageContext> middleware = middlewareFactory.Create(middlewareType);
                if (middleware == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(middlewareFactory.GetType(), middlewareType));
                }
                try
                {
                    await middleware.InvokeAsync(context, next);
                }
                finally
                {
                    middlewareFactory.Release(middleware);
                }
            });
        }

        private static IMessagingApplicationBuilder<ProtocolContext, MessageContext> UseMessagingApplicationMiddlewareType<ProtocolContext, MessageContext>(IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, Type middlewareType)
        {
            return app.Use((MessagingContextMiddlewareDelegate<MessageContext> next) => async delegate (IMessagingContext<MessageContext> context)
            {
                IMessagingMiddlewareFactory<MessageContext> middlewareFactory = (IMessagingMiddlewareFactory<MessageContext>)context.Services.GetService<IMiddlewareFactory>();
                if (middlewareFactory == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMessagingMiddlewareFactory<MessageContext>)));
                }
                IMessagingMiddleware<MessageContext> middleware = middlewareFactory.Create(middlewareType);
                if (middleware == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(middlewareFactory.GetType(), middlewareType));
                }
                try
                {
                    await middleware.InvokeAsync(context, next);
                }
                finally
                {
                    middlewareFactory.Release(middleware);
                }
            });
        }

        private static IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> UseMessagingApplicationMiddlewareType<ProtocolContext, MessageContext>(IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> app, Type middlewareType)
        {
            return app.Use((MessagingContextMiddlewareDelegate<MessageContext> next) => async delegate (IMessagingContext<MessageContext> context)
            {
                IMessagingMiddlewareFactory<MessageContext> middlewareFactory = (IMessagingMiddlewareFactory<MessageContext>)context.Services.GetService<IMiddlewareFactory>();
                if (middlewareFactory == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMessagingMiddlewareFactory<MessageContext>)));
                }
                IMessagingMiddleware<MessageContext> middleware = middlewareFactory.Create(middlewareType);
                if (middleware == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(middlewareFactory.GetType(), middlewareType));
                }
                try
                {
                    await middleware.InvokeAsync(context, next);
                }
                finally
                {
                    middlewareFactory.Release(middleware);
                }
            });
        }

        private static Func<T, IMessagingContext<MessageContext>, IServiceProvider, Task> Compile<T, MessageContext>(MethodInfo methodinfo, ParameterInfo[] parameters)
        {
            Type typeFromHandle = typeof(T);
            ParameterExpression parameterExpression = Expression.Parameter(typeof(IMessagingContext<MessageContext>), "messageHandlerContext<MessageType>");
            ParameterExpression parameterExpression2 = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
            ParameterExpression parameterExpression3 = Expression.Parameter(typeFromHandle, "middleware");
            Expression[] array = new Expression[parameters.Length];
            array[0] = parameterExpression;
            for (int i = 1; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    throw new NotSupportedException(Resources.FormatException_InvokeDoesNotSupportRefOrOutParams(InvokeMethodName));
                }
                Expression[] arguments = new Expression[3]
                {
                parameterExpression2,
                Expression.Constant(parameterType, typeof(Type)),
                Expression.Constant(methodinfo.DeclaringType, typeof(Type))
                };
                MethodCallExpression expression = Expression.Call(GetServiceInfo, arguments);
                array[i] = Expression.Convert(expression, parameterType);
            }
            Expression expression2 = parameterExpression3;
            if (methodinfo.DeclaringType != typeof(T))
            {
                expression2 = Expression.Convert(expression2, methodinfo.DeclaringType);
            }
            return Expression.Lambda<Func<T, IMessagingContext<MessageContext>, IServiceProvider, Task>>(Expression.Call(expression2, methodinfo, array), new ParameterExpression[3]
            {
                parameterExpression3,
                parameterExpression,
                parameterExpression2
            }).Compile();
        }

        private static object GetService(IServiceProvider sp, Type type, Type middleware)
        {
            return sp.GetService(type) ?? throw new InvalidOperationException(Resources.FormatException_InvokeMiddlewareNoService(type, middleware));
        }
    }

}
