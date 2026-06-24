using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;

using AllVerge.Core.Collections;

using AllVerge.Core.Model;
using AllVerge.Core.Model.Caches;
using AllVerge.Core.Model.Actuals;
using AllVerge.Core.Model.LexicalTypes;
using AllVerge.Core.Model.LexicalTypes.Facets;
using AllVerge.Core.Model.LexicalTypes.Structures;

using AllVerge.Core.Markup.Xml.Schema;

using AllVerge.Core.Model.Xml.Adapters;
using AllVerge.Core.Model.XMLSchema;
using AllVerge.Core.Model.XMLSchema.Adapters;

using AllVerge.Core.ServiceModel.Description.Model;

using wadl.dev.java.net._2009._02;

namespace AllVerge.Core.ServiceModel.Description.Wadl.v200902
{
    internal static class Wadl200902ToModelExtensions
    {
        public static IEnumerable<SchemaInclude> ReadSchemaIncludes(this include[] includes, String descriptionUrl)
        {
            if (includes == null)

                return CollectionUtils.ToEnumerable<SchemaInclude>();

            List<SchemaInclude> schemaIncludes = new List<SchemaInclude>();

            foreach (include include in includes)
            {
                schemaIncludes.Add(
                    include.ReadSchemaInclude(descriptionUrl));
            }

            return schemaIncludes;
        }

        public static SchemaInclude ReadSchemaInclude(this include include, String descriptionUrl)
        {
            return new SchemaInclude(descriptionUrl, include.href, include.AnyAttr, include.doc.ReadSchemaDocs());
        }

        public static IEnumerable<SchemaDoc> ReadSchemaDocs(this doc[] docs)
        {
            if (docs == null)

                return CollectionUtils.ToEnumerable<SchemaDoc>();

            return docs.Aggregate(
                new List<SchemaDoc>(), (docsList, d) =>
                {
                    docsList.Add(new SchemaDoc(d.title, d.lang, d.Any, d.AnyAttr));

                    return docsList;
                });
        }

        public static Potential[] ReadParameters(this IEnumerable<param> parameters, string targetNamespace, BindingProperties bindingProperties)
        {
            List<Potential> potentials = new List<Potential>();

            if (parameters == null)

                return potentials.ToArray();

            foreach (param parameter in parameters)
            {
                parameter.doc.ReadSchemaDocs(); //?
                //resource.id;
                //resource.type;

                QualifiedName parameterName = parameter.name;

                switch (parameter.style)
                {
                    case ParamStyle.header:
                        bindingProperties.Put(
                            BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                            BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName));
                        break;
                    case ParamStyle.matrix:
                        bindingProperties.Put(
                            BindingConstants.HTTP_BINDING_MATRIX_PROPERTY_NAME,
                            BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName));
                        break;
                    case ParamStyle.plain:
                        bindingProperties.Put(
                            BindingConstants.HTTP_BINDING_PLAIN_PROPERTY_NAME,
                            BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName));
                        break;
                    case ParamStyle.query:
                        bindingProperties.Put(
                            BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                            BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName));
                        break;
                    case ParamStyle.template:
                        bindingProperties.Put(
                            BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME,
                            BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName));
                        break;
                }

                potentials.Add(
                    parameter.GetOrReadScalarPotential(targetNamespace));
            }

            return potentials.ToArray();
        }

        public static Potential GetOrReadScalarPotential(this param param, String targetNamespace)
        {
            if (param.href != null)

                return (Actual)ModelCaches.GetPotentialFromCache(targetNamespace, param.href, Represents.Information).CloneDerived();

            return
                new Potential(
                    param.name,
                    param.AnyAttr.ReadAttributes(Represents.MetaData),
                    param.doc.ReadSchemaDocs().ReadAnnotations(),
                    param.ReadStructure(),
                    param.GetOrReadScalarType(targetNamespace),
                    Represents.Information);
        }

        public static Structure ReadStructure(this param param)
        {
            if (param.required)
            {
                if (param.repeating)

                    return Structure.OneOrMoreTotallyOrderedElements;

                return Structure.ExactlyOneElement;
            }
            else
            {
                if (param.repeating)

                    return Structure.ZeroOrMoreTotallyOrderedElements;

                return Structure.ZeroOrOneElement;
            }
        }

        public static ScalarTypeFacets ReadScalarTypeFacets(this param param)
        {
            LexicalValueExistenceFacet existenceFacet = param.required ? LexicalValueExistenceFacet.Required : LexicalValueExistenceFacet.Optional;

            if (param.@fixed != null)
            {
                return ScalarTypeFacets.CreateImmutableOnRead(
                    existenceFacet, 
                    LexicalConstantValueFacet.CreateImmutable(new Term(param.@fixed)));
            }
            else if (param.@default != null)
            {
                return ScalarTypeFacets.CreateImmutableOnRead(
                    existenceFacet,
                    LexicalDefaultValueFacet.CreateImmutable(new Term(param.@fixed)));
            }
            else
            {
                return ScalarTypeFacets.CreateImmutableOnRead(
                    existenceFacet);
            }
        }

        public static ILexicalType GetOrReadScalarType(this param param, String targetNamespace)
        {
            if (param.type == null)

                param.type = XsDataType.CreateBuiltInScalarValueDataTypeName(XsdStringDataType.NAME).ToXmlQualifiedName();

            XmlSchemaType schemaType = param.type.GetSchemaType(targetNamespace);

            if (schemaType is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType schemaSimpleType = (XmlSchemaSimpleType)schemaType;

                if (schemaSimpleType.TryReadScalarUnionDomainType(out DomainType unionDomainType, targetNamespace, param.ReadScalarTypeFacets()))
                {
                    if (param.option != null)
                    {
                        foreach (LexicalConstantValueFacet enumerationFacet in param.option.ReadEnumerationFacets())

                            unionDomainType.Facets.AddEnumerationConstraint(enumerationFacet);
                    }

                    return unionDomainType;
                }
                else
                {
                    IScalarType scalarType =
                        (schemaType as XmlSchemaSimpleType).ReadScalarType(targetNamespace, param.ReadScalarTypeFacets());

                    if (param.option != null)
                    {
                        foreach (LexicalConstantValueFacet enumerationFacet in param.option.ReadEnumerationFacets())

                            scalarType.Facets.AddEnumerationConstraint(enumerationFacet);
                    }

                    return scalarType;
                }
            }

            throw new NotImplementedException(param.type.ToString());
        }

        public static IEnumerable<LexicalConstantValueFacet> ReadEnumerationFacets(this option[] options)
        {
            return
                options.Select(option =>
                {
                    if (option.mediaType == null)

                        return new LexicalConstantValueFacet(
                            Attributes.Empty,
                            option.doc.ReadSchemaDocs().ReadAnnotations(), 
                            Term.CreateImmutableOnRead(option.value));

                    else
                    {
                        LexicalConstantValueFacet enumerationFacet = 
                            new LexicalConstantValueFacet(
                                Attributes.Empty,
                                option.doc.ReadSchemaDocs().ReadAnnotations(), 
                                Term.CreateImmutableOnRead(option.value));

                        enumerationFacet.Annotations.Add(
                            new Annotation(Wadl200902Constants.MediaType, Term.CreateImmutableOnRead(option.mediaType)));

                        return enumerationFacet;
                    }
                });
        }
    }
}
