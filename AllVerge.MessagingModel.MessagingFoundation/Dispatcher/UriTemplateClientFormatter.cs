//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
#pragma warning disable 1634, 1691
namespace AllVerge.MessagingModel.MessagingFoundation.Dispatcher
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.Text;
    using System.Xml;
    using System.ServiceModel.Web;
    using System.ServiceModel.Dispatcher;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions;
    using static AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.ResourceActions;

    public class UriTemplateClientFormatter : IClientMessageFormatter
    {
        internal Dictionary<int, string> pathMapping;
        internal Dictionary<int, KeyValuePair<string, Type>> queryMapping;
        Uri baseUri;
        IClientMessageFormatter inner;
        bool innerIsUntypedMessage;
        QueryStringConverter qsc;
        int totalNumUTVars;
        UriTemplate uriTemplate;

        public UriTemplateClientFormatter(ResourceActionAttribute resourceActionAttribute, OperationDescription operationDescription, IClientMessageFormatter inner, QueryStringConverter qsc, Uri baseUri, bool innerIsUntypedMessage, string contractName)
        {
            this.inner = inner;
            this.qsc = qsc;
            this.baseUri = baseUri;
            this.innerIsUntypedMessage = innerIsUntypedMessage;
            Populate(
                out this.pathMapping,
                out this.queryMapping,
                out this.totalNumUTVars,
                out this.uriTemplate,
                operationDescription,
                resourceActionAttribute,
                qsc,
                contractName);
            this.Method = resourceActionAttribute.ResourceAction;
            AllowedHalfDuplexMessages allowedHalfDuplexMessages = ResourceActions.GetAllowedHalfDuplexMessages(this.Method);
            this.SuppressRequestBody = !allowedHalfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.Request);
            this.SuppressResponseBody = !allowedHalfDuplexMessages.HasFlag(AllowedHalfDuplexMessages.Response);
        }

        public string Method { get; }

        public bool SuppressRequestBody { get; }

        public bool SuppressResponseBody { get; }

        public bool HasUriTemplate => this.uriTemplate.QueryValueVariableNames.Count + this.uriTemplate.PathSegmentVariableNames.Count > 0;

        public object DeserializeReply(Message message, object[] parameters)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                new NotSupportedException(
                    PublicSR.Format(
                        PublicSR.QueryStringFormatterOperationNotSupportedClientSide)));
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            object[] innerParameters = new object[parameters.Length - this.totalNumUTVars];
            NameValueCollection nvc = new NameValueCollection();
            int j = 0;
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (this.pathMapping.ContainsKey(i))
                {
                    nvc[this.pathMapping[i]] = parameters[i] as string;
                }
                else if (this.queryMapping.ContainsKey(i))
                {
                    if (parameters[i] != null)
                    {
                        nvc[this.queryMapping[i].Key] = this.qsc.ConvertValueToString(parameters[i], this.queryMapping[i].Value);
                    }
                }
                else
                {
                    innerParameters[j] = parameters[i];
                    ++j;
                }
            }
            Message m = inner.SerializeRequest(messageVersion, innerParameters);
            bool userSetTheToOnMessage = (this.innerIsUntypedMessage && m.Headers.To != null);
            bool userSetTheToOnOutgoingHeaders = (OperationContext.Current != null && OperationContext.Current.OutgoingMessageHeaders.To != null);
            if (!userSetTheToOnMessage && !userSetTheToOnOutgoingHeaders)
            {
                m.Headers.To = this.uriTemplate.BindByName(this.baseUri, nvc);
            }
            //if (WebOperationContext.Current != null)
            //{
            //    if (suppressEntityBody)
            //    {
            //        WebOperationContext.Current.OutgoingRequest.SuppressEntityBody = true;
            //    }
            //    if (this.method != WebHttpBehavior.WildcardMethod && WebOperationContext.Current.OutgoingRequest.Method != null)
            //    {
            //        WebOperationContext.Current.OutgoingRequest.Method = this.method;
            //    }
            //}
            //else
            {
                HttpRequestMessageProperty hrmp;
                if (m.Properties.ContainsKey(HttpRequestMessageProperty.Name))
                {
                    hrmp = m.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                }
                else
                {
                    hrmp = new HttpRequestMessageProperty();
                    m.Properties.Add(HttpRequestMessageProperty.Name, hrmp);
                }
                if (this.SuppressRequestBody)
                {
                    hrmp.SuppressEntityBody = true;
                }
                //if (this.method != WebHttpBehavior.WildcardMethod)
                {
                    hrmp.Method = this.Method;
                }
            }
            return m;
        }

        internal static string GetUTStringOrDefault(OperationDescription operationDescription, ResourceActionAttribute resourceActionAttribute)
        {
            string utString;
            if (resourceActionAttribute is ResourceActionTemplateAttribute)
            {
                utString = (resourceActionAttribute as ResourceActionTemplateAttribute).Template;
            }
            else
            {
                utString = MakeDefaultGetUTString(operationDescription);
            }
            if (utString == null)
            {
                utString = operationDescription.Name;
            }
            return utString;
        }

        internal static void Populate(out Dictionary<int, string> pathMapping,
            out Dictionary<int, KeyValuePair<string, Type>> queryMapping,
            out int totalNumUTVars,
            out UriTemplate uriTemplate,
            OperationDescription operationDescription,
            ResourceActionAttribute resourceActionAttribute,
            QueryStringConverter qsc,
            string contractName)
        {
            pathMapping = new Dictionary<int, string>();
            queryMapping = new Dictionary<int, KeyValuePair<string, Type>>();
            string utString = GetUTStringOrDefault(operationDescription, resourceActionAttribute);
            uriTemplate = new UriTemplate(utString);
            List<string> neededPathVars = new List<string>(uriTemplate.PathSegmentVariableNames);
            List<string> neededQueryVars = new List<string>(uriTemplate.QueryValueVariableNames);
            Dictionary<string, byte> alreadyGotVars = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            totalNumUTVars = neededPathVars.Count + neededQueryVars.Count;
            for (int i = 0; i < operationDescription.Messages[0].Body.Parts.Count; ++i)
            {
                MessagePartDescription mpd = operationDescription.Messages[0].Body.Parts[i];
                string parameterName = NamingHelper.CodeName(mpd.Name);//mpd.XmlName.DecodedName;
                if (alreadyGotVars.ContainsKey(parameterName))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new InvalidOperationException(
                            PublicSR.Format(
                                PublicSR.UriTemplateVarCaseDistinction, NamingHelper.CodeName(operationDescription.Name), contractName, parameterName)));
                }
                List<string> neededPathCopy = new List<string>(neededPathVars);
                foreach (string pathVar in neededPathCopy)
                {
                    if (string.Compare(parameterName, pathVar, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (mpd.Type != typeof(string))
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new InvalidOperationException(
                                    PublicSR.Format(
                                        PublicSR.UriTemplatePathVarMustBeString, NamingHelper.CodeName(operationDescription.Name), contractName, parameterName)));
                        }
                        pathMapping.Add(i, parameterName);
                        alreadyGotVars.Add(parameterName, 0);
                        neededPathVars.Remove(pathVar);
                    }
                }
                List<string> neededQueryCopy = new List<string>(neededQueryVars);
                foreach (string queryVar in neededQueryCopy)
                {
                    if (string.Compare(parameterName, queryVar, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (!qsc.CanConvert(mpd.Type))
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new InvalidOperationException(
                                    PublicSR.Format(
                                        PublicSR.UriTemplateQueryVarMustBeConvertible, NamingHelper.CodeName(operationDescription.Name), contractName, parameterName, mpd.Type, qsc.GetType().Name)));
                        }
                        queryMapping.Add(i, new KeyValuePair<string, Type>(parameterName, mpd.Type));
                        alreadyGotVars.Add(parameterName, 0);
                        neededQueryVars.Remove(queryVar);
                    }
                }
            }
            if (neededPathVars.Count != 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.Format(
                            PublicSR.UriTemplateMissingVar, NamingHelper.CodeName(operationDescription.Name), contractName, neededPathVars[0])));
            }
            if (neededQueryVars.Count != 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.Format(
                            PublicSR.UriTemplateMissingVar, NamingHelper.CodeName(operationDescription.Name), contractName, neededQueryVars[0])));
            }
        }

        static string MakeDefaultGetUTString(OperationDescription od)
        {
            StringBuilder sb = new StringBuilder(NamingHelper.CodeName(od.Name));
            if (od.Messages[0].IsTypedMessage)
            {
                sb.Append("?");
                foreach (MessagePartDescription mpd in od.Messages[0].Body.Parts)
                {
                    string parameterName = NamingHelper.CodeName(mpd.Name);
                    sb.Append(parameterName);
                    sb.Append("={");
                    sb.Append(parameterName);
                    sb.Append("}&");
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }
    }
}
