﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class DTypeVisitor : TexlFunctionalVisitor<DType, INameResolver>
    {
        private DTypeVisitor()
        {
        }

        public static DType Run(TexlNode node, INameResolver context)
        {
            return node.Accept(new DTypeVisitor(), context);
        }

        public override DType Visit(ErrorNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(BlankNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(BoolLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(StrLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(NumLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(DecLitNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(FirstNameNode node, INameResolver context)
        {
            var name = node.Ident.Name.Value;
            if (context.LookupType(new DName(name), out NameLookupInfo nameInfo))
            {
                return nameInfo.Type;
            }

            var typeFromString = FormulaType.GetFromStringOrNull(name);
            return typeFromString == null ? DType.Invalid : typeFromString._type;
        }

        public override DType Visit(ParentNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(SelfNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(StrInterpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(DottedNameNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(UnaryOpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(BinaryOpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(VariadicOpNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(CallNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(ListNode node, INameResolver context)
        {
            return DType.Invalid;
        }

        public override DType Visit(RecordNode node, INameResolver context)
        {
            var list = new List<TypedName>();
            foreach (var (cNode, ident) in node.ChildNodes.Zip(node.Ids, (a, b) => (a, b)))
            {
                var ty = cNode.Accept(this, context);
                if (ty == DType.Invalid)
                {
                    return DType.Invalid;
                }

                list.Add(new TypedName(ty, new DName(ident.Name.Value)));
            }

            return DType.CreateRecord(list);
        }

        public override DType Visit(TableNode node, INameResolver context)
        {
            var childNode = node.ChildNodes.First();
            var ty = childNode.Accept(this, context);
            if (ty == DType.Invalid)
            {
                return DType.Invalid;
            }

            return ty.ToTable();
        }

        public override DType Visit(AsNode node, INameResolver context)
        {
            return DType.Invalid;
        }
    }
}
