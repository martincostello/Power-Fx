﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(source:*, projection:n)
    // Max(source:*, projection:n)
    // Corresponding DAX functions: Min, MinA, MinX, Max, MaxA, MaxX
    internal sealed class MinMaxTableFunction : StatisticalTableFunction
    {
        private readonly DelegationCapability _delegationCapability;

        public override DelegationCapability FunctionDelegationCapability => _delegationCapability;

        public MinMaxTableFunction(bool isMin)
            : base(isMin ? "Min" : "Max", isMin ? TexlStrings.AboutMinT : TexlStrings.AboutMaxT, FunctionCategories.Table)
        {
            _delegationCapability = isMin ? DelegationCapability.Min : DelegationCapability.Max;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            // The return type will be the result type of the expression in the 2nd argument
            returnType = argTypes[1];
            nodeToCoercedTypeMap = null;

            // Coerce everything except date/times to numeric.
            if (argTypes[1] != DType.Date && argTypes[1] != DType.DateTime && argTypes[1] != DType.Time)
            {
                returnType = DetermineNumericFunctionReturnType(true, context.NumberIsFloat, argTypes[1]);
                if (!CheckType(context, args[1], argTypes[1], returnType, errors, ref nodeToCoercedTypeMap))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrNumberExpected);
                    nodeToCoercedTypeMap = null;
                    return false;
                }    
            }

            return true;
        }
    }
}
