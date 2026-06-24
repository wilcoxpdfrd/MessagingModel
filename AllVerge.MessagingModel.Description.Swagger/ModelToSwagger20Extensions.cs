using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using AllVerge.Core.Collections;
using AllVerge.Core.Resource;

using AllVerge.Core.Model;
using AllVerge.Core.Model.Actuals;
using AllVerge.Core.Model.LexicalTypes;

using AllVerge.Core.Model.JsonSchema;
using AllVerge.Core.Model.SwaggerTypes;

using AllVerge.Core.ServiceModel.Description.Model;
using AllVerge.Core.ServiceModel.Description.Adapters;
using AllVerge.Core.Model.JsonSchema.Adapters;
using AllVerge.Core.ServiceModel.Methods;

namespace AllVerge.Core.ServiceModel.Description.Swagger
{
    internal static class ModelToSwagger20Extensions
    {
        public static void WriteInfoAndExternalDocsNodes(this ProtocolDescription description, XmlDictionaryWriter writer)
        {
            // ToDo: echo swagger info node attributes, title, etc. in case we are rewriting a swagger ...

            if (description.AnnotationsSpecified)
            {
                List<Annotation> externalDocsAnnotations = new List<Annotation>();
                Dictionary<String, object> descriptionItems = new Dictionary<string, object>();

                foreach (Annotation annotation in description.Annotations)
                {
                    if (annotation.NameSpecified)
                    {
                        if (annotation.Name.StartsWith(SwaggerTokens.EXTERNAL_DOCS))
                        {
                            externalDocsAnnotations.Add(annotation);
                        }
                        else
                        {
                            if (annotation.Name.Contains(" "))
                            {
                                string[] entryNames = annotation.Name.Split(' ');

                                if (!descriptionItems.ContainsKey(entryNames[0]))

                                    descriptionItems.Add(entryNames[0], new Dictionary<String, String>());

                                (descriptionItems[entryNames[0]] as Dictionary<String, String>).Add(entryNames[1], annotation.Representation.ToFormattedString());
                            }
                            else

                                descriptionItems.Add(annotation.Name, annotation.Representation.ToFormattedString());
                        }
                    }
                    else if (descriptionItems.ContainsKey(SwaggerTokens.DESCRIPTION))

                        descriptionItems[SwaggerTokens.DESCRIPTION] += "\n" + annotation.Representation.ToFormattedString();

                    else

                        descriptionItems.Add(SwaggerTokens.DESCRIPTION, annotation.Representation.ToFormattedString());
                }

                writer.WriteStartElement(SwaggerTokens.INFO);

                writer.WriteAttributeString("type", "object");

                foreach (KeyValuePair<String, Object> descriptionItem in descriptionItems)
                {
                    if (descriptionItem.Value is Dictionary<String, String>)
                    {
                        writer.WriteStartElement(descriptionItem.Key);

                        writer.WriteAttributeString("type", "object");

                        foreach (KeyValuePair<String, String> valueItem in (Dictionary<String, String>)descriptionItem.Value)
                        {
                            writer.WriteStartElement(valueItem.Key);

                            writer.WriteRaw(valueItem.Value);

                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement(descriptionItem.Key);

                        writer.WriteRaw((String)descriptionItem.Value);

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();

                externalDocsAnnotations.WriteExternalDocAnnotations(writer);
            }
        }

        public static void WriteHostAndBasePathAndSchemeNodes(this Connection connection, String hostName, XmlDictionaryWriter writer)
        {
            String bindingNamespace;

            Uri location = connection.GetLocation(out bindingNamespace);

            writer.WriteStartElement(SwaggerTokens.HOST);

            writer.WriteString(hostName);

            writer.WriteEndElement();

            writer.WriteStartElement(SwaggerTokens.BASE_PATH);

            if (bindingNamespace == BindingConstants.HTTP_BINDING_PROPERTY_NAME.Namespace)

                writer.WriteRaw('/' + location.Host + location.LocalPath);

            writer.WriteEndElement();

            writer.WriteStartElement(SwaggerTokens.SCHEMES);

            writer.WriteAttributeString("type", "array");

            writer.WriteElementString("item", location.Scheme);

            writer.WriteEndElement();
        }

        public static IEnumerable<String> WriteTags(this ProtocolDescription description, Connection connection, string behaviorNameOrIndex, XmlDictionaryWriter writer)
        {
            Dictionary<String, IEnumerable<Annotation>> taggedAnnotations =
                description.GetTaggedAnnotations(connection, behaviorNameOrIndex);

            if (taggedAnnotations.Count > 0)
            {
                writer.WriteStartElement(SwaggerTokens.TAGS);

                writer.WriteAttributeString("type", "array");

                foreach (KeyValuePair<String, IEnumerable<Annotation>> tagAndAnnotations in taggedAnnotations)
                {
                    writer.WriteStartElement("item");

                    writer.WriteAttributeString("type", "object");

                    String tagName = tagAndAnnotations.Key;

                    writer.WriteElementString(SwaggerTokens.NAME, tagName);

                    if (tagAndAnnotations.Value.Count() > 0)
                    {
                        IEnumerable<Annotation> tagAnnotations = tagAndAnnotations.Value;

                        Annotation tagEntryDescription = tagAnnotations.FirstOrDefault(i => i.Name == SwaggerTokens.DESCRIPTION);

                        if (tagEntryDescription != null)

                            writer.WriteElementString(SwaggerTokens.DESCRIPTION, tagEntryDescription.Representation.ToFormattedString());

                        IEnumerable<Annotation> tagExternalDocsAnnotations = tagAnnotations.Where(i => i.Name.StartsWith(SwaggerTokens.EXTERNAL_DOCS));

                        tagExternalDocsAnnotations.WriteExternalDocAnnotations(writer);
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            return taggedAnnotations.Keys;
        }

        private static Dictionary<String, IEnumerable<Annotation>> GetTaggedAnnotations(this ProtocolDescription description, Connection connection, string interactionNameOrIndex)
        {
            IEnumerable<Interaction> selectedinteractions = connection.Interactions.Where(b => interactionNameOrIndex == "*" ? true : b.Name == interactionNameOrIndex);

            IEnumerable<IRepresentation> allTags = selectedinteractions.SelectMany(b => b.Annotations.SelectMany(an => an.Attributes.Where(a => a.Name == SwaggerTokens.TAG).Select(a => a.Representation)));

            Dictionary<String, IEnumerable<Annotation>> taggedAnnotations = new Dictionary<String, IEnumerable<Annotation>>();

            foreach (IRepresentation distinctTag in allTags.Distinct(RepresentationCollectionItemComparerComparer.GetInstance()))
            {
                taggedAnnotations.Add(
                    distinctTag.ToFormattedString(),
                    selectedinteractions.SelectMany(b => b.Annotations.Where(an => an.Attributes.Any(a => a.Name == SwaggerTokens.TAG && a.Representation.Equals(distinctTag)))).Distinct(Annotation.GetCollectionItemComparer())
                );
            }

            if (taggedAnnotations.Count == 0)
            {
                IEnumerable<Uri> selectedBehaviorUris = selectedinteractions.Select(b => b.GetLocationOrAction(true));

                IEnumerable<Uri> orderedDistinctBehaviorUris = selectedBehaviorUris.Distinct().OrderBy(u => u.ToString());

                List<Uri> behaviorUris = new List<Uri>();

                foreach (Uri distinctBehaviorUri in orderedDistinctBehaviorUris)
                {
                    if (!behaviorUris.Any(t => distinctBehaviorUri.ToString().StartsWith(t.ToString())))
                    {
                        if (selectedBehaviorUris.Count(u => u.ToString().StartsWith(distinctBehaviorUri.ToString())) > 1)

                            behaviorUris.Add(distinctBehaviorUri);

                        else
                        {
                            Uri currentDistinctBehaviorUri = distinctBehaviorUri;
                            Uri parentDistinctBehaviorUri;

                            while (currentDistinctBehaviorUri != null)
                            {
                                if (currentDistinctBehaviorUri.TryGetParentUri(out parentDistinctBehaviorUri))
                                {
                                    if (selectedBehaviorUris.Count(u => u.ToString().StartsWith(parentDistinctBehaviorUri.ToString())) > 1)
                                    {
                                        behaviorUris.Add(parentDistinctBehaviorUri);

                                        currentDistinctBehaviorUri = null;
                                    }
                                    else

                                        currentDistinctBehaviorUri = parentDistinctBehaviorUri;
                                }
                                else if (!behaviorUris.Any(t => currentDistinctBehaviorUri.ToString().StartsWith(t.ToString())))
                                {
                                    behaviorUris.Add(currentDistinctBehaviorUri);

                                    currentDistinctBehaviorUri = null;
                                }
                            }
                        }
                    }
                }

                foreach (Uri behaviorUri in behaviorUris)
                {
                    taggedAnnotations.Add(
                        behaviorUri.ToBaseOfPathName("default"),
                        Enumerable.Empty<Annotation>()
                    );
                }
            }

            return taggedAnnotations;
        }

        private static void WriteExternalDocAnnotations(this IEnumerable<Annotation> externalDocsAnnotations, XmlDictionaryWriter writer)
        {
            if (externalDocsAnnotations.Count() > 0)
            {
                writer.WriteStartElement(SwaggerTokens.EXTERNAL_DOCS);

                writer.WriteAttributeString("type", "object");

                Annotation externalDocsDescriptionAnnotation = externalDocsAnnotations.FirstOrDefault(i => i.Name.EndsWith(SwaggerTokens.DESCRIPTION));

                if (externalDocsDescriptionAnnotation != null)

                    // Todo:  Markdown formatting?

                    writer.WriteElementString(SwaggerTokens.DESCRIPTION, externalDocsDescriptionAnnotation.Representation.ToFormattedString());

                Annotation externalDocsUrlAnnotation = externalDocsAnnotations.FirstOrDefault(i => i.Name.EndsWith(SwaggerTokens.URL));

                if (externalDocsUrlAnnotation != null)

                    writer.WriteElementString(SwaggerTokens.URL, externalDocsUrlAnnotation.Representation.ToFormattedString());

                writer.WriteEndElement();
            }
        }

        private static void MergeBehaviorAndMessageAnnotations(this IEnumerable<Annotation> behaviorAnnotations, InteractionMessage[] inputs, InteractionMessage[] outputs, InteractionMessage[] faults, out Annotations mergedAnnotations)
        {
            mergedAnnotations =
                behaviorAnnotations.Aggregate(Annotations.CreateImmutableOnRead(), (m, a) => { m.Add(a.Clone()); return m; });

            foreach (Annotation inputDocumentation in inputs.Where(i => i.AnnotationsSpecified).SelectMany(i => i.Annotations).Select(i => { Annotation e = i.Clone(); if (e.NameSpecified) e.Name = "parameters " + e.Name; else e.Name = "parameters"; return e; }))

                mergedAnnotations.Put(inputDocumentation);

            foreach (Annotation outputDocumentation in outputs.Where(i => i.AnnotationsSpecified).SelectMany(i => i.Annotations).Select(i => { Annotation e = i.Clone(); if (e.NameSpecified) e.Name = "responses " + e.Name; else e.Name = "responses"; return e; }))

                mergedAnnotations.Put(outputDocumentation);

            foreach (Annotation faultDocumentation in faults.Where(i => i.AnnotationsSpecified).SelectMany(i => i.Annotations).Select(i => { Annotation e = i.Clone(); if (e.NameSpecified) e.Name = "faults " + e.Name; else e.Name = "faults"; return e; }))

                mergedAnnotations.Put(faultDocumentation);
        }

        private static void WriteVerbAnnotationsNodes(this Annotations annotations, XmlDictionaryWriter writer)
        {
            if (annotations.Count > 0)
            {
                Annotation summaryAnnotation;

                if (annotations.TryGetAnnotation(SwaggerTokens.SUMMARY, out summaryAnnotation))

                    summaryAnnotation.WriteAnnotation(writer);

                Annotation descriptionAnnotation;

                if (annotations.TryGetAnnotation(SwaggerTokens.DESCRIPTION, out descriptionAnnotation))

                    descriptionAnnotation.WriteAnnotation(writer);

                IEnumerable<Annotation> externalDocsAnnotations;

                if (annotations.TryGetAnnotations(a => a.NameSpecified && a.Name.StartsWith(SwaggerTokens.EXTERNAL_DOCS), out externalDocsAnnotations))

                    externalDocsAnnotations.WriteExternalDocAnnotations(writer);

                IEnumerable<Annotation> remainingAnnotions =
                    annotations.Where(a =>
                    {
                        if (!a.NameSpecified)
                            return true;
                        else
                        {
                            switch (a.Name)
                            {
                                case SwaggerTokens.SUMMARY:
                                case SwaggerTokens.DESCRIPTION:
                                    return false;
                                default:
                                    if (a.Name.StartsWith(SwaggerTokens.EXTERNAL_DOCS))
                                        return false;
                                    return true;
                            }
                        }
                    });

                if (remainingAnnotions.Count() > 0)
                {
                    // Todo:  Markdown formatting

                    StringBuilder sb = annotations.Aggregate(new StringBuilder(), (s, e) => { if (e.Name != null) s.Append(e.Name + " - "); s.AppendLine(e.Representation.ToFormattedString()); return s; });

                    writer.WriteStartElement(SwaggerTokens.DESCRIPTION);

                    writer.WriteRaw(sb.ToString());

                    writer.WriteEndElement();
                }
            }
        }

        private static void WriteAnnotation(this Annotation annotation, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(annotation.Name);

            // Todo:  Markdown formatting?

            writer.WriteRaw(annotation.Representation.ToFormattedString());

            writer.WriteEndElement();
        }

        private static void WriterOperationIdNode(this Interaction interaction, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(SwaggerTokens.OPERATION_ID);

            writer.WriteRaw(interaction.Name);

            writer.WriteEndElement();
        }

        private static void WriteConsumesArray(this InteractionMessage[] inputs, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("consumes");

            writer.WriteAttributeString("type", "array");

            IEnumerable<String> mimeContentTypes = inputs.SelectMany(i => { return i.GetMimeContentTypes(); }, (i, m) => m);

            foreach (String mimeType in mimeContentTypes)
            {
                writer.WriteStartElement("item");

                writer.WriteRaw(mimeType);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteProducesArray(this InteractionMessage[] outputs, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("produces");

            writer.WriteAttributeString("type", "array");

            foreach (String mimeType in outputs.SelectMany(i => { return i.GetMimeContentTypes(); }, (i, m) => m))
            {
                writer.WriteStartElement("item");

                writer.WriteRaw(mimeType);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static IEnumerable<string> GetMimeContentTypes(this InteractionMessage actionMessage)
        {
            string[] mimeTypes;

            if (actionMessage.TryGetMimeContentBindings(out mimeTypes))

                return mimeTypes;

            else
            {
                BindingProperty soapBindingProperty;

                if (actionMessage.TryGetSoapBodyBinding(out soapBindingProperty))

                    return new String[] { "application/json", "application/xml" };

                else return new string[0];
            }
        }

        private static void WriteParametersArray(this InteractionMessage[] inputs, ReferenceManagers references, XmlDictionaryWriter writer)
        {
            if (inputs.Length > 1)

                throw new InvalidOperationException("SWAGGER does not support multiple parameters.");

            if (inputs.Length > 0)
            {
                foreach (InteractionMessage inputMessage in inputs)
                {
                    writer.WriteStartElement(SwaggerTokens.PARAMETERS);

                    writer.WriteAttributeString("type", "array");

                    IEnumerable<Potential> headerParts;
                    IEnumerable<Potential> pathParts;
                    IEnumerable<Potential> queryParts;
                    IEnumerable<Potential> formParts;
                    IEnumerable<Potential> bodyParts;

                    inputMessage.GetRequestMessagePotentials(out headerParts, out pathParts, out queryParts, out formParts, out bodyParts);

                    foreach (Potential part in headerParts)
                    {
                        part.WriteParameterNode(SwaggerTokens.HEADER, references, writer);
                    }

                    foreach (Potential part in queryParts)
                    {
                        part.WriteParameterNode(SwaggerTokens.QUERY, references, writer);
                    }

                    foreach (Potential part in pathParts)
                    {
                        part.WriteParameterNode(SwaggerTokens.PATHS, references, writer);
                    }

                    foreach (Potential part in formParts)
                    {
                        part.WriteParameterNode(SwaggerTokens.FORM_DATA, references, writer);
                    }

                    foreach (Potential part in bodyParts)

                        part.WriteParameterNode(SwaggerTokens.IN_BODY, references, writer);
                }

                writer.WriteEndElement();
            }
        }

        private static void WriteParameterNode(this Potential potential, String parameterLocation, ReferenceManagers references, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("item");

            writer.WriteAttributeString("type", "object");

            writer.WriteElementString(SwaggerTokens.NAME, potential.Name);

            writer.WriteElementString(SwaggerTokens.IN, parameterLocation);

            potential.WritePotentialNode(WriteContext.TypeContent, parameterLocation == SwaggerTokens.IN_BODY, JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI, SwaggerDataTypes.VERSION_2_0_URI, references.Definitions, writer);

            writer.WriteEndElement();
        }

        private static void WriteReferenceNode(string referenceName, string referenceKey, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(referenceName);

            writer.WriteAttributeString("type", "object");

            writer.WriteStartElement("$ref");

            writer.WriteRaw(referenceKey);

            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        private static void WriteResponsesNode(this InteractionMessage[] outputs, InteractionMessage[] faults, ReferenceManagers references, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(SwaggerTokens.RESPONSES);

            writer.WriteAttributeString("type", "object");

            foreach (InteractionMessage message in outputs.Union(faults))
            {
                BindingProperty statusCodeBindingProperty;

                List<String> boundPartNames = new List<string>();

                if (message.Bindings.TryGetProperty(out statusCodeBindingProperty, BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME) ||
                    message.Bindings.TryGetProperty(out statusCodeBindingProperty, BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME) ||
                    message.Bindings.TryGetProperty(out statusCodeBindingProperty, BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME))
                {
                    BindingAttribute[] statusCodeAttributes;

                    if (statusCodeBindingProperty.Attributes.TryGetItems(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out statusCodeAttributes))
                    {
                        foreach (BindingAttribute statusCodeAttribute in statusCodeAttributes)
                        {
                            String statusCodePartName = statusCodeAttribute.Value;

                            Potential statusCodeCompactPart;

                            if (message.Domain.TryGetPotentialOfKind(LexicalTypeKind.Scalar, statusCodePartName, out statusCodeCompactPart))
                            {
                                String statusCodeValueName = statusCodeCompactPart.LexicalType.Facets.ValueFacet.Representation.ToFormattedString();

                                writer.WriteStartElement(statusCodeValueName);

                                writer.WriteAttributeString("type", "object");

                                if (statusCodeCompactPart.AnnotationsSpecified)

                                    statusCodeCompactPart.Annotations.WriteResponseAnnotationsNodes(message.GetMimeContentTypes(), writer);

                                BindingProperty[] headerBindingProperties;
                                 
                                if (message.Bindings.TryGetProperties(out headerBindingProperties, BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME))
                                {
                                    writer.WriteStartElement(SwaggerTokens.HEADERS);

                                    writer.WriteAttributeString("type", "object");

                                    foreach (BindingProperty headerBindingProperty in headerBindingProperties)
                                    {
                                        String[] httpHeaderPartNames = headerBindingProperty.Attributes.GetItems(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME).Select(p => p.Value).ToArray();

                                        List<Potential> httpHeaderCompactPotentials = new List<Potential>();

                                        foreach (String httpHeaderPartName in httpHeaderPartNames)
                                        {
                                            Potential httpHeaderCompactPart;

                                            if (message.Domain.TryGetPotentialOfKind(LexicalTypeKind.Scalar, httpHeaderPartName, out httpHeaderCompactPart))
                                            {
                                                httpHeaderCompactPart.WritePotentialNode(WriteContext.Potential, false, JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI, SwaggerDataTypes.VERSION_2_0_URI, null, writer);

                                                boundPartNames.Add(httpHeaderCompactPart.Name);
                                            }
                                        }
                                    }

                                    writer.WriteEndElement();
                                }

                                IEnumerable<Potential> responsePotentials;

                                if (message.Domain.TryGetPotentials(CollectionExtensions.Itemize(statusCodePartName, statusCodePartName + SwaggerTokens.CONTENT_NAME_SUFFIX), out responsePotentials, false, false))
                                {
                                    foreach (Potential responsePotential in responsePotentials)
                                    {
                                        if (!responsePotential.LexicalType.Facets.IsConstantValueFacetValue(statusCodeValueName))

                                            responsePotential.WritePotentialNode(WriteContext.TypeContent, true, JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI, SwaggerDataTypes.VERSION_2_0_URI, references.Definitions, writer);
                                    }
                                }

                                writer.WriteEndElement();

                                boundPartNames.Add(statusCodePartName);
                            }
                        }
                    }
                }
            }

            writer.WriteEndElement();
        }

        private static void WriteResponseAnnotationsNodes(this Annotations annotations, IEnumerable<String> mimeContentTypes, XmlDictionaryWriter writer)
        {
            if (annotations.Count > 0)
            {
                Annotation descriptionAnnotation;

                if (annotations.TryGetAnnotation(SwaggerTokens.DESCRIPTION, out descriptionAnnotation))

                    descriptionAnnotation.WriteAnnotation(writer);

                Annotation examplesAnnotation;

                if (annotations.TryGetAnnotation(SwaggerTokens.EXAMPLES, out examplesAnnotation))

                    examplesAnnotation.WriteExamplesAnnotation(mimeContentTypes, writer);

                IEnumerable<Annotation> remainingAnnotions =
                    annotations.Where(a =>
                    {
                        if (!a.NameSpecified)
                            return true;
                        else
                        {
                            switch (a.Name)
                            {
                                case SwaggerTokens.DESCRIPTION:
                                case SwaggerTokens.EXAMPLES:
                                    return false;
                                default:
                                    return true;
                            }
                        }
                    });

                if (remainingAnnotions.Count() > 0)
                {
                    // Todo:  Markdown formatting

                    StringBuilder sb = annotations.Aggregate(new StringBuilder(), (s, e) => { if (e.Name != null) s.Append(e.Name + " - "); s.AppendLine(e.Representation.ToFormattedString()); return s; });

                    writer.WriteStartElement(SwaggerTokens.DESCRIPTION);

                    writer.WriteRaw(sb.ToString());

                    writer.WriteEndElement();
                }
            }
        }

        private static void WriteExamplesAnnotation(this Annotation examplesAnnotation, IEnumerable<String> mimeContentTypes, XmlDictionaryWriter writer)
        {
            writer.WriteStartElement(SwaggerTokens.EXAMPLES);

            // Todo:  Markdown formatting?

            String mimeContentType = mimeContentTypes.FirstOrDefault();

            if (mimeContentType == null)

                writer.WriteRaw(examplesAnnotation.Representation.ToFormattedString(MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE));

            else

                writer.WriteRaw(examplesAnnotation.Representation.ToFormattedString(mimeContentType));

            writer.WriteEndElement();
        }

        public static void WritePathNodes(this Connection connection, string behaviorNameOrIndex, XmlDictionaryWriter writer, ReferenceManagers references, IEnumerable<String> tags)
        {
            writer.WriteStartElement(SwaggerTokens.PATHS);

            writer.WriteAttributeString("type", "object");

            foreach (Interaction interaction in connection.Interactions.Where(o => behaviorNameOrIndex == "*" ? true : o.Name == behaviorNameOrIndex))
            {
                interaction.WritePathNode(writer, references, tags);
            }

            writer.WriteEndElement();
        }

        private static void WritePathNode(this Interaction interaction, XmlDictionaryWriter writer, ReferenceManagers references, IEnumerable<String> tags)
        {
            Uri locationOrAction = interaction.GetLocationOrAction(true);

            if (locationOrAction == null)

                throw new InvalidOperationException("Service behavior binding missing required location or action attribute.");

            String path;

            if (locationOrAction.IsAbsoluteUri)

                path = '/' + locationOrAction.Host + locationOrAction.AbsolutePath;

            else

                path = locationOrAction.OriginalString;

            writer.WriteStartElement(path);

            writer.WriteAttributeString("type", "object");

            interaction.WriteVerbNode(writer, references, tags);

            writer.WriteEndElement();
        }

        private static void WriteVerbNode(this Interaction interaction, XmlDictionaryWriter writer, ReferenceManagers references, IEnumerable<String> tags)
        {
            BindingProperty bindingProperty;

            string verb = null;

            if (interaction.Bindings.TryGetProperty(out bindingProperty, "binding"))
            {
                if (bindingProperty.QualifiedName.Namespace == BindingConstants.HTTP_BINDING_PROPERTY_NAME.Namespace)
                {
                    BindingAttribute verbAttribute;

                    if (bindingProperty.Attributes.TryGetItem(BindingConstants.BINDING_VERB_ATTRIBUTE_NAME, out verbAttribute))

                        verb = verbAttribute.Value;
                }
                else

                    verb = ResourceMethods.POST;
            }

            if (verb == null)

                throw new InvalidOperationException("Behavior binding missing required verb attribute.");

            writer.WriteStartElement(verb.ToLower());

            writer.WriteAttributeString("type", "object");

            Annotations behaviorAnnotations;

            IEnumerable<Annotation> untaggedBehaviorAnnotations;

            IEnumerable<String> behaviorTags = interaction.GetTagsAndUntaggedAnnotations(out untaggedBehaviorAnnotations);

            writer.WriteStartElement(SwaggerTokens.TAGS);

            writer.WriteAttributeString("type", "array");

            foreach (String behaviorTag in behaviorTags)
            {
                writer.WriteStartElement("item");

                writer.WriteAttributeString("type", "string");

                writer.WriteRaw(behaviorTag);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            untaggedBehaviorAnnotations.MergeBehaviorAndMessageAnnotations(interaction.Inputs, interaction.Outputs, interaction.Faults, out behaviorAnnotations);

            (behaviorAnnotations as IFixable).SetFixed();

            behaviorAnnotations.WriteVerbAnnotationsNodes(writer);

            interaction.WriterOperationIdNode(writer);

            interaction.Inputs.WriteConsumesArray(writer);

            interaction.Outputs.WriteProducesArray(writer);

            interaction.Inputs.WriteParametersArray(references, writer);

            interaction.Outputs.WriteResponsesNode(interaction.Faults, references, writer);

            writer.WriteEndElement();
        }

        private static IEnumerable<String> GetTagsAndUntaggedAnnotations(this Interaction interaction, out IEnumerable<Annotation> untaggedAnnotations)
        {
            untaggedAnnotations = interaction.Annotations.Where(an => !an.Attributes.Any(a => a.Name == SwaggerTokens.TAG));

            IRepresentation[] distinctTagRepresentations = interaction.Annotations.SelectMany(an => an.Attributes.Where(a => a.Name == SwaggerTokens.TAG).Select(a => a.Representation)).Distinct(RepresentationCollectionItemComparerComparer.GetInstance()).ToArray();

            if (distinctTagRepresentations.Length == 0)

                return Enumerable.Repeat(interaction.GetLocationOrAction(true).ToBaseOfPathName("default"), 1);

            else

                return distinctTagRepresentations.Select(r => r.ToFormattedString());
        }
    }
}
