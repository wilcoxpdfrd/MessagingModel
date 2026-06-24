using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization.Yaml
{
    public enum YamlTextModes
    {
        None,
        FlowColonTerminated,
        FlowDoubleQuoted,
        FlowSingleQuoted,
        FlowPlain,
        BlockIndented,
        BlockChomped,
        BlockLiteral,
        BlockFolded
    }
}
