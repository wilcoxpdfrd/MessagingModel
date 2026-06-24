using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization.Yaml
{
    internal enum YamlScopeType
    {
        None,
        Directive,
        Comment,
        Indent,
        ContentNode,
        BlockSequence,
        BlockMapping,
        FlowSequence,
        FlowMapping,
        SequenceItem,
    }
}
