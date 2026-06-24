using System;
using System.Xml;

namespace AllVerge.MessagingModel.Description.Html
{
    using AllVerge.DataModel.Primitives.DataTypes;
    using AllVerge.DataModel.Primitives.LexicalTypes;
    using AllVerge.DataModel.Primitives.LexicalTypes.Facets;
    using AllVerge.DataModel.Primitives.LexicalTypes.Structures;

    public static class ModelExtensions
    {
        public static void WriteHtmlInputs(this Domain domain, XmlWriter writer, String domainName, String domainLabel, String formName)
        {
            writer.WriteElementString("h3", $"{domainLabel}");

            int blockCount = 0;

            foreach (Block block in domain)
            {
                switch (block.Refinement)
                {
                    case Refinement.Singleton:
                        writer.WriteElementString("h4", " (One Of)");
                        writer.WriteStartElement("ul");
                        break;
                    case Refinement.PartialCovering:
                        writer.WriteElementString("h4", " (Some Of)");
                        writer.WriteStartElement("ul");
                        break;
                    case Refinement.TotalCovering:
                        writer.WriteElementString("h4", " (All Of)");
                        writer.WriteStartElement("ul");
                        break;
                    case Refinement.PartialOrdering:
                    case Refinement.TotalOrdering:
                        writer.WriteElementString("h4", " (All Of)");
                        writer.WriteStartElement("ol");
                        break;
                }

                foreach (Potential potential in block)
                {
                    String potentialName = $"{domainName}-{blockCount}-{potential.Name}";

                    writer.WriteStartElement("li");

                    switch (potential.LexicalType.Kind)
                    {
                        case LexicalTypeKind.Any:
                            break;
                        case LexicalTypeKind.Nil:
                            break;
                        case LexicalTypeKind.Domain:
                            DomainType potentialDomainType = potential.LexicalType as DomainType;
                            if (potentialDomainType.DomainSpecified)
                                potentialDomainType.Domain.WriteHtmlInputs(writer, potentialName, potential.Name, formName);
                            else
                            {
                                writer.WriteElementString("h3", potential.Name);
                                writer.WriteElementString("h4", " (Empty)");
                            }
                            break;
                        case LexicalTypeKind.Scalar:
                            ScalarType potentialScalarType = potential.LexicalType as ScalarType;
                            potentialScalarType.WriteHtmlInput(writer, potentialName, potential.Name, formName);
                            break;
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                blockCount++;
            }
        }

        public static void WriteHtmlInput(this ScalarType scalarType, XmlWriter writer, String inputName, String inputLabel, String formName)
        {
            writer.WriteStartElement("label");

            writer.WriteAttributeString("for", inputName);

            writer.WriteString(inputLabel);

            writer.WriteEndElement();

            ScalarTypeFacets scalarTypeFacets = scalarType.Facets;

            scalarTypeFacets.TryGetValueFacet(out bool constantValue, out String valueFacetValue);

            LexicalValueExistenceFacet lexicalValueExistenceFacet = scalarTypeFacets.GetExistenceFacet();

            if (scalarTypeFacets.EnumerationSpecified)
            {
                writer.WriteStartElement("select");

                writer.WriteAttributeString("form", formName);

                writer.WriteAttributeString("name", inputName);

                //ToDo:  LexicalValueExistenceFacet.Forbidden, LexicalValueExistenceFacet.Nillable;

                if (lexicalValueExistenceFacet == LexicalValueExistenceFacet.Required)

                    writer.WriteAttributeString("required", String.Empty);

                foreach (LexicalConstantValueFacet enumeral in scalarTypeFacets.Enumeration)
                {
                    String enumeralValue = enumeral.Representation.ToFormattedString();

                    if (constantValue && enumeralValue != valueFacetValue)

                        continue;

                    writer.WriteStartElement("option");

                    writer.WriteAttributeString("value", enumeralValue);

                    if (valueFacetValue != null && valueFacetValue == enumeralValue)

                        writer.WriteAttributeString("selected", String.Empty);

                    writer.WriteString(enumeralValue);

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartElement("input");

                writer.WriteAttributeString("form", formName);

                switch (scalarType.DataType.BinaryForm.BinaryFormat)
                {
                    case BinaryFormats.BIT:
                        writer.WriteAttributeString("type", "checkbox");
                        break;
                    case BinaryFormats.DATE_TIME:
                        if (scalarType.DataType.BinaryForm.HasBinaryDataFormat)
                        {
                            switch (scalarType.DataType.BinaryForm.BinaryDataFormat)
                            {
                                case BinaryForm.DateTimeOnlyBinaryDataFormat:
                                    writer.WriteAttributeString("type", "datetime-local");
                                    break;
                                case BinaryForm.DateOnlyBinaryDataFormat:
                                    writer.WriteAttributeString("type", "date");
                                    break;
                                case BinaryForm.TimeOnlyBinaryDataFormat:
                                    writer.WriteAttributeString("type", "time");
                                    break;
                                case BinaryForm.DayOnlyBinaryDataFormat:
                                    // ToDo: enforce current month/year?
                                    writer.WriteAttributeString("type", "date");
                                    break;
                                case BinaryForm.MonthOnlyBinaryDataFormat:
                                    // ToDo: enforce current year?
                                    writer.WriteAttributeString("type", "month");
                                    break;
                                case BinaryForm.MonthDayBinaryDataFormat:
                                    // ToDo: enforce current year?
                                    writer.WriteAttributeString("type", "date");
                                    break;
                                case BinaryForm.YearMonthBinaryDataFormat:
                                    writer.WriteAttributeString("type", "month");
                                    break;
                                case BinaryForm.YearOnlyBinaryDataFormat:
                                    // ToDo: enforce month=1?
                                    writer.WriteAttributeString("type", "date");
                                    break;
                                default:
                                    //ToDo: Unknown BinaryDataFormat?
                                    writer.WriteAttributeString("type", "date");
                                    break;
                            }
                        }
                        else
                            writer.WriteAttributeString("type", "date");
                        scalarTypeFacets.TryWriteComparableFacetAttribues(writer);
                        break;
                    case BinaryFormats.DURATION:
                        writer.WriteAttributeString("type", "text");
                        break;
                    case BinaryFormats.INTEGER:
                    case BinaryFormats.INTEGER_16BIT:
                    case BinaryFormats.INTEGER_32BIT:
                    case BinaryFormats.INTEGER_64BIT:
                    case BinaryFormats.INTEGER_8BIT:
                        // ToDo: enforce no-sign
                        writer.WriteAttributeString("type", "number");
                        scalarTypeFacets.TryWriteComparableFacetAttribues(writer);
                        break;
                    case BinaryFormats.NUMBER_BASE10:
                    case BinaryFormats.NUMBER_BASE16:
                    case BinaryFormats.NUMBER_BASE8:
                    case BinaryFormats.NUMBER_SINGLE:
                    case BinaryFormats.NUMBER_DOUBLE:
                        writer.WriteAttributeString("type", "number");
                        scalarTypeFacets.TryWriteComparableFacetAttribues(writer);
                        break;
                    case BinaryFormats.SIGNED_INTEGER:
                    case BinaryFormats.SIGNED_INTEGER_8BIT:
                    case BinaryFormats.SIGNED_INTEGER_16BIT:
                    case BinaryFormats.SIGNED_INTEGER_32BIT:
                    case BinaryFormats.SIGNED_INTEGER_64BIT:
                        // ToDo: enforce allow sign
                        scalarTypeFacets.TryWriteComparableFacetAttribues(writer);
                        writer.WriteAttributeString("type", "number");
                        break;
                    case BinaryFormats.ANY:
                        writer.WriteAttributeString("type", "text");
                        scalarTypeFacets.TryWriteLengthFacetAttribues(writer);
                        break;
                    case BinaryFormats.BASE16_ENCODING:
                        writer.WriteAttributeString("type", "text");
                        break;
                    case BinaryFormats.BASE64_ENCODING:
                        writer.WriteAttributeString("type", "text");
                        break;
                    case BinaryFormats.EMPTY:
                        writer.WriteAttributeString("type", "text");
                        break;
                    case BinaryFormats.NETWORK_ADDRESS:
                        writer.WriteAttributeString("type", "text");
                        break;
                    case BinaryFormats.QUALIFIED_NAME:
                        writer.WriteAttributeString("type", "text");
                        break;
                    case BinaryFormats.RESOURCE_ID:
                        if (scalarType.DataType.BinaryForm.HasBinaryDataFormat)
                        {
                            if (scalarType.DataType.BinaryForm.BinaryDataFormat == BinaryForm.ResourceIdHasUserInfoDataFormat)
                                writer.WriteAttributeString("type", "email");
                            else
                                writer.WriteAttributeString("type", "url");
                        }
                        else
                            writer.WriteAttributeString("type", "url");
                        break;
                    case BinaryFormats.UNICODE_ENCODING:
                        writer.WriteAttributeString("type", "text");
                        scalarTypeFacets.TryWriteLengthFacetAttribues(writer);
                        break;
                }

                writer.WriteAttributeString("name", inputName);

                if (scalarTypeFacets.TryGetPatternFacetsPattern(writer, out String patternFacetsValue))

                    writer.WriteAttributeString("pattern", patternFacetsValue);

                //ToDo:  LexicalValueExistenceFacet.Forbidden, LexicalValueExistenceFacet.Nillable;

                if (lexicalValueExistenceFacet == LexicalValueExistenceFacet.Required)

                    writer.WriteAttributeString("required", String.Empty);

                if (constantValue)

                    writer.WriteAttributeString("disabled", String.Empty);

                if (valueFacetValue != null)

                    writer.WriteAttributeString("value", valueFacetValue);

                writer.WriteEndElement();
            }
        }

        private static bool TryGetPatternFacetsPattern(this ScalarTypeFacets scalarTypeFacets, XmlWriter writer, out String patternFacetsValue)
        {
            if (scalarTypeFacets.PatternsSpecified)

                patternFacetsValue = scalarTypeFacets.Patterns.Flatten();

            else

                patternFacetsValue = null;

            return patternFacetsValue != null;
        }

        private static bool TryGetValueFacet(this ScalarTypeFacets scalarTypeFacets, out bool constantValue, out String valueFacetValue)
        {
            constantValue = false;

            if (scalarTypeFacets.ValueFacetSpecified)
            {
                switch (scalarTypeFacets.ValueFacet.Evaluates)
                {
                    case LexicalValueFacet.ValueConstraints.Constant:
                        valueFacetValue = scalarTypeFacets.ValueFacet.Representation.ToFormattedString();
                        constantValue = true;
                        break;
                    case LexicalValueFacet.ValueConstraints.Default:
                        valueFacetValue = scalarTypeFacets.ValueFacet.Representation.ToFormattedString();
                        break;
                    default:
                        valueFacetValue = null;
                        break;
                }
            }
            else
            {
                valueFacetValue = null;
            }

            return valueFacetValue != null;
        }

        private static LexicalValueExistenceFacet GetExistenceFacet(this ScalarTypeFacets scalarTypeFacets)
        {
            if (scalarTypeFacets.ValueExistenceFacetSpecified)
            {
                return scalarTypeFacets.ValueExistenceFacet;
            }

            return LexicalValueExistenceFacet.Optional;
        }

        private static void TryWriteLengthFacetAttribues(this ScalarTypeFacets scalarTypeFacets, XmlWriter writer)
        {
            if (scalarTypeFacets.LengthFacetsSpecified)
            {
                foreach (ScalarTypeLengthFacet scalarTypeLengthFacet in scalarTypeFacets.LengthFacets)
                {
                    switch (scalarTypeLengthFacet.Evaluates)
                    {
                        case ScalarTypeLengthFacet.LengthConstraints.Length:
                            writer.WriteAttributeString("minlength", scalarTypeLengthFacet.Representation.ToFormattedString());
                            writer.WriteAttributeString("maxlength", scalarTypeLengthFacet.Representation.ToFormattedString());
                            break;
                        case ScalarTypeLengthFacet.LengthConstraints.MinLength:
                            writer.WriteAttributeString("minlength", scalarTypeLengthFacet.Representation.ToFormattedString());
                            break;
                        case ScalarTypeLengthFacet.LengthConstraints.MaxLength:
                            writer.WriteAttributeString("maxlength", scalarTypeLengthFacet.Representation.ToFormattedString());
                            break;
                    }
                }
            }
        }

        private static void TryWriteComparableFacetAttribues(this ScalarTypeFacets scalarTypeFacets, XmlWriter writer)
        {
            if (scalarTypeFacets.ComparableFacetsSpecified)
            {
                foreach (ScalarTypeComparableFacet scalarTypeComparableFacet in scalarTypeFacets.ComparableFacets)
                {
                    //ToDo: calculate offsets for "exclusive"s ...

                    switch (scalarTypeComparableFacet.Evaluates)
                    {
                        case ScalarTypeComparableFacet.ComparableConstraints.MinInclusive:
                            writer.WriteAttributeString("min", scalarTypeComparableFacet.Representation.ToFormattedString());
                            break;
                        case ScalarTypeComparableFacet.ComparableConstraints.MinExclusive:
                            writer.WriteAttributeString("min", scalarTypeComparableFacet.Representation.ToFormattedString());
                            break;
                        case ScalarTypeComparableFacet.ComparableConstraints.MaxInclusive:
                            writer.WriteAttributeString("max", scalarTypeComparableFacet.Representation.ToFormattedString());
                            break;
                        case ScalarTypeComparableFacet.ComparableConstraints.MaxExclusive:
                            writer.WriteAttributeString("max", scalarTypeComparableFacet.Representation.ToFormattedString());
                            break;
                    }
                }
            }
        }
    }
}
