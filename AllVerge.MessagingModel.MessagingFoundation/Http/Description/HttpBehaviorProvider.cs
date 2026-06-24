using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace AllVerge.MessagingModel.MessagingFoundation.Http.Description
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    public class HttpBehaviorProvider : IWebHttpBehaviorProvider
    {
        public HttpBehaviorProvider() : base() { }

        public bool TryGetRequestFormat(OperationDescription od, out WebMessageFormat? requestMessageFormat)
        {
            ResourceActionTemplateAttribute actionAttribute = od.Behaviors.Find<ResourceActionTemplateAttribute>();
            MessageActionAttribute messageActionAttribute = od.Behaviors.Find<MessageActionAttribute>();

            ResourceMediaType mediaType;

            if (actionAttribute != null)
            {
                if (actionAttribute.IsMediaTypeSetExplicitly)

                    mediaType = actionAttribute.MediaType;

                else

                    mediaType = ResourceMediaType.Any;
            }
            else if (messageActionAttribute != null)
            {
                if (messageActionAttribute.IsMediaTypeSetExplicitly)

                    mediaType = messageActionAttribute.MediaType;

                else
                
                    mediaType = ResourceMediaType.Any;

            }
            else

                mediaType = ResourceMediaType.None;

            switch (mediaType)
            {
                case ResourceMediaType.Json:

                    requestMessageFormat =  WebMessageFormat.Json;

                    return true;

                case ResourceMediaType.Xml:

                    requestMessageFormat = WebMessageFormat.Xml;

                    return true;

                case ResourceMediaType.Any:

                    requestMessageFormat = null;

                    return true;

                case ResourceMediaType.None:
                default:

                    requestMessageFormat = null;

                    return false;
            }
        }

        public bool TryGetResponseFormat(OperationDescription od, out WebMessageFormat? responseMessageFormat)
        {
            ResourceActionTemplateAttribute actionAttribute = od.Behaviors.Find<ResourceActionTemplateAttribute>();
            MessageActionAttribute messageAttribute = od.Behaviors.Find<MessageActionAttribute>();

            ResourceMediaType mediaType;

            if (actionAttribute != null)
            {
                if (actionAttribute.IsMediaTypeSetExplicitly)

                    mediaType = actionAttribute.MediaType;

                else

                    mediaType = ResourceMediaType.Any;
            }
            else if (messageAttribute != null)
            {
                if (messageAttribute.IsMediaTypeSetExplicitly)

                    mediaType = messageAttribute.MediaType;

                else

                    mediaType = ResourceMediaType.Any;
            }
            else

                mediaType = ResourceMediaType.None;

            switch (mediaType)
            {
                case ResourceMediaType.Json:

                    responseMessageFormat = WebMessageFormat.Json;

                    return true;

                case ResourceMediaType.Xml:

                    responseMessageFormat = WebMessageFormat.Xml;

                    return true;

                case ResourceMediaType.Any:

                    responseMessageFormat = null;

                    return true;

                case ResourceMediaType.None:
                default:

                    responseMessageFormat = null;

                    return false;
            }
        }

        public bool TryGetBodyStyle(OperationDescription od, out WebMessageBodyStyle? bodyStyle)
        {
            ResourceActionTemplateAttribute actionAttribute = od.Behaviors.Find<ResourceActionTemplateAttribute>();
            MessageActionAttribute messageAttribute = od.Behaviors.Find<MessageActionAttribute>();

            if (actionAttribute != null)
            {
                switch (actionAttribute.ActionStyle)
                {
                    case ResourceActionStyle.Bare:
                        bodyStyle = WebMessageBodyStyle.Bare;
                        return true;
                    case ResourceActionStyle.Wrapped:
                        bodyStyle = WebMessageBodyStyle.Wrapped;
                        return true;
                    case ResourceActionStyle.WrappedRequest:
                        bodyStyle = WebMessageBodyStyle.WrappedRequest;
                        return true;
                    case ResourceActionStyle.WrappedResponse:
                        bodyStyle = WebMessageBodyStyle.WrappedResponse;
                        return true;
                }
            }
            else if (messageAttribute != null)
            {
                bool isWrappedRequest = messageAttribute.GetIsWrapRequest(out _, out _, out _);
                bool isWrappedResponse = messageAttribute.GetIsWrapResponse(out _, out _, out _);

                if (isWrappedRequest && isWrappedResponse)
                    bodyStyle = WebMessageBodyStyle.Wrapped;
                else if (isWrappedRequest)
                    bodyStyle = WebMessageBodyStyle.WrappedRequest;
                else if (isWrappedResponse)
                    bodyStyle = WebMessageBodyStyle.WrappedResponse;
                else
                    bodyStyle = WebMessageBodyStyle.Bare;

                return true;
            }

            bodyStyle = null;

            return false;
        }

        public bool TryGetWebMethod(OperationDescription od, out string webMethod)
        {
            ResourceActionAttribute methodAttribute = od.Behaviors.Find<ResourceActionAttribute>();

            if (methodAttribute != null)
            {
                webMethod = methodAttribute.ResourceAction;

                return true;
            }

            webMethod = null;

            return false;
        }

        public bool TryGetWebUriTemplate(OperationDescription od, out string webUriTemplate)
        {
            ResourceActionTemplateAttribute templateAttribute = od.Behaviors.Find<ResourceActionTemplateAttribute>();

            if (templateAttribute != null)
            {
                webUriTemplate = templateAttribute.Template;

                return true;
            }

            webUriTemplate = null;

            return false;
        }
    }
}
