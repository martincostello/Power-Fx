﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal class InternalTableCapabilities : IExternalTabularDataSource
    {
        public const string IsChoiceValue = "Value";

        public InternalTableCapabilities(DName name, ServiceCapabilities2 serviceCapabilities2, bool isReadOnly, DType type, string datasetName)
        {
            string GetDisplayName(string fieldName)
            {
                DisplayNameProvider dnp = type.DisplayNameProvider;
                return dnp == null || !dnp.TryGetDisplayName(new DName(fieldName), out DName displayName) ? fieldName : displayName.Value;
            }

            DType GetFieldType(string fieldName) => type.TryGetType(new DName(fieldName), out var dType) ? dType : DType.ObjNull /* Blank */;

            DataFormat? ToDataFormat(DType dType)
            {
                return dType.Kind switch
                {
                    DKind.Record or DKind.Table or DKind.OptionSetValue => DataFormat.Lookup,
                    DKind.String or DKind.Decimal or DKind.Number or DKind.Currency => DataFormat.AllowedValues,
                    _ => null
                };
            }

            EntityName = name;
            IsWritable = !isReadOnly;
            _serviceCapabilities2 = serviceCapabilities2;
            _type = type;

            IEnumerable<string> fieldNames = type.GetRootFieldNames().Select(name => name.Value);

            _displayNameMapping = new BidirectionalDictionary<string, string>(fieldNames.Select(f => new KeyValuePair<string, string>(f, GetDisplayName(f))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            List<ColumnMetadata> columns = fieldNames.Select(f => new ColumnMetadata(f, GetFieldType(f), ToDataFormat(GetFieldType(f)), GetDisplayName(f), false /* is read-only */, false /* primary key */, false /* isRequired */, ColumnCreationKind.UserProvided, ColumnVisibility.Default, f, f, f, null, null)).ToList();

            _externalTableMetadata = new InternalTableMetadata(Name, Name, isReadOnly, columns);
            _externalDataEntityMetadataProvider = new InternalDataEntityMetadataProvider();
            _externalDataEntityMetadataProvider.AddSource(Name, new InternalDataEntityMetadata(name, datasetName, _displayNameMapping));
            _delegationMetadata = new DelegationMetadataBase(type, new CompositeCapabilityMetadata(type, GetCapabilityMetadata(type, serviceCapabilities2)));
            _tabularDataQueryOptions = new TabularDataQueryOptions(this);
            _previousDisplayNameMapping = null;
        }

        private readonly ServiceCapabilities2 _serviceCapabilities2;

        private readonly DType _type;

        private readonly InternalDataEntityMetadataProvider _externalDataEntityMetadataProvider;

        private readonly InternalTableMetadata _externalTableMetadata;

        private readonly DelegationMetadataBase _delegationMetadata;

        private readonly TabularDataQueryOptions _tabularDataQueryOptions;

        private readonly BidirectionalDictionary<string, string> _displayNameMapping;

        private readonly BidirectionalDictionary<string, string> _previousDisplayNameMapping;

        TabularDataQueryOptions IExternalTabularDataSource.QueryOptions => _tabularDataQueryOptions;

        public string Name => EntityName.Value;

        public DName EntityName { get; }

        public bool IsSelectable => _serviceCapabilities2.SelectionRestriction == null ? false : _serviceCapabilities2.SelectionRestriction.IsSelectable;

        public bool IsDelegatable => (_serviceCapabilities2.SortRestriction != null) ||
                                     (_serviceCapabilities2.FilterRestriction != null) ||
                                     (_serviceCapabilities2.FilterFunctions != null);

        public bool IsRefreshable => true;

        public bool RequiresAsync => true;

        public bool IsWritable { get; }

        public bool IsClearable => throw new System.NotImplementedException();

        IExternalDataEntityMetadataProvider IExternalDataSource.DataEntityMetadataProvider => _externalDataEntityMetadataProvider;

        DataSourceKind IExternalDataSource.Kind => DataSourceKind.Connected;

        IExternalTableMetadata IExternalDataSource.TableMetadata => _externalTableMetadata;

        IDelegationMetadata IExternalDataSource.DelegationMetadata => _delegationMetadata;

        DType IExternalEntity.Type => _type;

        public bool IsPageable => _serviceCapabilities2.PagingCapabilities.IsOnlyServerPagable || IsDelegatable;

        public bool IsConvertingDisplayNameMapping => false;

        BidirectionalDictionary<string, string> IDisplayMapped<string>.DisplayNameMapping => _displayNameMapping;

        BidirectionalDictionary<string, string> IDisplayMapped<string>.PreviousDisplayNameMapping => _previousDisplayNameMapping;

        public IReadOnlyList<string> GetKeyColumns() => _externalTableMetadata?.KeyColumns ?? new List<string>();

        IEnumerable<string> IExternalTabularDataSource.GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(string selectColumnName) => _externalTableMetadata != null && _externalTableMetadata.CanIncludeSelect(selectColumnName);

        bool IExternalTabularDataSource.CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName)
        {
            throw new System.NotImplementedException();
        }

        bool IExternalTabularDataSource.CanIncludeExpand(IExpandInfo expandToAdd)
        {
            throw new System.NotImplementedException();
        }

        bool IExternalTabularDataSource.CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd)
        {
            throw new System.NotImplementedException();
        }

        private static List<OperationCapabilityMetadata> GetCapabilityMetadata(DType type, ServiceCapabilities2 serviceCapabilities)
        {
            DPath GetDPath(string prop) => DPath.Root.Append(new DName(prop));

            void AddOrUpdate(Dictionary<DPath, DelegationCapability> dic, string prop, DelegationCapability capability)
            {
                DPath dPath = GetDPath(prop);

                if (!dic.TryGetValue(dPath, out DelegationCapability existingCapability))
                {
                    dic.Add(dPath, capability);
                }
                else
                {
                    dic[dPath] = new DelegationCapability(existingCapability.Capabilities | capability.Capabilities);
                }
            }

            List<OperationCapabilityMetadata> capabilities = new List<OperationCapabilityMetadata>();

            Dictionary<DPath, DelegationCapability> columnRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (serviceCapabilities?.FilterRestriction?.NonFilterableProperties != null)
            {
                foreach (string nonFilterableProperties in serviceCapabilities.FilterRestriction.NonFilterableProperties)
                {
                    AddOrUpdate(columnRestrictions, nonFilterableProperties, DelegationCapability.Filter);
                }
            }

            Dictionary<DPath, DelegationCapability> columnCapabilities = new Dictionary<DPath, DelegationCapability>();

            if (serviceCapabilities?.ColumnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase2> kvp in serviceCapabilities.ColumnsCapabilities)
                {
                    if (kvp.Value is ColumnCapabilities2 cc)
                    {
                        DelegationCapability columnDelegationCapability = DelegationCapability.None;

                        if (cc.Capabilities?.FilterFunctions != null)
                        {
                            foreach (string columnFilterFunction in cc.Capabilities.FilterFunctions)
                            {
                                if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(columnFilterFunction, out DelegationCapability filterFunctionCapability))
                                {
                                    columnDelegationCapability |= filterFunctionCapability;
                                }
                            }
                        }

                        if (columnDelegationCapability.Capabilities != DelegationCapability.None && !columnRestrictions.ContainsKey(GetDPath(kvp.Key)))
                        {
                            AddOrUpdate(columnCapabilities, kvp.Key, columnDelegationCapability | DelegationCapability.Filter);
                        }

                        if (cc.Capabilities.IsChoice == true && !columnRestrictions.ContainsKey(GetDPath(IsChoiceValue)))
                        {
                            AddOrUpdate(columnCapabilities, IsChoiceValue, columnDelegationCapability | DelegationCapability.Filter);
                        }
                    }
                    else if (kvp.Value is ComplexColumnCapabilities2)
                    {
                        throw new NotImplementedException($"ComplexColumnCapabilities not supported yet");
                    }
                    else
                    {
                        throw new NotImplementedException($"Unknown ColumnCapabilitiesBase, type {kvp.Value.GetType().Name}");
                    }
                }
            }

            DelegationCapability filterFunctionSupportedByAllColumns = DelegationCapability.None;

            if (serviceCapabilities?.FilterFunctions != null)
            {
                foreach (string globalFilterFunction in serviceCapabilities.FilterFunctions)
                {
                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalFilterFunction, out DelegationCapability globalFilterFunctionCapability))
                    {
                        filterFunctionSupportedByAllColumns |= globalFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            DelegationCapability? filterFunctionsSupportedByTable = null;

            if (serviceCapabilities?.FilterSupportedFunctions != null)
            {
                filterFunctionsSupportedByTable = DelegationCapability.None;

                foreach (string globalSupportedFilterFunction in serviceCapabilities.FilterSupportedFunctions)
                {
                    if (DelegationCapability.OperatorToDelegationCapabilityMap.TryGetValue(globalSupportedFilterFunction, out DelegationCapability globalSupportedFilterFunctionCapability))
                    {
                        filterFunctionsSupportedByTable |= globalSupportedFilterFunctionCapability | DelegationCapability.Filter;
                    }
                }
            }

            Dictionary<DPath, DelegationCapability> groupByRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (serviceCapabilities?.GroupRestriction?.UngroupableProperties != null)
            {
                foreach (string ungroupableProperty in serviceCapabilities.GroupRestriction.UngroupableProperties)
                {
                    AddOrUpdate(groupByRestrictions, ungroupableProperty, DelegationCapability.Group);
                }
            }

            Dictionary<DPath, DPath> oDataReplacements = new Dictionary<DPath, DPath>();

            if (serviceCapabilities?.ColumnsCapabilities != null)
            {
                foreach (KeyValuePair<string, ColumnCapabilitiesBase2> kvp in serviceCapabilities.ColumnsCapabilities)
                {
                    if (kvp.Value is ColumnCapabilities2 cc)
                    {
                        DPath columnPath = GetDPath(kvp.Key);
                        DelegationCapability columnDelegationCapability = DelegationCapability.None;

                        if (cc.Capabilities.IsChoice == true)
                        {
                            oDataReplacements.Add(columnPath.Append(GetDPath(IsChoiceValue)), columnPath);
                        }

                        if (!string.IsNullOrEmpty(cc.Capabilities.QueryAlias))
                        {
                            oDataReplacements.Add(columnPath, GetReplacementPath(cc.Capabilities.QueryAlias, columnPath));
                        }
                    }
                    else if (kvp.Value is ComplexColumnCapabilities2)
                    {
                        throw new NotImplementedException($"ComplexColumnCapabilities not supported yet");
                    }
                    else
                    {
                        throw new NotImplementedException($"Unknown ColumnCapabilitiesBase, type {kvp.Value.GetType().Name}");
                    }
                }
            }

            Dictionary<DPath, DelegationCapability> sortRestrictions = new Dictionary<DPath, DelegationCapability>();

            if (serviceCapabilities?.SortRestriction?.UnsortableProperties != null)
            {
                foreach (string unsortableProperty in serviceCapabilities.SortRestriction.UnsortableProperties)
                {
                    AddOrUpdate(sortRestrictions, unsortableProperty, DelegationCapability.Sort);
                }
            }

            if (serviceCapabilities?.SortRestriction?.AscendingOnlyProperties != null)
            {
                foreach (string ascendingOnlyProperty in serviceCapabilities.SortRestriction.AscendingOnlyProperties)
                {
                    AddOrUpdate(sortRestrictions, ascendingOnlyProperty, DelegationCapability.SortAscendingOnly);
                }
            }

            FilterOpMetadata filterOpMetadata = new FilterOpMetadata(type, columnRestrictions, columnCapabilities, filterFunctionSupportedByAllColumns, filterFunctionsSupportedByTable);
            GroupOpMetadata groupOpMetadata = new GroupOpMetadata(type, groupByRestrictions);
            ODataOpMetadata oDataOpMetadata = new ODataOpMetadata(type, oDataReplacements);
            SortOpMetadata sortOpMetadata = new SortOpMetadata(type, sortRestrictions);

            capabilities.Add(filterOpMetadata);
            capabilities.Add(groupOpMetadata);
            capabilities.Add(oDataOpMetadata);
            capabilities.Add(sortOpMetadata);

            return capabilities;
        }

        internal static DPath GetReplacementPath(string alias, DPath currentColumnPath)
        {
            if (alias.Contains("/"))
            {
                var fullPath = DPath.Root;

                foreach (var name in alias.Split('/'))
                {
                    fullPath = fullPath.Append(new DName(name));
                }

                return fullPath;
            }
            else
            {
                // Task 5593666: This is temporary to not cause regressions while sharepoint switches to using full query param
                return currentColumnPath.Append(new DName(alias));
            }
        }
    }
}
