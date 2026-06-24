using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Yaml
{
    internal class XmlYamlReader : XmlBaseReader, IXmlTextReaderInitializer
    {
        internal class YamlScope
        {
            internal static readonly YamlScope NONE = new YamlScope(-1, 0, YamlScopeType.None, 0, YamlScopeType.None);

            private int offset;
            private int previousIndent;
            private YamlScopeType previousScopeType;
            private int indent;
            private YamlScopeType scopeType;

            public YamlScope(int offset, int previousIndent, YamlScopeType previousScopeType, int indent, YamlScopeType scopeType)
            {
                this.offset = offset;
                this.previousIndent = previousIndent;
                this.previousScopeType = previousScopeType;
                this.indent = indent;
                this.scopeType = scopeType;
            }

            public int Offset { get => offset; }
            public int PreviousIndent { get => previousIndent; }
            public YamlScopeType PreviousScopeType { get => previousScopeType; }
            public int Indent { get => indent; }
            public YamlScopeType ScopeType { get => scopeType; internal set => scopeType = value; }

            public void WriteScope(StringBuilder sb)
            {
                if (Indent > 0)
                    sb.Append(new String(Enumerable.Repeat(' ', Indent).ToArray()));
                sb.Append("Offset(");
                sb.Append(offset);
                sb.Append(')');
                sb.Append("Previous(");
                sb.Append(previousScopeType);
                sb.Append(',');
                sb.Append(previousIndent);
                sb.Append(')');
                sb.Append("Current(");
                sb.Append(scopeType);
                sb.Append(',');
                sb.Append(indent);
                sb.Append(')');
                sb.AppendLine();
            }
        }

        XmlDeclarationNode declarationNode;

        private YamlTextModes textMode = YamlTextModes.None;
        private bool buffered;
        private bool addedBlockSequenceElement;
        private int maxBytesPerRead;
        private OnXmlDictionaryReaderClose onReaderClose;
        private int scopeDepth;
        private YamlScope[] scopes;
        private XmlDictionary dictionary;
        private XmlDictionaryString versionString;
        private XmlDictionaryString blockSequenceString;
        private XmlDictionaryString flowSequenceString;
        private XmlDictionaryString blockMappingString;
        private XmlDictionaryString flowMappingString;
        private XmlDictionaryString anchorString;
        private XmlDictionaryString aliasString;
        private XmlDictionaryString literalString;
        private XmlDictionaryString foldedString;
        private PrefixHandle prefix;
        private StringHandle localName;

        public XmlYamlReader()
        {
            this.addedBlockSequenceElement = false;
            this.dictionary = new XmlDictionary();
            this.versionString = this.AddToDictionary(YamlTokens.VERSION);
            this.blockSequenceString = this.AddToDictionary(YamlTokens.BLOCK_SEQUENCE);
            this.flowSequenceString = this.AddToDictionary(YamlTokens.FLOW_SEQUENCE);
            this.blockMappingString = this.AddToDictionary(YamlTokens.BLOCK_MAPPING);
            this.flowMappingString = this.AddToDictionary(YamlTokens.FLOW_MAPPING);
            this.anchorString = this.AddToDictionary(YamlTokens.ANCHOR);
            this.aliasString = this.AddToDictionary(YamlTokens.ALIAS);
            this.literalString = this.AddToDictionary(YamlTokens.LITERAL);
            this.foldedString = this.AddToDictionary(YamlTokens.FOLDED);
            this.prefix = new PrefixHandle(base.BufferReader);
            this.localName = new StringHandle(base.BufferReader);
        }

        //private bool IsReadingCollection
        //{
        //    get
        //    {
        //        if (this.scopeDepth > 0)
        //        {
        //            YamlScopeType scopeType = this.scopes[this.scopeDepth].ScopeType;

        //            return scopeType == YamlScopeType.BlockSequence || scopeType == YamlScopeType.BlockMapping;
        //        }
        //        return false;
        //    }
        //}

        //private bool IsReadingSequence
        //{
        //    get
        //    {
        //        if (this.scopeDepth > 0)
        //        {
        //            YamlScope currentScope = this.scopes[this.scopeDepth - 1];

        //            return currentScope.ScopeType == YamlScopeType.SequenceItem || currentScope.PreviousScopeType == YamlScopeType.SequenceItem;
        //        }
        //        return false;
        //    }
        //}

        //private bool IsReadingMapping
        //{
        //    get
        //    {
        //        if (this.scopeDepth > 0)

        //            return this.scopes[this.scopeDepth-1].ScopeType == YamlScopeType.BlockMapping;

        //        return false;
        //    }
        //}

        private bool IsReadingText
        {
            get
            {
                if (!this.Node.IsAtomicValue)
                
                    return this.Node.NodeType == XmlNodeType.Text;

                return false;
            }
        }

        public void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            if (stream == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(stream));
            this.MoveToInitial(quotas, onClose);
            stream = (Stream)new YamlEncodingStreamWrapper(stream, encoding, true);
            this.BufferReader.SetBuffer(stream, this.dictionary, (XmlBinaryReaderSession)null);
            this.buffered = false;
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            if (buffer == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            if (offset < 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)new ArgumentOutOfRangeException(nameof(offset), PublicSR.Format(PublicSR.ValueMustBeNonNegative)));
            if (offset > buffer.Length)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)new ArgumentOutOfRangeException(nameof(offset), PublicSR.Format(PublicSR.JsonOffsetExceedsBufferSize, new object[1] { (object)buffer.Length })));
            if (count < 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)new ArgumentOutOfRangeException(nameof(count), PublicSR.Format(PublicSR.ValueMustBeNonNegative)));
            if (count > buffer.Length - offset)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)new ArgumentOutOfRangeException(nameof(count), PublicSR.Format(PublicSR.JsonSizeExceedsRemainingBufferSpace, new object[1] { (object)(buffer.Length - offset) })));
            this.MoveToInitial(quotas, onClose);
            ArraySegment<byte> arraySegment = YamlEncodingStreamWrapper.ProcessBuffer(buffer, offset, count, encoding);
            this.BufferReader.SetBuffer(arraySegment.Array, arraySegment.Offset, arraySegment.Count, this.dictionary, (XmlBinaryReaderSession)null);
            this.buffered = true;
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            throw new NotImplementedException();
        }

        protected new XmlBaseReader.XmlDeclarationNode MoveToDeclaration()
        {
            //if (base.AttrCount < 1)
            //    XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.GetString("XmlDeclMissingVersion")));
            //if (this.AttrCount > 1)
            //    XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.GetString("XmlMalformedDecl")));
            //if (!base.CheckDeclAttribute(0, YamlTokens.VERSION, "1.2", false, "XmlInvalidVersion"))
            //    XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.GetString("XmlDeclMissingVersion")));
            //if (base.DeclarationNode == null)
            //    base.DeclarationNode = new XmlBaseReader.XmlDeclarationNode(base.BufferReader);
            //this.MoveToNode((XmlBaseReader.XmlNode)base.DeclarationNode);
            //return base.DeclarationNode;
            if (base.AttributeCount < 1)
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.XmlDeclMissingVersion));
            if (this.AttributeCount > 1)
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.XmlMalformedDecl));
            if (!CheckDeclAttribute(YamlTokens.VERSION, "1.2", PublicSR.XmlInvalidVersion))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.XmlDeclMissingVersion));
            if (declarationNode == null)
            {
                declarationNode = new XmlDeclarationNode(base.BufferReader);
            }
            MoveToNode(declarationNode);
            return declarationNode;

        }

        bool CheckDeclAttribute(string localName, string value, string valueSR)
        {
            String nodeValue = base.GetAttribute(localName);

            if (String.IsNullOrWhiteSpace(nodeValue))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.XmlMalformedDecl));

            if (value != null && !nodeValue.Equals(value, StringComparison.InvariantCultureIgnoreCase))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(valueSR));

            return true;
        }

        public override bool Read()
        {
            if (this.TryNavigate(out bool @continue))

                return @continue;

            if (!this.buffered)
            
                base.BufferReader.SetWindow(base.ElementNode.BufferOffset, this.maxBytesPerRead);

            if (this.OutsideRootElement)
            {
                if (!this.buffered)

                    this.BufferElement();

                this.SkipWhitespaceInBufferReader(out int lines, out int indent);

                byte @byte = base.BufferReader.GetByte();

                if (@byte == EncodingChars.UTF8_BOM_1 && this.IsUTF8_BOM(base.BufferReader.Buffer, base.BufferReader.Offset))
                {
                    base.BufferReader.Advance(3);

                    @byte = base.BufferReader.GetByte();
                }

                if (@byte == PrintableChars.PERCENT_SIGN)
                {
                    if (!this.ReadDeclaration() && !this.ReadTagDirective())
                        
                        throw new Exception();
                    
                    return true;
                }

                if (@byte == PrintableChars.HYPHEN && this.IsStartDocument(base.BufferReader.Buffer, base.BufferReader.Offset))
                {
                    this.SkipExpectedByteInBufferReader(PrintableChars.HYPHEN);
                    this.SkipExpectedByteInBufferReader(PrintableChars.HYPHEN);
                    this.SkipExpectedByteInBufferReader(PrintableChars.HYPHEN);
                    this.SkipWhitespaceInBufferReader(out lines, out indent);
                }

                if (this.scopeDepth == 0)
                {
                    this.ReadNonExistentElementName(StringHandleConstStringType.Root, 0, 0, YamlScopeType.BlockMapping);

                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Object);
                }
            }
            else
            {
                if (this.AtYamlScopeIndented(out int? previousIndentOffset, out int? previousIndent, out YamlScopeType previousScopeType))
                {
                    bool flag = false;

                    if (previousIndentOffset == 0)
                    {
                        YamlScope indentScope;

                        switch (previousScopeType)
                        {
                            case YamlScopeType.SequenceItem:

                                if (this.TryRemoveScope(YamlScopeType.Indent, out indentScope))
                                {
                                    TryExitYamlScope(); // remove SequenceItem scope

                                    YamlScope sequenceParentScope = CurrentYamlScope;

                                    if (sequenceParentScope.ScopeType == YamlScopeType.BlockSequence)
                                    {
                                        int offset, maxOffset;

                                        byte[] buffer = base.BufferReader.GetBuffer(2, out offset, out maxOffset);

                                        byte @byte = buffer[offset];

                                        if (@byte == PrintableChars.HYPHEN && IsWhitespace(buffer[offset + 1]))
                                        {
                                            //System.Diagnostics.Debugger.Break(); // Need this block?

                                            base.BufferReader.SkipByte();
                                            base.BufferReader.SkipByte();

                                            this.AddSequenceItem(indentScope.Offset, indentScope.Indent, indentScope.Offset, indentScope.Indent + 2);
                                        }
                                        else
                                        {
                                            TryExitYamlScope();

                                            EnterYamlScope(indentScope.Offset, indentScope.Indent, indentScope.ScopeType);

                                            if (this.Depth > 0)

                                                this.MoveToEndElement();

                                            else

                                                throw new InvalidOperationException("Missing expected EndElement");
                                        }
                                    }
                                    else if (sequenceParentScope.ScopeType != YamlScopeType.FlowSequence)
                                    {
                                        throw new InvalidOperationException($"Unexpected scope {sequenceParentScope.ScopeType}.  Expected 'sequence'.");
                                    }
                                }
                                else

                                    flag = true;

                                break;

                            case YamlScopeType.BlockSequence:

                                if (this.TryRemoveScope(YamlScopeType.Indent, out indentScope))
                                {
                                    int offset, maxOffset;

                                    byte[] buffer = base.BufferReader.GetBuffer(2, out offset, out maxOffset);

                                    byte @byte = buffer[offset];

                                    if (@byte == PrintableChars.HYPHEN && IsWhitespace(buffer[offset + 1]))
                                    {
                                        System.Diagnostics.Debugger.Break(); // Need this block?

                                        base.BufferReader.SkipByte();
                                        base.BufferReader.SkipByte();

                                        this.AddSequenceItem(indentScope.Offset, indentScope.Indent, indentScope.Offset, indentScope.Indent + 2);
                                    }
                                    else
                                    {
                                        TryExitYamlScope();

                                        EnterYamlScope(indentScope.Offset, indentScope.Indent, indentScope.ScopeType);

                                        flag = true;
                                    }
                                }
                                else

                                    flag = true;

                                break;

                            default:

                                flag = true;

                                break;

                        }
                    }
                    else if (previousIndentOffset < 0)
                    {
                        if (this.TryRemoveScope(YamlScopeType.Indent, out YamlScope indentScope))
                        {
                            switch (previousScopeType)
                            {
                                //case YamlScopeType.BlockSequence:
                                case YamlScopeType.SequenceItem:

                                    int offset, maxOffset;

                                    byte[] buffer = base.BufferReader.GetBuffer(2, out offset, out maxOffset);

                                    byte @byte = buffer[offset];

                                    if (@byte == PrintableChars.HYPHEN && IsWhitespace(buffer[offset + 1]))
                                    {
                                        System.Diagnostics.Debugger.Break(); // Need this block?

                                        base.BufferReader.SkipByte();
                                        base.BufferReader.SkipByte();

                                        this.AddSequenceItem(indentScope.Offset, indentScope.Indent, indentScope.Offset, indentScope.Indent + 2);
                                    }
                                    else
                                    {
                                        TryExitYamlScope();

                                        EnterYamlScope(indentScope.Offset, indentScope.Indent, indentScope.ScopeType);

                                        //flag = true;
                                    }

                                    break;

                                //case YamlScopeType.FlowSequence:

                                //break;

                                default:

                                    this.TryTransitionToIndentScope(indentScope.Indent);

                                    break;
                            }
                        }

                        if (this.Depth > 0)

                            this.MoveToEndElement();

                        else

                            flag = true;
                    }
                    else

                        flag = true;

                    if (flag) 
                    {
                        if (this.MoveToAttribute(YamlTokens.LITERAL) || this.MoveToAttribute(YamlTokens.FOLDED))
                        {
                            string textTyle = this.Name;

                            this.MoveToElement();

                            this.ReadMultiLineText(textTyle, out int lines, out int indent);

                            if (lines > 0)
                            {
                                this.TryRemoveScope(YamlScopeType.Indent, out YamlScope indentScope);

                                this.TryTransitionToIndentScope(indent);
                            }
                        }
                        else if (this.addedBlockSequenceElement)
                        {
                            this.addedBlockSequenceElement = false;

                            if (this.TryRemoveScope(YamlScopeType.Indent, out YamlScope indentScope))
                            {
                                if (indentScope.PreviousScopeType == YamlScopeType.SequenceItem)
                                {
                                    YamlScopeType currentScopeType = this.CurrentYamlScope.ScopeType;

                                    if (currentScopeType == YamlScopeType.BlockSequence)

                                        this.AddSequenceItem(indentScope.Offset, indentScope.PreviousIndent, indentScope.Offset, indentScope.Indent);

                                    else

                                        throw new InvalidOperationException(String.Format("Unexpected current scope {0}; expected {1}.", currentScopeType, YamlScopeType.BlockSequence));
                                }
                                else

                                    throw new InvalidOperationException(String.Format("Unexpected parent scope {0}; expected {1}.", indentScope.PreviousScopeType, YamlScopeType.SequenceItem));
                            }
                            else
                            {
                                throw new InvalidOperationException("Unexpected scope encountered.  Expected indent scope after block sequence element.");
                            }
                        }
                        else if (this.IsNextByteInBufferReader(PrintableChars.NUMBER_SIGN))
                        {
                            this.ReadComment();

                            this.TryTransitionToIndentScope();
                        }
                        else if (this.HasByteInBufferReaderFollowedByWhitespace(PrintableChars.COLON) || this.HasTagHandle())
                        {
                            this.ParseStartElement();
                        }
                        else
                        {
                            this.ReadMultiLineText(YamlTokens.PLAIN, out int lines, out int indent);

                            if (lines > 0)
                            {
                                this.TryRemoveScope(YamlScopeType.Indent, out YamlScope indentScope);

                                this.TryTransitionToIndentScope(indent);
                            }
                        }
                    }
                }
                else
                {
                    YamlScope currentScope = this.CurrentYamlScope;

                    bool readAtomicText = false;

                    if (currentScope.PreviousScopeType == YamlScopeType.FlowSequence)
                    {
                        if (currentScope.ScopeType == YamlScopeType.SequenceItem)
                        {
                            if (this.NodeType == XmlNodeType.Element)
                            {
                                byte @byte = base.BufferReader.GetByte();

                                if (IsInlineWhitespace(@byte))
                                {
                                    this.SkipWhitespaceInBufferReader();

                                    @byte = base.BufferReader.GetByte();
                                }

                                if (@byte == PrintableChars.COMMA)
                                {
                                    this.BufferReader.SkipByte();
                                }

                                readAtomicText = true;
                            }
                            else if (this.NodeType == XmlNodeType.EndElement)
                            {
                                this.TryExitYamlScope();

                                currentScope = this.CurrentYamlScope;
                            }
                        }
                    }

                    if (currentScope.ScopeType == YamlScopeType.FlowSequence)
                    {
                        byte @byte = base.BufferReader.GetByte();

                        if (IsInlineWhitespace(@byte))
                        {
                            this.SkipWhitespaceInBufferReader();

                            @byte = base.BufferReader.GetByte();
                        }

                        if (@byte == PrintableChars.CLOSING_BRACKET)
                        {
                            this.BufferReader.SkipByte();

                            this.SkipWhitespaceInBufferReader(out int lines, out int indent);

                            if (lines > 0)

                                this.TryTransitionToIndentScope(indent);

                            this.MoveToEndElement();
                        }
                        else

                            this.AddSequenceItem(base.BufferReader.Offset, currentScope.Indent);
                    }
                    else
                    {
                        byte @byte = base.BufferReader.GetByte();

                        if (IsInlineWhitespace(@byte))
                        {
                            this.SkipWhitespaceInBufferReader();

                            @byte = base.BufferReader.GetByte();
                        }

                        if (@byte == PrintableChars.NUMBER_SIGN)
                        {
                            this.ReadComment();

                            this.TryTransitionToIndentScope();
                        }
                        else if (@byte == EncodingChars.UTF8_BOM_1)
                        {
                            this.ReadText(true);

                            this.TryTransitionToIndentScope();
                        }
                        else if (@byte == PrintableChars.AMPERSAND)
                        {
                            this.ReadEscapedText();

                            this.TryTransitionToIndentScope();
                        }
                        else if (@byte == PrintableChars.SINGLE_QUOTE)
                        {
                            this.ReadSingleQuotedText(true);

                            this.TryTransitionToIndentScope();
                        }
                        else if (@byte == PrintableChars.DOUBLE_QUOTE)
                        {
                            this.ReadDoubleQuotedText(true);

                            this.TryTransitionToIndentScope();
                        }
                        else if (@byte.IsTextChar())
                        {
                            if (readAtomicText)

                                this.ReadPlainText(true);

                            else

                                this.ReadText(false);

                            this.TryTransitionToIndentScope();
                        }
                        else if (@byte == ControlChars.LINE_FEED)
                        {
                            base.BufferReader.SkipByte();
                            base.MoveToComplexText().Value.SetCharValue(ControlChars.LINE_FEED);
                        }
                        else if (@byte == ControlChars.CARRIAGE_RETURN)
                        {
                            base.BufferReader.SkipByte();
                            if (base.BufferReader.EndOfFile)
                            {
                                base.MoveToComplexText().Value.SetCharValue(ControlChars.LINE_FEED);
                            }
                            else if (base.BufferReader.GetByte() == ControlChars.LINE_FEED)
                            {
                                base.BufferReader.SkipByte();
                                base.MoveToComplexText().Value.SetCharValue(ControlChars.LINE_FEED);
                            }
                        }
                        else
                        {
                            XmlExceptionHelper.ThrowInvalidXml(this, @byte);
                        }
                    }
                }
            }

            return true;
        }

        private bool TryNavigate(out bool @continue)
        {
            if (base.Node.ReadState == ReadState.Closed)
            {
                @continue = false;

                return true;
            }

            if (base.Node.CanMoveToElement)
            {
                this.MoveToElement();
            }

            base.SignNode();

            if (base.Node.ExitScope)
            {
                base.ExitScope();
            }
            else if (base.Node.IsAtomicValue)
            {
                this.MoveToEndElement();

                @continue = true;

                return true;
            }
            else if (this.IsReadingText)
            {
                this.MoveToEndElement();

                @continue = true;

                return true;
            }

            if (base.BufferReader.EndOfFile)
            {
                this.TryRemoveScope(YamlScopeType.Indent, out YamlScope removedScope);

                if (this.TryExitYamlScope() != YamlScopeType.None)
                {
                    this.MoveToEndElement();

                    @continue = true;

                    return true;
                }
                else
                {
                    base.MoveToEndOfFile();

                    @continue = false;

                    return true;
                }
            }
            else if (this.OutsideRootElement)
            {
                if (this.NodeType == XmlNodeType.EndElement && this.Name == "root")
                {
                    base.MoveToEndOfFile();

                    @continue = true;

                    return true;
                }

                if (this.NodeType == XmlNodeType.None)
                {
                    this.MoveToInitial(this.Quotas);
                }
            }
            else if (this.IsStartDocument(base.BufferReader.Buffer, base.BufferReader.Offset))
            {
                if (this.scopeDepth > 0)
                {
                    this.TryRemoveScope(YamlScopeType.Indent, out YamlScope removedScope);

                    if (this.TryExitYamlScope() != YamlScopeType.None)
                    {
                        this.MoveToEndElement();

                        @continue = true;

                        return true;
                    }
                    else

                        throw new InvalidOperationException(String.Format("Unexpected scope {0}", YamlScopeType.None));
                }

                this.SkipExpectedByteInBufferReader(PrintableChars.HYPHEN);
                this.SkipExpectedByteInBufferReader(PrintableChars.HYPHEN);
                this.SkipExpectedByteInBufferReader(PrintableChars.HYPHEN);
                this.SkipWhitespaceInBufferReader(out int lines, out int indent);
                this.MoveToInitial(this.Quotas);

                if (this.scopeDepth == 0)
                {
                    this.ReadNonExistentElementName(StringHandleConstStringType.Root, 0, 0, YamlScopeType.BlockMapping);

                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Object);
                }

                @continue = true;

                return true;
            }

            @continue = false;

            return false;
        }

        private void AddSequenceItem(int itemOffset, int itemIndent)
        {
            EnterYamlScope(itemOffset, itemIndent, YamlScopeType.SequenceItem);

            XmlBaseReader.XmlElementNode elementNode = this.EnterScope();

            this.SetItemNameWithMapping(elementNode);

            elementNode.BufferOffset = base.BufferReader.Offset;
        }

        private void AddSequenceItem(int itemOffset, int itemIndent, int indentScopeOffset, int indentScopeIndent)
        {
            EnterYamlScope(itemOffset, itemIndent, YamlScopeType.SequenceItem);

            XmlBaseReader.XmlElementNode elementNode = this.EnterScope();

            this.SetItemNameWithMapping(elementNode);

            elementNode.BufferOffset = base.BufferReader.Offset;

            EnterYamlScope(indentScopeOffset, indentScopeIndent, YamlScopeType.Indent);
        }

        private XmlDictionaryString AddToDictionary(String value)
        {
            XmlDictionaryString @string = this.dictionary.Add(value);

            return new XmlDictionaryString(this.dictionary, value, @string.Key << 1);
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            this.MoveToInitial(quotas);
            this.onReaderClose = onClose;
        }

        protected new void MoveToInitial(XmlDictionaryReaderQuotas quotas)
        {
            this.scopeDepth = 0;
            this.scopes = new YamlScope[4];
            base.MoveToInitial(quotas);
            this.maxBytesPerRead = quotas.MaxBytesPerRead;
        }

        private void ReadNonExistentElementName(StringHandleConstStringType elementName, int offset, int indent, YamlScopeType elementScopeType)
        {
            XmlBaseReader.XmlElementNode xmlElementNode = this.EnterScope();
            xmlElementNode.LocalName.SetConstantValue(elementName);
            xmlElementNode.Namespace.Uri.SetValue(xmlElementNode.NameOffset, 0);
            xmlElementNode.Prefix.SetValue(PrefixHandleType.Empty);
            xmlElementNode.BufferOffset = this.BufferReader.Offset;
            xmlElementNode.IsEmptyElement = false;
            xmlElementNode.ExitScope = false;
            switch (elementScopeType)
            {
                case YamlScopeType.BlockMapping:
                    this.ReadNonExistentAttribute(this.blockMappingString);
                    this.EnterYamlScope(offset, indent, elementScopeType);
                    this.EnterYamlScope(base.BufferReader.Offset, indent, YamlScopeType.Indent);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool HasTagHandle()
        {
            int offset, offsetMax;
            bool escaped;

            byte[] buffer;

            buffer = this.BufferReader.GetBuffer(out offset, out offsetMax);

            int num = this.CountBytesBeforeByte(buffer, offset, offsetMax, PrintableChars.EXCLAMATION_POINT, out escaped);

            if (num < 0)

                return false;

            return true;
        }

        private Namespace ReadTagHandle()
        {
            //!<tag:clarkevans.com,2002:invoice>   
            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);
            this.SkipExpectedByteInBufferReader(PrintableChars.LESS_THAN);
            int offset;
            byte[] buffer = base.BufferReader.GetBuffer(4, out offset);
            //if (buffer[offset + 0] != PrintableChars.t || buffer[offset + 1] != PrintableChars.a || buffer[offset + 2] != PrintableChars.g || buffer[offset + 3] != PrintableChars.COLON || (XmlYamlReader.charTypesMap[buffer[offset + 4]] & XmlCharMasks.WhiteSpace) == 0)
            //    return false;
            //base.BufferReader.Advance(4);
            int offsetMax;
            buffer = this.BufferReader.GetBuffer(2048, out offset, out offsetMax);
            int whiteSpaceAtEnd;
            bool escaped;
            int lengthUntilGT = this.CountBytesBeforeByte(buffer, offset, offsetMax, PrintableChars.GREATER_THAN, out whiteSpaceAtEnd, out escaped);
            if (lengthUntilGT < 0)
                XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)PrintableChars.GREATER_THAN).ToString(), "End of line");
            Namespace ns = this.AddNamespace();
            ns.Uri.SetValue(offset, lengthUntilGT - whiteSpaceAtEnd);
            ns.Prefix.SetValue(PrefixHandleType.Empty);
            base.BufferReader.Advance(lengthUntilGT);
            this.SkipExpectedByteInBufferReader(PrintableChars.GREATER_THAN);
            return ns;
        }

        private bool ReadTagDirective()
        {
            //%TAG !yaml! tag:yaml.org,2002:
            int offset;

            byte[] buffer = base.BufferReader.GetBuffer(5, out offset);

            if (buffer[offset + 0] != PrintableChars.PERCENT_SIGN || buffer[offset + 1] != PrintableChars.T || buffer[offset + 2] != PrintableChars.A || buffer[offset + 3] != PrintableChars.G || buffer[offset + 4].IsWhiteSpaceChar())
            
                return false;

            base.BufferReader.Advance(5);

            offset = this.ReadInlineWhitespace();

            Namespace ns = null;

            byte @byte = base.BufferReader.GetByte();

            if (@byte == PrintableChars.EXCLAMATION_POINT)
            {
                ns = this.AddNamespace();

                switch (@byte)
                {
                    case PrintableChars.SPACE:

                        ReadPrimaryTagPrefix(ns.Prefix);

                        break;

                    case PrintableChars.EXCLAMATION_POINT:

                        ReadSecondaryTagPrefix(ns.Prefix);

                        break;

                    default:

                        ReadNamedTagPrefix(ns.Prefix);

                        break;
                }
            }

            if (ns != null)
            {
                this.ReadInlineWhitespace();

                @byte = base.BufferReader.GetByte();

                switch (@byte)
                {
                    case PrintableChars.EXCLAMATION_POINT:

                        ReadLocalTag(ns.Uri);

                        break;

                    default:

                        ReadGlobalTag(ns.Uri);

                        break;
                }

                return true;
            }

            return false;
        }

        private void ReadPrimaryTagPrefix(PrefixHandle prefixHandle)
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);

            prefixHandle.SetValue((PrefixHandleType.Empty));
        }

        private void ReadSecondaryTagPrefix(PrefixHandle prefixHandle)
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);
            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);

            prefixHandle.SetValue(PrefixHandleType.Y);
        }

        private void ReadNamedTagPrefix(PrefixHandle prefixHandle)
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);

            int offset, offsetMax;
            byte[] buffer = this.BufferReader.GetBuffer(out offset, out offsetMax);
            int whiteSpaceBeforeEndByte;
            bool escaped;
            int numBytes = this.CountBytesBeforeByte(buffer, offset, offsetMax, PrintableChars.EXCLAMATION_POINT, out whiteSpaceBeforeEndByte, out escaped);
            if (numBytes < 0)
                XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)PrintableChars.EXCLAMATION_POINT).ToString(), "End of line");

            prefixHandle.SetValue(offset, numBytes);

            base.BufferReader.Advance(numBytes);

            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);
        }

        private void ReadLocalTag(StringHandle uriHandle)
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.EXCLAMATION_POINT);

            int offset, offsetMax;

            byte[] buffer = this.BufferReader.GetBuffer(out offset, out offsetMax);

            bool escaped;

            int numBytes = this.CountBytesUntilWhiteSpaceByte(buffer, offset, offsetMax, out escaped);
                
            uriHandle.SetValue(offset, numBytes);

            base.BufferReader.Advance(numBytes);
        }

        private void ReadGlobalTag(StringHandle uriHandle)
        {
            int offset, offsetMax;

            byte[] buffer = this.BufferReader.GetBuffer(out offset, out offsetMax);

            bool escaped;

            int numBytes = this.CountBytesUntilWhiteSpaceByte(buffer, offset, offsetMax, out escaped);

            uriHandle.SetValue(offset, numBytes);

            base.BufferReader.Advance(numBytes);
        }

        private bool ReadDeclaration()
        {
            if (base.Node.ReadState != ReadState.Initial || this.scopeDepth > 0)
            {
                XmlExceptionHelper.ThrowDeclarationNotFirst(this);
            }
            if (!this.buffered)
            {
                this.BufferElement();
            }
            int offset;
            byte[] buffer = base.BufferReader.GetBuffer(6, out offset);
            if (buffer[offset + 0] != PrintableChars.PERCENT_SIGN || buffer[offset + 1] != PrintableChars.Y || buffer[offset + 2] != PrintableChars.A || buffer[offset + 3] != PrintableChars.M || buffer[offset + 4] != PrintableChars.L || !buffer[offset + 5].IsWhiteSpaceChar())
                return false;
            this.EnterYamlScope(offset, 0, YamlScopeType.Directive);
            this.SkipExpectedByteInBufferReader(PrintableChars.PERCENT_SIGN);
            offset = base.BufferReader.Offset;
            base.BufferReader.Advance(4);
            int lines, indent;
            this.SkipWhitespaceInBufferReader(out lines, out indent);
            this.ReadAttribute(versionString);
            if (lines == 0)
                this.SkipWhitespaceInBufferReader(out lines, out indent);
            XmlBaseReader.XmlDeclarationNode xmlDeclarationNode = this.MoveToDeclaration();
            xmlDeclarationNode.LocalName.SetValue(offset, 4);
            this.TryExitYamlScope();
            return true;
        }

        private void ReadComment()
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.NUMBER_SIGN);
            int num;
            int num2;
            byte[] buffer;
            int num3;
            if (this.buffered)
            {
                buffer = base.BufferReader.GetBuffer(out num, out num2);

                num3 = this.ReadText(buffer, num, num2);
            }
            else
            {
                buffer = base.BufferReader.GetBuffer(2048, out num, out num2);

                num3 = this.ReadText(buffer, num, num2);

                num3 = this.BreakText(buffer, num, num3);
            }

            base.MoveToComment().Value.SetValue(ValueHandleType.UTF8, num, num3);

            base.BufferReader.Advance(num3);
        }

        private int ReadCharRef()
        {
            int offset = base.BufferReader.Offset;
            base.BufferReader.SkipByte();
            while (base.BufferReader.GetByte() != PrintableChars.SEMI_COLON)
            {
                base.BufferReader.SkipByte();
            }
            base.BufferReader.SkipByte();
            int num = base.BufferReader.Offset - offset;
            base.BufferReader.Offset = offset;
            int charEntity = base.BufferReader.GetCharEntity(offset, num);
            base.BufferReader.Advance(num);
            return charEntity;
        }

        private bool IsWhitespace(byte @byte)
        {
            return @byte.IsWhiteSpaceChar();
        }

        private bool IsInlineWhitespace(byte @byte)
        {
            return @byte.IsInlineWhtespaceChar();
        }

        private int ReadInlineWhitespace()
        {
            int offset;
            int num;
            if (this.buffered)
            {
                int offsetMax;
                byte[] buffer = base.BufferReader.GetBuffer(out offset, out offsetMax);
                num = this.ReadInlineWhitespace(buffer, offset, offsetMax);
            }
            else
            {
                int offsetMax;
                byte[] buffer = base.BufferReader.GetBuffer(2048, out offset, out offsetMax);
                num = this.ReadInlineWhitespace(buffer, offset, offsetMax);
                num = this.BreakText(buffer, offset, num);
            }
            base.BufferReader.Advance(num);
            base.MoveToWhitespaceText().Value.SetValue(ValueHandleType.UTF8, offset, num);

            return num;
        }

        private int ReadInlineWhitespace(byte[] buffer, int offset, int offsetMax)
        {
            int num = offset;
            while (offset < offsetMax && buffer[offset].IsInlineWhtespaceChar())
            {
                offset++;
            }
            return offset - num;
        }

        private int BreakText(byte[] buffer, int offset, int length)
        {
            if (length > 0 && (buffer[offset + length - 1] & 128) == 128)
            {
                int num = length;
                do
                {
                    length--;
                }
                while (length > 0 && (buffer[offset + length] & 192) != 192);
                if (length == 0)
                {
                    return num;
                }
                byte b = (byte)(buffer[offset + length] << 2);
                int num2 = 2;
                while ((b & 128) == 128)
                {
                    b = (byte)(b << 1);
                    num2++;
                    if (num2 > 4)
                    {
                        return num;
                    }
                }
                if (length + num2 == num)
                {
                    return num;
                }
                if (length == 0)
                {
                    return num;
                }
            }
            return length;
        }

        private int ReadAttributeText(byte[] buffer, int offset, int offsetMax)
        {
            int num = offset;
            while (offset < offsetMax && buffer[offset].IsAttributeTextChar())
            {
                offset++;
            }
            return offset - num;
        }

        private int ReadText(byte[] buffer, int offset, int offsetMax)
        {
            int num = offset;
            while (offset < offsetMax && buffer[offset].IsTextChar())
            {
                offset++;
            }
            return offset - num;
        }

        private int ReadPlainText(byte[] buffer, int offset, int offsetMax, out int leadingWhitespace, out int trailingWhitespace)
        {
            leadingWhitespace = 0;
            trailingWhitespace = -1;
            int num = offset;
            bool forbiddenPunctuation = false;
            while (!forbiddenPunctuation && offset < offsetMax && buffer[offset].IsMultilineTextChar())
            {
                if (buffer[offset].IsWhiteSpaceChar())
                {
                    if (trailingWhitespace < 0)
                        leadingWhitespace++;
                    else
                        trailingWhitespace++;
                    offset++;
                }
                else
                {
                    switch (buffer[offset])
                    {
                        case PrintableChars.NUMBER_SIGN:
                        case PrintableChars.OPENING_BRACE:
                        case PrintableChars.CLOSING_BRACE:
                        case PrintableChars.OPENING_BRACKET:
                        case PrintableChars.CLOSING_BRACKET:
                        case PrintableChars.COMMA:
                            forbiddenPunctuation = true;
                            break;
                        default:
                            trailingWhitespace = 0;
                            offset++;
                            break;
                    }
                }
            }
            return offset - num;
        }

        private void ReadEscapedText()
        {
            int num = this.ReadCharRef();
            if (num < 256 && ((byte)num).IsWhiteSpaceChar())
            {
                base.MoveToWhitespaceText().Value.SetCharValue(num);
                return;
            }
            base.MoveToComplexText().Value.SetCharValue(num);
        }

        private int ReadTextAndWatchForInvalidCharacters(byte[] buffer, int offset, int offsetMax)
        {
            int num = offset;
            while (offset < offsetMax && buffer[offset].IsTextChar() || buffer[offset] == EncodingChars.UTF8_BOM_1)
            {
                if (buffer[offset] != EncodingChars.UTF8_BOM_1)
                {
                    offset++;
                }
                else if (offset + 2 < offsetMax)
                {
                    if (!this.IsNextCharacterFFFFOrFFFE(buffer, offset))
                    {
                        offset += 3;
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.XmlInvalidFFFE));
                    }
                }
                else
                {
                    if (base.BufferReader.Offset < offset)
                    {
                        break;
                    }
                    int num2;
                    base.BufferReader.GetBuffer(3, out num2);
                }
            }
            return offset - num;
        }

        private void ReadText(bool hasLeadingByteOf0xEF)
        {
            int num;
            int num2;
            byte[] buffer;
            int num3;
            if (this.buffered)
            {
                buffer = base.BufferReader.GetBuffer(out num, out num2);
                if (hasLeadingByteOf0xEF)
                {
                    num3 = this.ReadTextAndWatchForInvalidCharacters(buffer, num, num2);
                }
                else
                {
                    num3 = this.ReadText(buffer, num, num2);
                }
            }
            else
            {
                buffer = base.BufferReader.GetBuffer(2048, out num, out num2);
                if (hasLeadingByteOf0xEF)
                {
                    num3 = this.ReadTextAndWatchForInvalidCharacters(buffer, num, num2);
                }
                else
                {
                    num3 = this.ReadText(buffer, num, num2);
                }
                num3 = this.BreakText(buffer, num, num3);
            }
            base.BufferReader.Advance(num3);
            if (num < num2 - 1 - num3 && buffer[num + num3] == PrintableChars.LESS_THAN && buffer[num + num3 + 1] != PrintableChars.EXCLAMATION_POINT)
            {
                base.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, num, num3);
                return;
            }
            base.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, num, num3);
        }

        private void SetItemNameWithMapping(XmlBaseReader.XmlElementNode elementNode)
        {
            XmlBaseReader.Namespace ns = this.AddNamespace();
            ns.Prefix.SetValue(PrefixHandleType.A);
            ns.Uri.SetConstantValue(StringHandleConstStringType.Item);
            this.AddXmlnsAttribute(ns);
            XmlBaseReader.XmlAttributeNode xmlAttributeNode = this.AddAttribute();
            xmlAttributeNode.LocalName.SetConstantValue(StringHandleConstStringType.Item);
            xmlAttributeNode.Namespace.Uri.SetValue(0, 0);
            xmlAttributeNode.Prefix.SetValue(PrefixHandleType.Empty);
            xmlAttributeNode.Value.SetValue(ValueHandleType.UTF8, elementNode.NameOffset, elementNode.NameLength);
            elementNode.NameLength = 0;
            elementNode.Prefix.SetValue(PrefixHandleType.A);
            elementNode.LocalName.SetConstantValue(StringHandleConstStringType.Item);
            elementNode.Namespace = ns;
        }

        private void SkipExpectedByteInBufferReader(byte characterToSkip)
        {
            if ((int)this.BufferReader.GetByte() != (int)characterToSkip)
                XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)characterToSkip).ToString(), (char)this.BufferReader.GetByte());
            this.BufferReader.SkipByte();
        }

        private int SkipWhitespaceInBufferReader()
        {
            int charsSkipped = 0;
            byte ch;
            while (this.TryGetByte(out ch) && this.IsWhitespace(ch))
            {
                charsSkipped++;
                this.BufferReader.SkipByte();
            }
            return charsSkipped;
        }

        private void SkipWhitespaceInBufferReader(out int lines, out int indent)
        {
            lines = 0;
            indent = 0;
            while (!base.BufferReader.EndOfFile)
            {
                byte @byte = base.BufferReader.GetByte();
                if (this.IsWhitespace(@byte))
                {
                    base.BufferReader.SkipByte();
                    if (@byte == ControlChars.LINE_FEED)
                    {
                        lines++;
                        indent = 0;
                    }
                    else if (@byte == ControlChars.CARRIAGE_RETURN)
                    {
                        byte b = base.BufferReader.GetByte();
                        if (b == ControlChars.LINE_FEED)
                        {
                            base.BufferReader.SkipByte();
                        }
                        lines++;
                        indent = 0;
                    }
                    else if (lines > 0)
                        indent++;
                }
                else
                    break;
            }

            if (base.BufferReader.EndOfFile)
            {
                lines++;
                indent = 0;
            }
        }

        private bool TryGetByte(out byte ch)
        {
            int offset;
            int offsetMax;
            byte[] buffer = this.BufferReader.GetBuffer(1, out offset, out offsetMax);
            if (offset < offsetMax)
            {
                ch = buffer[offset];
                return true;
            }
            ch = (byte)0;
            return false;
        }

        private void ParseStartElement()
        {
            if (this.TryRemoveScope(YamlScopeType.Indent, out YamlScope indentScope))
            {
                if (indentScope.PreviousScopeType == YamlScopeType.BlockSequence)

                    TryExitYamlScope();
            }
            else
            {
                indentScope = CurrentYamlScope;

                if (indentScope.ScopeType != YamlScopeType.SequenceItem)

                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(AMMMYR.YamlElementMissingPreceedingIndent));
            }

            if (!this.buffered)

                this.BufferElement();

            this.ParseAndSetLocalName();

            this.SkipWhitespaceInBufferReader(out int lines, out int indent);

            bool possibleEmptyElement = false;

            if (lines == 0)

                indent = indentScope.Indent;

            else if (indent <= indentScope.Indent) // lines > 0

                possibleEmptyElement = true;

            if (!this.ReadAttributeAndTryEnterScope(indentScope.Indent, possibleEmptyElement, out int consumedBytes))
            {
                this.SkipWhitespaceInBufferReader(out lines, out indent);

                if (lines == 0)

                    indent = indentScope.Indent;

                if (!this.ReadAttributeAndTryEnterScope(indentScope.Indent, false, out consumedBytes))

                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(AMMMYR.YamlCouldNotDetermineElementScope));
            }

            if (this.ElementNode.IsEmptyElement)
            {
                this.TryExitYamlScope();

                this.EnterYamlScope(base.BufferReader.Offset, indent + consumedBytes, YamlScopeType.Indent);
            }
            else if (lines > 0)
            {
                if (this.addedBlockSequenceElement)

                    this.EnterYamlScope(base.BufferReader.Offset, indent, YamlScopeType.SequenceItem, indent + consumedBytes, YamlScopeType.Indent);

                else

                    this.EnterYamlScope(base.BufferReader.Offset, indent + consumedBytes, YamlScopeType.Indent);
            }
        }

        private bool IsUTF8_BOM(byte[] buffer, int offset)
        {
            return buffer[offset + 0] == EncodingChars.UTF8_BOM_1 && (buffer[offset + 1] == EncodingChars.UTF8_BOM_2 && buffer[offset + 2] == EncodingChars.UTF8_BOM_3);
        }

        private bool IsNextCharacterFFFFOrFFFE(byte[] buffer, int offset)
        {
            return (buffer[offset + 1] == EncodingChars.FF && (buffer[offset + 2] == EncodingChars.FF || buffer[offset + 2] == EncodingChars.FE));
        }

        private bool IsStartDocument(byte[] buffer, int offset)
        {
            return buffer[offset] == PrintableChars.HYPHEN && buffer[offset + 1] == PrintableChars.HYPHEN && buffer[offset + 2] == PrintableChars.HYPHEN;
        }

        private bool IsNextByteInBufferReader(byte @byte)
        {
            return (int)this.BufferReader.GetByte() == (int)@byte;
        }

        private bool HasByteInBufferReaderFollowedByWhitespace(byte @byte)
        {
            int offset, offsetMax;
            bool escaped;

            byte[] buffer;

            buffer = this.BufferReader.GetBuffer(out offset, out offsetMax);

            while (true)
            {
                int num = this.CountBytesBeforeByte(buffer, offset, offsetMax, @byte, out escaped);

                if (num < 0)

                    return false;

                byte nextByte = buffer[offset + num + 1];

                bool found = this.IsWhitespace(nextByte);

                if (found)

                    return true;

                offset += num + 1;
            }
        }

        private void BufferElement()
        {
            int offset1 = this.BufferReader.Offset;
            bool flag = false;
            byte num1 = 0;
            while (!flag)
            {
                int offset2;
                int offsetMax;
                byte[] buffer = this.BufferReader.GetBuffer(128, out offset2, out offsetMax);
                if (offset2 + 128 == offsetMax)
                {
                    for (int index = offset2; index < offsetMax && !flag; ++index)
                    {
                        byte num2 = buffer[index];
                        if (num2 == PrintableChars.BACKSLASH)
                        {
                            ++index;
                            if (index >= offsetMax)
                                break;
                        }
                        else if (num1 == ControlChars.NULL)
                        {
                            if (num2 == PrintableChars.SINGLE_QUOTE || num2 == PrintableChars.DOUBLE_QUOTE)
                                num1 = num2;
                            if (num2 == PrintableChars.COLON)
                                flag = true;
                        }
                        else if ((int)num2 == (int)num1)
                            num1 = ControlChars.NULL;
                    }
                    this.BufferReader.Advance(128);
                }
                else
                    break;
            }
            this.BufferReader.Offset = offset1;
        }

        private void ParseAndSetLocalName()
        {
            XmlBaseReader.XmlElementNode elementNode = this.EnterScope();

            byte @byte = base.BufferReader.GetByte();

            if (@byte == PrintableChars.HYPHEN)
            {
                base.BufferReader.SkipByte();

                @byte = base.BufferReader.GetByte();
            }

            if (IsInlineWhitespace(@byte))
            {
                this.SkipWhitespaceInBufferReader();

                @byte = base.BufferReader.GetByte();
            }

            Namespace ns = null;

            if (@byte == PrintableChars.EXCLAMATION_POINT)
            {
                ns = this.ReadTagHandle();

                int lines, indent;

                SkipWhitespaceInBufferReader(out lines, out indent);
            }

            elementNode.NameOffset = this.BufferReader.Offset;

            int whiteSpaceBeforeColon = 0;
            do
            {
                byte[] buffer = this.BufferReader.GetBuffer(2, out int os);
                if (buffer[os] == PrintableChars.BACKSLASH && buffer[os + 1] != PrintableChars.BACKSLASH)
                    this.ReadEscapedCharacter(false);
                else
                    this.ReadColonTerminatedText(false, out whiteSpaceBeforeColon);
            }
            while (this.textMode == YamlTextModes.FlowColonTerminated);

            int offset = this.BufferReader.Offset;

            this.SkipExpectedByteInBufferReader(PrintableChars.COLON);

            elementNode.LocalName.SetValue(elementNode.NameOffset, offset - whiteSpaceBeforeColon - elementNode.NameOffset);

            elementNode.NameLength = offset - whiteSpaceBeforeColon - elementNode.NameOffset;

            if (ns != null)
            {
                elementNode.Namespace = ns;
            }
            else
            {
                elementNode.Namespace.Uri.SetValue(elementNode.NameOffset, 0);
                elementNode.Prefix.SetValue(PrefixHandleType.Empty);
            }
           
            elementNode.IsEmptyElement = false;
            elementNode.ExitScope = false;
            elementNode.BufferOffset = offset;

            @byte = this.BufferReader.GetByte(elementNode.NameOffset);

            // if name is not name formatted, persist to Item attribute ...
            if (!@byte.IsAlphaChar())
            {
                this.SetItemNameWithMapping(elementNode);
            }
            else
            {
                int index = 0;
                int nameOffset = elementNode.NameOffset;
                while (index < elementNode.NameLength)
                {
                    @byte = this.BufferReader.GetByte(nameOffset);
                    if (!@byte.IsAlphaNumericChar() || ((int)@byte) >= 128)
                    {
                        this.SetItemNameWithMapping(elementNode);
                        break;
                    }
                    ++index;
                    ++nameOffset;
                }
            }
        }

        private bool TryRemoveScope(YamlScopeType scopeType, out YamlScope removedScope)
        {
            removedScope = CurrentYamlScope;

            if (removedScope.ScopeType == scopeType)
            {
                TryExitYamlScope();

                return true;
            }

            removedScope = null;

            return false;
        }

        private void TryTransitionToIndentScope()
        {
            this.SkipWhitespaceInBufferReader(out int lines, out int indent);

            if (lines > 0)

                this.TryTransitionToIndentScope(indent);
        }
        
        private void TryTransitionToIndentScope(int indent)
        {
            YamlScope scope = CurrentYamlScope;

            if (scope.ScopeType != YamlScopeType.SequenceItem)

                TryExitYamlScope();

            if (indent >= 0)

                this.EnterYamlScope(base.BufferReader.Offset, scope.Indent, scope.ScopeType, indent, YamlScopeType.Indent);
        }

        private bool AtYamlScopeIndented(out int? previousIndentOffset, out int? previousIndent, out YamlScopeType previousScopeType)
        {
            YamlScope scope = CurrentYamlScope;

            if (scope.ScopeType == YamlScopeType.Indent)
            {
                if (scope.PreviousScopeType == YamlScopeType.Indent)
                {
                    YamlScope previousScope = PreviousYamlScope;

                    previousIndentOffset = scope.Indent - previousScope.Indent;

                    previousIndent = previousScope.Indent;

                    previousScopeType = previousScope.ScopeType;
                }
                else
                {
                    previousIndentOffset = scope.Indent - scope.PreviousIndent;

                    previousIndent = scope.PreviousIndent;

                    previousScopeType = scope.PreviousScopeType;
                }

                return true;
            }

            previousIndentOffset = null;

            previousIndent = null;

            previousScopeType = YamlScopeType.None;

            return false;
        }

        private void EnterYamlScope(int offset, int indent, YamlScopeType scopeType)
        {
            if (this.scopes.Length == this.scopeDepth + 1)
            {
                YamlScope[] resizedScopes = new YamlScope[this.scopeDepth * 2];
                Array.Copy((Array)this.scopes, (Array)resizedScopes, this.scopeDepth + 1);
                this.scopes = resizedScopes;
            }

            this.scopeDepth++;

            int scopeIndex = this.scopeDepth - 1;

            YamlScope previousScope;

            if (scopeIndex > 0)
                previousScope = this.scopes[scopeIndex - 1];
            else
                previousScope = YamlScope.NONE;

            this.scopes[scopeIndex] = new YamlScope(offset, previousScope.Indent, previousScope.ScopeType, indent, scopeType);
        }

        private void EnterYamlScope(int offset, int previousIndent, YamlScopeType previousScopeType, int indent, YamlScopeType scopeType)
        {
            if (this.scopes.Length == this.scopeDepth + 1)
            {
                YamlScope[] resizedScopes = new YamlScope[this.scopeDepth * 2];
                Array.Copy((Array)this.scopes, (Array)resizedScopes, this.scopeDepth + 1);
                this.scopes = resizedScopes;
            }

            this.scopeDepth++;

            int scopeIndex = this.scopeDepth - 1;

            this.scopes[scopeIndex] = new YamlScope(offset, previousIndent, previousScopeType, indent, scopeType);
        }

        private YamlScope PreviousYamlScope
        {
            get => this.scopeDepth > 1 ? this.scopes[this.scopeDepth - 2] : YamlScope.NONE;
        }

        private YamlScope CurrentYamlScope
        {
            get => this.scopeDepth > 0 ? this.scopes[this.scopeDepth - 1] : YamlScope.NONE;
        }

        private YamlScopeType TryExitYamlScope()
        {
            int scopeIndex = this.scopeDepth - 1;

            if (scopeIndex < 0)

                return YamlScopeType.None;

            this.scopeDepth--;

            YamlScopeType scopeType = this.scopes[scopeIndex].ScopeType;

            this.scopes[scopeIndex] = null;

            return scopeType;
        }

        private void ReadAttribute(XmlDictionaryString attributeNameString)
        {
            this.prefix.SetValue(PrefixHandleType.Empty);
            this.localName.SetValue(attributeNameString.Key);
            int offset = base.BufferReader.Offset;
            bool flag = false;
            this.ReadAttributeText(ref flag);
            this.WriteAttributeNode(this.prefix, this.localName, ControlChars.NULL, flag, offset);
        }

        private void ReadNonExistentAttribute(XmlDictionaryString attributeNameString)
        {
            this.prefix.SetValue(PrefixHandleType.Empty);
            this.localName.SetValue(attributeNameString.Key);
            int offset = base.BufferReader.Offset;
            bool flag = false;
            this.WriteAttributeNode(this.prefix, this.localName, ControlChars.NULL, flag, offset);
        }

        private void ReadNonExistentTypeAttribute(ValueHandleConstStringType constStringType)
        {
            this.prefix.SetValue(PrefixHandleType.Empty);
            this.localName.SetConstantValue(StringHandleConstStringType.Type);
            this.WriteAttributeNode(this.prefix, this.localName, constStringType);
        }

        /// <summary>
        /// Reads the next attribute; returns whether a yaml scope (<see cref="YamlScopeType"/>) was entered as a result.
        /// </summary>
        /// <returns><c>true</c>, if a yaml scope (<see cref="YamlScopeType"/>) was entered, <c>false</c> otherwise.</returns>
        /// <param name="indent"></param>
        /// <param name="indentAdvancedBy"></param>
        private bool ReadAttributeAndTryEnterScope(int indent, bool possibleEmptyElement, out int indentAdvancedBy)
        {
            int offset, maxOffset;

            byte[] buffer = base.BufferReader.GetBuffer(2, out offset, out maxOffset);

            if (this.MoveToAttribute("type"))
            {
                this.MoveToElement();

                indentAdvancedBy = 0;

                this.EnterYamlScope(offset, indent, YamlScopeType.BlockMapping);
            }
            else
            {
                byte @byte = buffer[offset];

                switch (@byte)
                {
                    case PrintableChars.AMPERSAND:

                        //anchor

                        base.BufferReader.SkipByte();

                        indentAdvancedBy = 0;

                        this.ReadAttribute(this.anchorString);

                        return false;

                    case PrintableChars.ASTERISK:

                        //alias

                        base.BufferReader.SkipByte();

                        indentAdvancedBy = 0;

                        this.ReadAttribute(this.aliasString);

                        this.ElementNode.IsEmptyElement = true;

                        this.ElementNode.ExitScope = true;

                        return false;

                    case PrintableChars.VERTICAL_BAR:

                        //literal content

                        base.BufferReader.SkipByte();

                        indentAdvancedBy = 0;

                        this.ReadAttribute(this.literalString);

                        this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.String);

                        return false;

                    case PrintableChars.GREATER_THAN:

                        //folded content

                        base.BufferReader.SkipByte();

                        indentAdvancedBy = 0;

                        this.ReadAttribute(this.foldedString);

                        this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.String);

                        return false;

                    case PrintableChars.OPENING_BRACKET:

                        //flow sequence

                        base.BufferReader.SkipByte();

                        indentAdvancedBy = 0;

                        this.ReadNonExistentAttribute(this.flowSequenceString);

                        this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Array);

                        this.EnterYamlScope(offset, indent, YamlScopeType.FlowSequence);

                        break;

                    case PrintableChars.OPENING_BRACE:

                        //flow mapping

                        base.BufferReader.SkipByte();

                        indentAdvancedBy = 0;

                        this.ReadNonExistentAttribute(this.flowMappingString);

                        this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Object);

                        this.EnterYamlScope(offset, indent, YamlScopeType.FlowMapping);

                        break;

                    case PrintableChars.HYPHEN:

                        //block sequence?

                        if (IsWhitespace(buffer[offset + 1]))
                        {
                            base.BufferReader.Advance(2);

                            indentAdvancedBy = 2;

                            this.ReadNonExistentAttribute(this.blockSequenceString);

                            this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Array);

                            this.EnterYamlScope(offset, indent, YamlScopeType.BlockSequence);

                            this.addedBlockSequenceElement = true;
                        }
                        else
                        {
                            //block mapping

                            this.ReadNonExistentAttribute(this.blockMappingString);

                            this.ReadNonExistentTypeAttribute(out indentAdvancedBy);

                            this.EnterYamlScope(offset, indent, YamlScopeType.BlockMapping);
                        }

                        break;

                    default:

                        if (possibleEmptyElement)
                        {
                            indentAdvancedBy = 0;

                            this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Null);

                            this.ElementNode.IsEmptyElement = true;

                            this.ElementNode.ExitScope = true;
                        }
                        else
                        {
                            //block mapping

                            this.ReadNonExistentAttribute(this.blockMappingString);

                            this.ReadNonExistentTypeAttribute(out indentAdvancedBy);
                        }

                        this.EnterYamlScope(offset, indent, YamlScopeType.BlockMapping);

                        break;
                }
            }

            return true;
        }

        private void ReadNonExistentTypeAttribute(out int indentAdvancedBy)
        {
            byte[] buffer = this.BufferReader.GetBuffer(out int offset, out int offsetMax);

            if (0 <= this.CountBytesBeforeByte(buffer, offset, offsetMax, PrintableChars.COLON, out int whiteSpaceBeforeColon, out bool escaped))
            {
                this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Object);

                indentAdvancedBy = 0;
            }
            else if (buffer[offset] == PrintableChars.EXCLAMATION_POINT && buffer[offset + 1] == PrintableChars.EXCLAMATION_POINT)
            {
                if (buffer[offset + 2] == PrintableChars.n && buffer[offset + 3] == PrintableChars.u && buffer[offset + 4] == PrintableChars.l && buffer[offset + 5] == PrintableChars.l && buffer[offset + 6] == PrintableChars.SPACE)
                {//null
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Null);

                    base.BufferReader.Advance(7);

                    indentAdvancedBy = 0;
                }
                else if (buffer[offset + 2] == PrintableChars.b && buffer[offset + 3] == PrintableChars.o && buffer[offset + 4] == PrintableChars.o && buffer[offset + 5] == PrintableChars.l && buffer[offset + 6] == PrintableChars.SPACE)
                {//bool
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Boolean);

                    base.BufferReader.Advance(7);

                    indentAdvancedBy = 0;
                }
                else if (buffer[offset + 2] == PrintableChars.s && buffer[offset + 3] == PrintableChars.t && buffer[offset + 4] == PrintableChars.r && buffer[offset + 5] == PrintableChars.SPACE)
                {//str
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.String);

                    base.BufferReader.Advance(6);

                    indentAdvancedBy = 0;
                }
                else if (buffer[offset + 2] == PrintableChars.i && buffer[offset + 3] == PrintableChars.n && buffer[offset + 4] == PrintableChars.t && buffer[offset + 5] == PrintableChars.SPACE)
                {//integer
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Number);

                    base.BufferReader.Advance(6);

                    indentAdvancedBy = 0;
                }
                else if (buffer[offset + 2] == PrintableChars.f && buffer[offset + 3] == PrintableChars.l && buffer[offset + 4] == PrintableChars.o && buffer[offset + 5] == PrintableChars.a && buffer[offset + 6] == PrintableChars.t && buffer[offset + 7] == PrintableChars.SPACE)
                {//float
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Number);

                    base.BufferReader.Advance(8);

                    indentAdvancedBy = 0;
                }
                else
                {
                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.JsonEncounteredUnexpectedCharacter));

                    indentAdvancedBy = 0;
                }
            }
            else
            {
                if (buffer[offset] == PrintableChars.n && buffer[offset + 1] == PrintableChars.u && buffer[offset + 2] == PrintableChars.l && buffer[offset + 3] == PrintableChars.l)
                {//null
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Null);

                    indentAdvancedBy = 0;
                }
                else if (buffer[offset] == PrintableChars.t && buffer[offset + 1] == PrintableChars.r && buffer[offset + 2] == PrintableChars.u && buffer[offset + 3] == PrintableChars.e)
                {//bool
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Boolean);

                    indentAdvancedBy = 0;
                }
                else if (buffer[offset] == PrintableChars.f && buffer[offset + 1] == PrintableChars.a && buffer[offset + 2] == PrintableChars.l && buffer[offset + 3] == PrintableChars.s && buffer[offset + 4] == PrintableChars.e)
                {//bool
                    this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Boolean);

                    indentAdvancedBy = 0;
                }
                else
                {
                    switch (buffer[offset])
                    {
                        case 48:
                        case 49:
                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                        case 56:
                        case 57:

                            this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.Number);

                            indentAdvancedBy = 0;

                            break;

                        default:

                            this.ReadNonExistentTypeAttribute(ValueHandleConstStringType.String);

                            indentAdvancedBy = 0;

                            break;
                    }
                }
            }
        }

        private void ReadAttributeText(ref bool escapedFlag)
        {
            byte @byte;
            while (true)
            {
                int offset;
                int offsetMax;
                byte[] buffer = base.BufferReader.GetBuffer(out offset, out offsetMax);
                int count = this.ReadAttributeText(buffer, offset, offsetMax);
                base.BufferReader.Advance(count);
                @byte = base.BufferReader.GetByte();
                if (IsWhitespace(@byte))
                {
                    break;
                }
                else
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "Whitespace character", (char)@byte);
                }
            }
        }

        private void WriteAttributeNode(PrefixHandle _prefix, StringHandle _localName, byte quoteByte, bool flag, int offset, int whiteSpaceAtEnd = 0)
        {
            int length = base.BufferReader.Offset - whiteSpaceAtEnd - offset;
            XmlBaseReader.XmlAttributeNode xmlAttributeNode;
            if (_prefix.IsXmlns)
            {
                XmlBaseReader.Namespace @namespace = base.AddNamespace();
                _localName.ToPrefixHandle(@namespace.Prefix);
                @namespace.Uri.SetValue(offset, length, flag);
                xmlAttributeNode = base.AddXmlnsAttribute(@namespace);
            }
            else if (_prefix.IsEmpty && _localName.IsXmlns)
            {
                XmlBaseReader.Namespace namespace2 = base.AddNamespace();
                namespace2.Prefix.SetValue(PrefixHandleType.Empty);
                namespace2.Uri.SetValue(offset, length, flag);
                xmlAttributeNode = base.AddXmlnsAttribute(namespace2);
            }
            else if (_prefix.IsXml)
            {
                xmlAttributeNode = base.AddXmlAttribute();
                xmlAttributeNode.Prefix.SetValue(_prefix);
                xmlAttributeNode.LocalName.SetValue(_localName);
                xmlAttributeNode.Value.SetValue(flag ? ValueHandleType.EscapedUTF8 : ValueHandleType.UTF8, offset, length);
                base.FixXmlAttribute(xmlAttributeNode);
            }
            else
            {
                xmlAttributeNode = base.AddAttribute();
                xmlAttributeNode.Prefix.SetValue(_prefix);
                xmlAttributeNode.LocalName.SetValue(_localName);
                xmlAttributeNode.Value.SetValue(flag ? ValueHandleType.EscapedUTF8 : ValueHandleType.UTF8, offset, length);
            }
            if (quoteByte != ControlChars.NULL)
            
                xmlAttributeNode.QuoteChar = (char)quoteByte;
        }

        private void WriteAttributeNode(PrefixHandle _prefix, StringHandle _localName, ValueHandleConstStringType constStringType)
        {
            if (_prefix.IsXmlns || _prefix.IsXml || _prefix.IsEmpty && _localName.IsXmlns)
            {
                throw new NotSupportedException($"{nameof(_prefix)}:{_prefix} and/or {nameof(_localName)}:{_localName}");
            }
            else
            {
                XmlBaseReader.XmlAttributeNode xmlAttributeNode = base.AddAttribute();
                xmlAttributeNode.Prefix.SetValue(_prefix);
                xmlAttributeNode.LocalName.SetValue(_localName);
                xmlAttributeNode.Value.SetConstantValue(constStringType);
            }
        }

        private void ReadEscapedCharacter(bool moveToText)
        {
            this.BufferReader.SkipByte();
            char ch1 = (char)this.BufferReader.GetByte();
            switch (ch1)
            {
                case '"':
                case '/':
                case '\\':
                    this.BufferReader.SkipByte();
                    if (this.BufferReader.GetByte() == PrintableChars.DOUBLE_QUOTE)
                    {
                        this.BufferReader.SkipByte();
                        if (moveToText)
                            this.MoveToAtomicText().Value.SetCharValue((int)ch1);
                        this.textMode = YamlTextModes.None;
                        break;
                    }
                    if (moveToText)
                        this.MoveToComplexText().Value.SetCharValue((int)ch1);
                    this.textMode = YamlTextModes.FlowDoubleQuoted;
                    break;
                case 'b':
                    ch1 = '\b';
                    goto case '"';
                case 'f':
                    ch1 = '\f';
                    goto case '"';
                case 'n':
                    ch1 = '\n';
                    goto case '"';
                case 'r':
                    ch1 = '\r';
                    goto case '"';
                case 't':
                    ch1 = '\t';
                    goto case '"';
                case 'u':
                    this.BufferReader.SkipByte();
                    int offset;
                    byte[] buffer = this.BufferReader.GetBuffer(5, out offset);
                    string str1 = Encoding.UTF8.GetString(buffer, offset, 4);
                    this.BufferReader.Advance(4);
                    int ch2 = (int)ParseChar(str1, NumberStyles.HexNumber);
                    if (char.IsHighSurrogate((char)ch2) && this.BufferReader.GetByte() == PrintableChars.BACKSLASH)
                    {
                        this.BufferReader.SkipByte();
                        this.SkipExpectedByteInBufferReader((byte)117);
                        buffer = this.BufferReader.GetBuffer(5, out offset);
                        string str2 = Encoding.UTF8.GetString(buffer, offset, 4);
                        this.BufferReader.Advance(4);
                        char ch3 = ParseChar(str2, NumberStyles.HexNumber);
                        if (!char.IsLowSurrogate(ch3))
                            XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.Format(PublicSR.XmlInvalidLowSurrogate, new object[1]
                            {
                                (object) str2
                            })));
                        ch2 = new SurrogateChar(ch3, (char)ch2).Char;
                    }
                    if (buffer[offset + 4] == PrintableChars.DOUBLE_QUOTE)
                    {
                        this.BufferReader.SkipByte();
                        if (moveToText)
                            this.MoveToAtomicText().Value.SetCharValue(ch2);
                        this.textMode = YamlTextModes.None;
                        break;
                    }
                    if (moveToText)
                        this.MoveToComplexText().Value.SetCharValue(ch2);
                    this.textMode = YamlTextModes.FlowDoubleQuoted;
                    break;
                default:
                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.Format(PublicSR.JsonEncounteredUnexpectedCharacter, new object[1]
                    {
                        (object) ch1
                    })));
                    goto case '"';
            }
        }

        private int ReadNon_FFFFOrFFFE()
        {
            int offset;
            byte[] buffer = this.BufferReader.GetBuffer(3, out offset);
            if (IsNextCharacterFFFFOrFFFE(buffer, offset))
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(PublicSR.XmlInvalidFFFE));
            return 3;
        }

        private void ReadColonTerminatedText(bool moveToText, out int whiteSpaceBeforeColon)
        {
            ReadTerminatedText(PrintableChars.COLON, moveToText, out whiteSpaceBeforeColon);
        }

        private void ReadTerminatedText(byte terminalByte, bool moveToText, out int whiteSpaceBeforeTerminalByte)
        {
            switch (terminalByte)
            {
                case PrintableChars.COLON:
                    break;
                default:
                    throw new ArgumentException($"{terminalByte} not supported.", nameof(terminalByte));
            }
            int offset;
            bool escaped;
            int num;
            bool flag;
            if (this.buffered)
            {
                int offsetMax;
                num = this.CountBytesBeforeByte(this.BufferReader.GetBuffer(out offset, out offsetMax), offset, offsetMax, terminalByte, out whiteSpaceBeforeTerminalByte, out escaped);
                if (num < 0)
                    XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)terminalByte).ToString(), "End of line");
                flag = offset < offsetMax - num;
            }
            else
            {
                int offsetMax;
                byte[] buffer = this.BufferReader.GetBuffer(2048, out offset, out offsetMax);
                int lengthUntilTerminalByte = this.CountBytesBeforeByte(buffer, offset, offsetMax, terminalByte, out whiteSpaceBeforeTerminalByte, out escaped);
                if (lengthUntilTerminalByte < 0)
                    XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)terminalByte).ToString(), "End of line");
                flag = offset < offsetMax - lengthUntilTerminalByte;
                num = BreakText(buffer, offset, lengthUntilTerminalByte);
            }
            if (escaped && this.BufferReader.GetByte() == EncodingChars.UTF8_BOM_1)
            {
                offset = this.BufferReader.Offset;
                num = this.ReadNon_FFFFOrFFFE();
                whiteSpaceBeforeTerminalByte = 0;
            }
            this.BufferReader.Advance(num);
            if (!escaped & flag)
            {
                if (moveToText)
                    this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, num - whiteSpaceBeforeTerminalByte - 1);
                this.textMode = YamlTextModes.None;
            }
            else if (num == 0 & escaped)
            {
                this.ReadEscapedCharacter(moveToText);
            }
            else
            {
                if (moveToText)
                    this.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                switch (terminalByte)
                {
                    case PrintableChars.COLON:
                        this.textMode = YamlTextModes.FlowColonTerminated;
                        break;
                }
            }
        }

        private void ReadMultiLineText(string textStyle, out int lines, out int indent)
        {
            //int lines, indent;

            StringBuilder sb = new StringBuilder();

            do
            {
                this.ReadText(false);

                if (this.HasValue)
                {
                    if (sb.Length > 0)
                    {
                        switch (textStyle)
                        {
                            case YamlTokens.LITERAL:

                                sb.Append((char)ControlChars.LINE_FEED);

                                break;

                            case YamlTokens.FOLDED:
                            case YamlTokens.PLAIN:

                                sb.Append((char)PrintableChars.SPACE);

                                break;
                        }
                    }
                    sb.Append(this.Value);
                }

                this.SkipWhitespaceInBufferReader(out lines, out indent);
            }
            while (lines > 0 && indent >= this.CurrentYamlScope.Indent);

            base.MoveToComplexText().Value.SetDictionaryValue(this.AddToDictionary(sb.ToString()).Key);
        }

        private void ReadPlainText(bool moveToText)
        {
            int num;
            int num2;
            byte[] buffer;
            int num3;
            int num4;
            int num5;
            if (this.buffered)
            {
                buffer = base.BufferReader.GetBuffer(out num, out num2);
                num3 = this.ReadPlainText(buffer, num, num2, out num4, out num5);
            }
            else
            {
                buffer = base.BufferReader.GetBuffer(2048, out num, out num2);
                num3 = this.ReadPlainText(buffer, num, num2, out num4, out num5);
                num3 = this.BreakText(buffer, num, num3);
            }
            base.BufferReader.Advance(num3 + num4);
            if (num < num2 - 1 - num3 && buffer[num + num3] == PrintableChars.LESS_THAN && buffer[num + num3 + 1] != PrintableChars.EXCLAMATION_POINT)
            {
                if (moveToText)
                    base.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, num + num4, num3 - num5);
                return;
            }
            if (moveToText)
                base.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, num + num4, num3 - num5);
            this.textMode = YamlTextModes.FlowPlain;
        }

        private void ReadSingleQuotedText(bool moveToText)
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.SINGLE_QUOTE);
            int offset;
            bool escaped;
            int num;
            bool flag;
            if (this.buffered)
            {
                int offsetMax;
                num = this.CountBytesBeforeByte(this.BufferReader.GetBuffer(out offset, out offsetMax), offset, offsetMax, PrintableChars.SINGLE_QUOTE, out escaped);
                if (num < 0)
                    XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)PrintableChars.SINGLE_QUOTE).ToString(), "End of line");
                flag = offset < offsetMax - num;
            }
            else
            {
                int offsetMax;
                byte[] buffer = this.BufferReader.GetBuffer(2048, out offset, out offsetMax);
                int lengthUntilEndQuote = this.CountBytesBeforeByte(buffer, offset, offsetMax, PrintableChars.SINGLE_QUOTE, out escaped);
                if (lengthUntilEndQuote < 0)
                    XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)PrintableChars.SINGLE_QUOTE).ToString(), "End of line");
                flag = offset < offsetMax - lengthUntilEndQuote;
                num = BreakText(buffer, offset, lengthUntilEndQuote);
            }
            if (escaped && this.BufferReader.GetByte() == EncodingChars.UTF8_BOM_1)
            {
                offset = this.BufferReader.Offset;
                num = this.ReadNon_FFFFOrFFFE();
            }
            this.BufferReader.Advance(num);
            if (!escaped & flag)
            {
                if (moveToText)
                    this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                this.SkipExpectedByteInBufferReader(PrintableChars.SINGLE_QUOTE);
                this.textMode = YamlTextModes.None;
            }
            else if (num == 0 & escaped)
            {
                this.ReadEscapedCharacter(moveToText);
            }
            else
            {
                if (moveToText)
                    this.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                this.textMode = YamlTextModes.FlowSingleQuoted;
            }
        }

        private void ReadDoubleQuotedText(bool moveToText)
        {
            this.SkipExpectedByteInBufferReader(PrintableChars.DOUBLE_QUOTE);
            int offset;
            bool escaped;
            int num;
            bool flag;
            if (this.buffered)
            {
                int offsetMax;
                num = this.CountBytesBeforeByte(this.BufferReader.GetBuffer(out offset, out offsetMax), offset, offsetMax, PrintableChars.DOUBLE_QUOTE, out escaped);
                if (num < 0)
                    XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)PrintableChars.DOUBLE_QUOTE).ToString(), "End of line");
                flag = offset < offsetMax - num;
            }
            else
            {
                int offsetMax;
                byte[] buffer = this.BufferReader.GetBuffer(2048, out offset, out offsetMax);
                int lengthUntilEndQuote = this.CountBytesBeforeByte(buffer, offset, offsetMax, PrintableChars.DOUBLE_QUOTE, out escaped);
                if (lengthUntilEndQuote < 0)
                    XmlExceptionHelper.ThrowTokenExpected((XmlDictionaryReader)this, ((char)PrintableChars.DOUBLE_QUOTE).ToString(), "End of line");
                flag = offset < offsetMax - lengthUntilEndQuote;
                num = BreakText(buffer, offset, lengthUntilEndQuote);
            }
            if (escaped && this.BufferReader.GetByte() == EncodingChars.UTF8_BOM_1)
            {
                offset = this.BufferReader.Offset;
                num = this.ReadNon_FFFFOrFFFE();
            }
            this.BufferReader.Advance(num);
            if (!escaped & flag)
            {
                if (moveToText)
                    this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                this.SkipExpectedByteInBufferReader(PrintableChars.DOUBLE_QUOTE);
                this.textMode = YamlTextModes.None;
            }
            else if (num == 0 & escaped)
            {
                this.ReadEscapedCharacter(moveToText);
            }
            else
            {
                if (moveToText)
                    this.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                this.textMode = YamlTextModes.FlowDoubleQuoted;
            }
        }

        private int CountBytesUntilWhiteSpaceByte(byte[] buffer, int offset, int offsetMax, out bool escaped)
        {
            int initialOffset = offset;
            escaped = false;
            bool skipNext = false;
            for (; offset < offsetMax; ++offset)
            {
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }
                byte @byte = buffer[offset];
                if (@byte < PrintableChars.SPACE)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)new FormatException(PublicSR.Format(PublicSR.InvalidCharacterEncountered, new object[1] { (object)(char)@byte })));
                if (this.IsWhitespace(@byte))
                    goto label_exit;
                switch (@byte)
                {
                    case PrintableChars.BACKSLASH:
                        if (buffer[offset + 1] == PrintableChars.BACKSLASH)
                            skipNext = true;
                        else
                        {
                            escaped = true;
                            goto label_exit;
                        }
                        continue;
                    case EncodingChars.UTF8_BOM_1:
                        escaped = true;
                        goto label_exit;
                    default:
                        continue;
                }
            }
        label_exit:
            return offset - initialOffset;
        }

        /// <summary>
        /// Counts the bytes until either a control byte (such as a line break), or the <paramref name="byte"/> is encoutered.
        /// </summary>
        /// <returns>The bytes until end byte, or -1, if a control byteis encountered first.</returns>
        /// <param name="buffer">Buffer.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="offsetMax">Offset max.</param>
        /// <param name="byte">End byte.</param>
        /// <param name="escaped">If set to <c>true</c> escaped.</param>
        private int CountBytesBeforeByte(byte[] buffer, int offset, int offsetMax, byte @byte, out bool escaped)
        {
            int whiteSpaceBeforeEndByte;

            return CountBytesBeforeByte(buffer, offset, offsetMax, @byte, out whiteSpaceBeforeEndByte, out escaped);
        }

        /// <summary>
        /// Counts the bytes until either a control byte (such as a line break), or the <p <paramref name="byte"/> is encoutered.
        /// </summary>
        /// <returns>The bytes until end byte, or -1, if a control byteis encountered first.</returns>
        /// <param name="buffer">Buffer.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="offsetMax">Offset max.</param>
        /// <param name="byte">End byte.</param>
        /// <param name="whiteSpaceBeforeEndByte">White space before end byte.</param>
        /// <param name="escaped">If set to <c>true</c> escaped.</param>
        private int CountBytesBeforeByte(byte[] buffer, int offset, int offsetMax, byte @byte, out int whiteSpaceBeforeEndByte, out bool escaped)
        {
            whiteSpaceBeforeEndByte = 0;
            int initialOffset = offset;
            escaped = false;
            bool skipNext = false;
            for (; offset < offsetMax; ++offset)
            {
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }
                byte offsetByte = buffer[offset];
                if (offsetByte < PrintableChars.SPACE)
                    return -1;
                if (offsetByte == @byte)
                    goto label_exit;
                if (IsWhitespace(offsetByte))
                    whiteSpaceBeforeEndByte++;
                else
                    whiteSpaceBeforeEndByte = 0;
                switch (offsetByte)
                {
                    case PrintableChars.BACKSLASH:
                        if (buffer[offset + 1] == PrintableChars.BACKSLASH)
                            skipNext = true;
                        else
                        {
                            escaped = true;
                            goto label_exit;
                        }
                        continue;
                    case EncodingChars.UTF8_BOM_1:
                        escaped = true;
                        goto label_exit;
                    default:
                        continue;
                }
            }
        label_exit:
            return offset - initialOffset;
        }

        private static char ParseChar(string value, NumberStyles style)
        {
            int num = ParseInt(value, style);
            try
            {
                return Convert.ToChar(num);
            }
            catch (OverflowException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)XmlExceptionHelper.CreateConversionException(value, "char", (Exception)ex));
            }
        }

        private static int ParseInt(string value, NumberStyles style)
        {
            try
            {
                return int.Parse(value, style, (IFormatProvider)NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)XmlExceptionHelper.CreateConversionException(value, "Int32", (Exception)ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)XmlExceptionHelper.CreateConversionException(value, "Int32", (Exception)ex));
            }
            catch (OverflowException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)XmlExceptionHelper.CreateConversionException(value, "Int32", (Exception)ex));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('(');

            sb.Append(this.NodeType);

            sb.Append(',');

            sb.Append(this.Name);

            sb.Append(')');

#if DEBUG
            sb.AppendLine(" --> ");

            foreach (YamlScope scope in this.scopes.Where(s => s != null).Reverse())
            {
                scope.WriteScope(sb);
            }
#else
            sb.AppendLine();
#endif
            return sb.ToString();
        }
    }
}