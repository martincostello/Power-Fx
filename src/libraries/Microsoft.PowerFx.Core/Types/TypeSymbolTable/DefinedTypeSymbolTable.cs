﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    [NotThreadSafe]
    internal class DefinedTypeSymbolTable : TypeSymbolTable, IGlobalSymbolNameResolver
    {
        private readonly BidirectionalDictionary<string, FormulaType> _definedTypes = new ();

        IEnumerable<KeyValuePair<string, FormulaType>> INameResolver.DefinedTypes => _definedTypes.AsEnumerable();

        IEnumerable<KeyValuePair<string, NameLookupInfo>> IGlobalSymbolNameResolver.GlobalSymbols => _definedTypes.ToDictionary(kvp => kvp.Key, kvp => ToLookupInfo(kvp.Value));

        internal void RegisterType(string typeName, FormulaType type)
        {
            // todo: include gaurd
            Inc();            

            _definedTypes.Add(typeName, type);
        }

        internal void RemoveType(string typeName)
        {
            Inc();
            _definedTypes.TryRemoveFromFirst(typeName);
        }

        protected void ValidateName(string name)
        {
            if (!DName.IsValidDName(name))
            {
                throw new ArgumentException("Invalid name: ${name}");
            }
        }

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (!_definedTypes.TryGetFromFirst(name.Value, out var type))
            {
                nameInfo = default;
                return false;
            }

            nameInfo = ToLookupInfo(type);
            return true;
        }

        internal override bool TryGetTypeName(FormulaType type, out string typeName)
        {
            return _definedTypes.TryGetFromSecond(type, out typeName);
        }

        internal void AddTypes(IEnumerable<KeyValuePair<string, FormulaType>> types)
        {
            foreach (var type in types) 
            {
                RegisterType(type.Key, type.Value);
            }
        }

        internal void RemoveTypes(IEnumerable<KeyValuePair<string, FormulaType>> types)
        {
            foreach (var type in types)
            {
                RemoveType(type.Key);
            }
        }

        bool INameResolver.LookupType(DName name, out NameLookupInfo nameInfo)
        {
            return TryLookup(name, out nameInfo);
        }
    }
}
