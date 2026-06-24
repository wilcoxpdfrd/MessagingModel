using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    public static class ResourceEndpointAttributeExtensions
    {
        public static bool GetIsDuplexResponse(this ResourceActionAttribute resourceActionAttribute, out Type callbackResourceContractType)
        {
            if (resourceActionAttribute is PostMessageActionAttribute)
            {
                PostMessageActionAttribute resourceActionMethodAttribute = (PostMessageActionAttribute)resourceActionAttribute;

                callbackResourceContractType = resourceActionMethodAttribute.CallbackContractType;
            }
            else

                callbackResourceContractType = null;

            return callbackResourceContractType != null;
        }

        public static MessageContractAttribute GetRequestContractAttribute(this ResourceEndpointAttribute resourceEndpointAttribute, String wrapperName, String wrapperNamespace)
        {
            if (resourceEndpointAttribute.GetIsWrapRequest(out bool? isEnvelopeVersionNone, out string actionName, out ProtectionLevel protectionLevel))
            
                return new MessageContractAttribute() { IsWrapped = true, WrapperName = actionName ?? wrapperName, WrapperNamespace = wrapperNamespace, ProtectionLevel = protectionLevel };

            return new MessageContractAttribute();
        }

        public static MessageContractAttribute GetResponseContractAttribute(this ResourceEndpointAttribute resourceEndpointAttribute, String wrapperName, String wrapperNamespace)
        {
            if (resourceEndpointAttribute.GetIsWrapResponse(out bool? isEnvelopeVersionNone, out string replyActionName, out ProtectionLevel protectionLevel))

                return new MessageContractAttribute() { IsWrapped = true, WrapperName = replyActionName ?? wrapperName, WrapperNamespace = wrapperNamespace, ProtectionLevel = protectionLevel };

            return new MessageContractAttribute();
        }

        public static bool GetIsWrapRequest(this ResourceEndpointAttribute resourceEndpointAttribute, out bool? isEnvelopeVersionNone, out String actionName, out ProtectionLevel protectionLevel)
        {
            if (resourceEndpointAttribute is ResourceActionTemplateAttribute)
            {
                isEnvelopeVersionNone = true;

                actionName = null;
                
                protectionLevel = ProtectionLevel.None;

                ResourceActionTemplateAttribute resourceTemplateMethodAttribute = (ResourceActionTemplateAttribute)resourceEndpointAttribute;

                if (resourceTemplateMethodAttribute.IsActionStyleSetExplicitly)
                {
                    switch (resourceTemplateMethodAttribute.ActionStyle)
                    {
                        case ResourceActionStyle.Wrapped:
                        case ResourceActionStyle.WrappedRequest:

                            return true;
                    }
                }
            }
            else if (resourceEndpointAttribute is MessageActionAttribute)
            {
                isEnvelopeVersionNone = false;

                MessageActionAttribute resourceActionMethodAttribute = (MessageActionAttribute)resourceEndpointAttribute;

                actionName = resourceActionMethodAttribute.Action;

                protectionLevel = resourceActionMethodAttribute.ProtectionLevel;

                return true;
            }
            else

                isEnvelopeVersionNone = null;

            actionName = null;
            
            protectionLevel = ProtectionLevel.None;

            return false;
        }

        public static bool GetIsWrapResponse(this ResourceEndpointAttribute resourceEndpointAttribute, out bool? isEnvelopeVersionNone, out String replyActionName, out ProtectionLevel protectionLevel)
        {
            if (resourceEndpointAttribute is ResourceActionTemplateAttribute)
            {
                isEnvelopeVersionNone = true;
                replyActionName = null;
                protectionLevel = ProtectionLevel.None;

                ResourceActionTemplateAttribute resourceTemplateMethodAttribute = (ResourceActionTemplateAttribute)resourceEndpointAttribute;

                if (resourceTemplateMethodAttribute.IsActionStyleSetExplicitly)
                {
                    switch (resourceTemplateMethodAttribute.ActionStyle)
                    {
                        case ResourceActionStyle.Wrapped:
                        case ResourceActionStyle.WrappedResponse:

                            return true;
                    }
                }

                return false;
            }
            else if (resourceEndpointAttribute is MessageActionAttribute)
            {
                isEnvelopeVersionNone = false;

                MessageActionAttribute resourceActionMethodAttribute = (MessageActionAttribute)resourceEndpointAttribute;
                
                bool isDuplexResponse = resourceActionMethodAttribute.GetIsDuplexResponse(out _);

                if (isDuplexResponse)
                {
                    replyActionName = resourceActionMethodAttribute.Name;

                    protectionLevel = resourceActionMethodAttribute.ProtectionLevel;

                    return true;
                }
                else
                {
                    // default is responses for Soap messages are wrapped if not explicitely disabled ...

                    replyActionName = resourceActionMethodAttribute.ReplyAction;

                    protectionLevel = resourceActionMethodAttribute.ProtectionLevel;

                    return true;
                }
            }
            else

                isEnvelopeVersionNone = null;

            replyActionName = null;

            protectionLevel =  ProtectionLevel.None;

            return false;
        }
    }
}
