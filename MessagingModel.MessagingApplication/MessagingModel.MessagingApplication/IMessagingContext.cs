using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Principal;
using System.Text;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public class ApplicationEndpointAddress
    {
        public class AddressHeader
        {
            XmlQualifiedName Name { get; }

            Object Value { get; }
        }

        public ApplicationEndpointAddress(Uri uri) :
            this(uri, new List<AddressHeader>(), null)
        {
        }

        public ApplicationEndpointAddress(Uri uri, IList<AddressHeader> addressHeaders) :
            this(uri, addressHeaders, null)
        {
        }

        public ApplicationEndpointAddress(Uri uri, IList<AddressHeader> addressHeaders, Claim identityClaim)
        {
            this.Uri = uri;
            this.Headers = new ReadOnlyCollection<AddressHeader>(addressHeaders);
            this.IdentityClaim = identityClaim;
        }

        public Uri Uri { get; }

        public ReadOnlyCollection<AddressHeader> Headers { get; }

        public Claim IdentityClaim { get; }
    }

    public interface IMessagingContext<MessageContext> :
        IDisposable
    {
        IServiceProvider Services { get; set; }
        IDictionary<object, object> Items { get; set; }
        IApplicationMessagingContext ApplicationContext { get; }
        Boolean CanBind { get; }
        BindingContext BindingContext { get; }
        TimeSpan ReceiveInputWaitTimeout { get; }
        MessageContext InputContext { get; }
        UniqueId CorrelationId { get; }
        UniqueId RequestId { get; }
        UniqueId RelatesTo { get; }
        ApplicationEndpointAddress OutputTo { get; }
        string SessionId { get; }
        MiddlewarePipelineResult Result { get; }
        IDictionary<MiddlewarePipelineResultHeaders, StringValues> ResultHeaders { get; }
        MessageContext OutputContext { get; }
        Exception Fault { get; }

        void AddAuthenticationChangeListener(Action<IPrincipal> onAuthenticationChange);
        void RemoveAuthenticationChangeListener(Action<IPrincipal> onAuthenticationChange);

        /// <summary>
        /// Sets input in a one-way, duplex or request-reply message exchange, 
        /// with an implicit <see cref="MiddlewarePipelineResult.NotHandled"/> result.
        /// </summary>
        /// <param name="inputContext"></param>
        /// <returns>If <see cref="ReceiveInputWaitTimeout"/> has been exceeded, method no-ops, and returns false.  Otherwise true.</returns>
        bool Input(MessageContext inputContext);
        /// <summary>
        /// Sets input (in a one-way, duplex or solicit-response message exchange), with the given <paramref name="result"/>.
        /// </summary>
        /// <param name="inputContext"></param>
        /// <param name="result"></param>
        /// <returns>If <see cref="ReceiveInputWaitTimeout"/> has been exceeded, method no-ops, and returns false.  Otherwise true.</returns>
        bool Input(MessageContext inputContext, MiddlewarePipelineResult result);
        /// <summary>
        /// Task that completes when input is set.  Cancelled if <see cref="RecieveInputWaitTimeout"/> is exceeded.
        /// </summary>
        /// <returns></returns>
        Task<IMessagingContext<MessageContext>> InputReceivedAsync();
        /// <summary>
        /// Invoked when entering middleware pipeline.
        /// </summary>
        /// <returns></returns>
        void EnteringMiddleware();
        /// <summary>
        /// Sets middleware pipeline output with an implied <see cref="MiddlewarePipelineResult.Completed"/> middleware pipeline result.
        /// </summary>
        /// <param name="outputContext"></param>
        void Output(MessageContext outputContext);
        /// <summary>
        /// Sets middleware pipeline output (in a one-way, duplex, or request-reply message exhange), with the given <paramref name="result"/>.
        /// </summary>
        /// <param name="outputContext"></param>
        /// <param name="result"></param>
        void Output(MessageContext outputContext, MiddlewarePipelineResult result);
        /// <summary>
        /// Sets middleware pipeline output (in a one-way, duplex, or request-reply message exhange), with the given <paramref name="result"/> and option <paramref name="resultHeaders"/>.
        /// </summary>
        /// <param name="outputContext"></param>
        /// <param name="resultHeaders"></param>
        /// <param name="result"></param>
        void Output(MessageContext outputContext, MiddlewarePipelineResult result, IDictionary<MiddlewarePipelineResultHeaders, StringValues> resultHeaders = null);
    }

}
