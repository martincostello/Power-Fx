﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    // Used by ConnectorDataSource.GetTablesAsync
    internal class GetTables : ISupportsPostProcessing
    {
        [JsonPropertyName("value")]
        public List<RawTable> Value { get; set; }

        public void PostProcess()
        {
            Value = Value.Select(rt => new RawTable() { Name = rt.Name, DisplayName = rt.DisplayName.Split('.').Last().Replace("[", string.Empty).Replace("]", string.Empty) }).ToList();
        }
    }

    internal interface ISupportsPostProcessing
    {
        void PostProcess();
    }

    internal class RawTable
    {
        // Logical Name
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; }

        public override string ToString() => $"{DisplayName}: {Name}";
    }

    // Used by ConnectorDataSource.GetDatasetsMetadataAsync
    public class DatasetMetadata
    {
        [JsonPropertyName("tabular")]
        public MetadataTabular Tabular { get; set; }

        [JsonPropertyName("blob")]
        public MetadataBlob Blob { get; set; }

        [JsonPropertyName("datasetFormat")]
        public string DatasetFormat { get; set; }

        [JsonPropertyName("parameters")]
        public IReadOnlyCollection<MetadataParameter> Parameters { get; set; }

        public bool IsDoubleEncoding => Tabular?.UrlEncoding == "double";
    }

    public class MetadataTabular
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("urlEncoding")]
        public string UrlEncoding { get; set; }

        [JsonPropertyName("tableDisplayName")]
        public string TableDisplayName { get; set; }

        [JsonPropertyName("tablePluralName")]
        public string TablePluralName { get; set; }
    }

    public class MetadataBlob
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("urlEncoding")]
        public string UrlEncoding { get; set; }
    }

    public class MetadataParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("urlEncoding")]
        public string UrlEncoding { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("x-ms-summary")]
        public string XMsSummary { get; set; }

        [JsonPropertyName("x-ms-dynamic-values")]
        public MetadataDynamicValues XMsDynamicValues { get; set; }
    }

    public class MetadataDynamicValues
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("value-collection")]
        public string ValueCollection { get; set; }

        [JsonPropertyName("value-path")]
        public string ValuePath { get; set; }

        [JsonPropertyName("value-title")]
        public string ValueTitle { get; set; }
    }
}
