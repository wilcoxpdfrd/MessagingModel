using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization.Yaml
{
    public struct YamlTokens
    {
        public const string VERSION = "version";
        public const string BLOCK_SEQUENCE = "blockSequence";
        public const string FLOW_SEQUENCE = "flowSequence";
        public const string BLOCK_MAPPING = "blockMapping";
        public const string FLOW_MAPPING = "flowMapping";
        public const string ANCHOR = "anchor";
        public const string ALIAS = "alias";
        public const string LITERAL = "literal";
        public const string FOLDED = "folded";
        public const string PLAIN = "plain";
        public const string ARRAY_TYPE = "array";
        public const string OBJECT_TYPE = "object";
        public const string STRING_TYPE = "string";
        public const string INTEGER_TYPE = "integer";
        public const string FLOAT_TYPE = "float";
        public const string BOOLEAN_TYPE = "boolean";
        public const string NULL_TYPE = "null";
    }
}
