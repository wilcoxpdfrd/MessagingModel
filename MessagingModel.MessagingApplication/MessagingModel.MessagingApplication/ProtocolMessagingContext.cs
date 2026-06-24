using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public class ProtocolMessagingContext
    {
        static AsyncLocal<Object> _asyncLocal = new AsyncLocal<Object>();
        static Dictionary<Tuple<Type, Type>, Object> mappers = new Dictionary<Tuple<Type, Type>, object>();
        static Tuple<Type, Type> Couple(params Type[] types) => new Tuple<Type, Type>(types[0], types[1]);

        public static void RegisterMapper<TContext, ToContext>(Func<TContext, ToContext> mapper) => ProtocolMessagingContext.mappers.Add(Couple(mapper.GetType().GenericTypeArguments), mapper);

        private static MethodInfo mapMethodInfo = typeof(ProtocolMessagingContext).GetMethod("Map", BindingFlags.Static | BindingFlags.Public);

        private static MethodInfo BindMap<ToContext>(Type contextType) => ProtocolMessagingContext.mapMethodInfo.MakeGenericMethod(contextType, typeof(ToContext));

        private static Object[] GetParams(params Object[] args) => args;

        public static Object Current { get => ProtocolMessagingContext._asyncLocal.Value; set => ProtocolMessagingContext._asyncLocal.Value = value; }

        public static ToContext GetCurrent<ToContext>() where ToContext : class
        {
            Object current = ProtocolMessagingContext.Current;

            return (ToContext)ProtocolMessagingContext.BindMap<ToContext>(current.GetType()).Invoke(null, GetParams(current));
        }

        public static ToContext Map<TContext, ToContext>(TContext context) where ToContext : class
        { 
            if (mappers.TryGetValue(Couple(typeof(TContext), typeof(ToContext)), out Object obj)) 

                return (obj as Func<TContext, ToContext>)(context);

            return context as ToContext;
        }
    }

    public class ProtocolMessagingContext<TContext>
    {
        public static TContext Current
        { 
            get 
            {
                object current = ProtocolMessagingContext.Current;
                if (current is TContext)
                    return (TContext)current;
                return default(TContext);
            }
            internal set => ProtocolMessagingContext.Current = value; }
    }
}
