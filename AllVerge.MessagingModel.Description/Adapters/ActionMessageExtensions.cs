using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    using AllVerge.MessagingModel.Description.Model;
    using AllVerge.DataModel.Primitives;
    using AllVerge.DataModel.Primitives.LexicalTypes;

    public static class ActionMessageExtensions
    {
        public static void GetRequestMessagePotentials(this InteractionMessage message, out IEnumerable<Potential> headerPotentials, out IEnumerable<Potential> pathPotentials, out IEnumerable<Potential> queryPotentials, out IEnumerable<Potential> formPotentials, out IEnumerable<Potential> bodyPotentials)
        {
            // keep track of bound agents as you go - remaining unbound (soap/mime) agents will bind to body

            List<Potential> boundPotentials = new List<Potential>();

            FilterAgents(
                message,
                BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out headerPotentials);

            FilterAgents(
                message,
                BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out pathPotentials);

            FilterAgents(
                message,
                BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out queryPotentials);

            String[] mimeContentTypes;

            if (message.TryGetMimeContentBindings(out mimeContentTypes))
            {
                if (mimeContentTypes.Any(m => m == "application/x-www-form-urlencoded" || m == "multipart/form-data"))
                {
                    formPotentials = message.Domain.GetPotentials<Potential>(false).Where(p => !boundPotentials.Contains(p)).ToArray();
                    bodyPotentials = Enumerable.Empty<Potential>();
                }
                else // application/json, application/text, ... e.g. body
                {
                    formPotentials = Enumerable.Empty<Potential>();
                    bodyPotentials = message.Domain.GetPotentials<Potential>().Where(p => !boundPotentials.Contains(p)).ToArray();
                }
            }
            else
            {
                BindingProperty bindingProperty;

                if (message.TryGetSoapBodyBinding(out bindingProperty))
                {
                    //Todo:  refer to "parts" attribute
                    formPotentials = Enumerable.Empty<Potential>();
                    bodyPotentials = message.Domain.GetPotentials<Potential>(false).Where(p => !boundPotentials.Contains(p)).ToArray();
                }
                else
                {
                    formPotentials = Enumerable.Empty<Potential>();
                    bodyPotentials = Enumerable.Empty<Potential>();
                }
            }
        }

        public static void GetResponseMessagePotentials(this InteractionMessage message, out IEnumerable<Potential> headerPotentials, out IEnumerable<Potential> pathPotentials, out IEnumerable<Potential> queryPotentials, out IEnumerable<Potential> formPotentials, out IEnumerable<Potential> bodyPotentials, out IEnumerable<Potential> statusCodePotentials)
        {
            // keep track of bound agents as you go - remaining unbound (soap/mime) items will bind to body

            BindingProperty bindingProperty;

            List<Potential> boundPotentials = new List<Potential>();

            FilterAgents(
                message,
                BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out headerPotentials);

            FilterAgents(
                message,
                BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out pathPotentials);

            FilterAgents(
                message,
                BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out queryPotentials);

            FilterAgents(
                message,
                BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME,
                BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                boundPotentials,
                out statusCodePotentials);

            if (statusCodePotentials.Count() == 0 && message.TryGetSoapStatusCodeBinding(out bindingProperty))
            {
                IEnumerable<String> potentialNames = bindingProperty.Attributes.GetItems(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME).Select(a => a.Value);

                if (message.Domain.TryGetPotentials(potentialNames, out statusCodePotentials))

                    boundPotentials.AddRange(statusCodePotentials);
            }

            String[] mimeContentTypes;

            if (message.TryGetMimeContentBindings(out mimeContentTypes))
            {
                if (mimeContentTypes.Any(m => m == "application/x-www-form-urlencoded" || m == "multipart/form-data"))
                {
                    formPotentials = message.Domain.GetPotentials<Potential>(false).Where(a => !boundPotentials.Contains(a)).ToArray();
                    bodyPotentials = Enumerable.Empty<Potential>();
                }
                else // application/json, application/text, ... e.g. body
                {
                    formPotentials = Enumerable.Empty<Potential>();
                    bodyPotentials = message.Domain.GetPotentials<Potential>(false).Where(a => !boundPotentials.Contains(a)).ToArray();
                }
            }
            else if (message.TryGetSoapBodyBinding(out bindingProperty))
            {
                //Todo:  refer to "parts" attribute
                formPotentials = Enumerable.Empty<Potential>();
                bodyPotentials = message.Domain.GetPotentials<Potential>(false).Where(a => !boundPotentials.Contains(a)).ToArray();
            }
            else
            {
                formPotentials = Enumerable.Empty<Potential>();
                bodyPotentials = Enumerable.Empty<Potential>();
            }
        }

        private static void FilterAgents(InteractionMessage message, QualifiedName bindingPropertyQualifiedName, string bindingPropertyAttributeName, List<Potential> boundPotentials, out IEnumerable<Potential> bindingPotentials)
        {
            BindingProperty bindingProperty;

            if (message.Bindings.TryGetProperty(out bindingProperty, bindingPropertyQualifiedName))
            {
                IEnumerable<String> agentNames = bindingProperty.Attributes.GetItems(bindingPropertyAttributeName).Select(a => a.Value);

                if (message.Domain.TryGetPotentials(agentNames, out bindingPotentials))

                    boundPotentials.AddRange(bindingPotentials);

                else

                    bindingPotentials = Enumerable.Empty<Potential>();
            }
            else

                bindingPotentials = Enumerable.Empty<Potential>();
        }

        public static bool TryGetSoapBodyBinding(this InteractionMessage message, out BindingProperty soapBindingProperty)
        {
            if (!message.Bindings.TryGetProperty(out soapBindingProperty, BindingConstants.SOAP_BINDING_BODY_PROPERTY_NAME))
            {
                if (!message.Bindings.TryGetProperty(out soapBindingProperty, BindingConstants.SOAP12_BINDING_BODY_PROPERTY_NAME))

                    soapBindingProperty = null;
            }

            return soapBindingProperty != null;
        }

        public static bool TryGetMimeContentBindings(this InteractionMessage message, out string[] mimeContentTypes)
        {
            mimeContentTypes = null;

            BindingProperty property;

            if (message.Bindings.TryGetProperty(out property, BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME))
            {
                mimeContentTypes = property.Attributes.Where(a => a.Name == BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME).Select(a => a.Value).ToArray();
            }

            return mimeContentTypes != null && mimeContentTypes.Length > 0;
        }

        public static bool TryGetSoapStatusCodeBinding(this InteractionMessage message, out BindingProperty statusCodeBindingProperty)
        {
            if (!message.Bindings.TryGetProperty(out statusCodeBindingProperty, BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME))
            {
                if (!message.Bindings.TryGetProperty(out statusCodeBindingProperty, BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME))

                    statusCodeBindingProperty = null;
            }

            return statusCodeBindingProperty != null;
        }
    }
}
