﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    // To use with clients that may need Type definition tree for analysis
    internal class UserDefinedType
    {
        public DName Name { get; }

        public TexlNode TypeDefinition { get; }

        public FormulaType Type { get; }

        public UserDefinedType(DName typeName, FormulaType type, TexlNode typeNode)
        {
            this.Name = typeName;
            this.Type = type;
            this.TypeDefinition = typeNode;
        }
    }
}
