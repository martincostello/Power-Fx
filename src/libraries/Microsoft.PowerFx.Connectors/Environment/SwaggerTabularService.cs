﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Swagger based CDP tabular service
    public sealed class SwaggerTabularService : CdpTabularService
    {
        public string Namespace => $"_tbl_{ConnectionId}";

        public string ConnectionId => _connectionId;

        private readonly string _connectionId;

        private IReadOnlyList<ConnectorFunction> _tabularFunctions;

        private ConnectorFunction _metadataService;

        private ConnectorFunction _getItems;

        private readonly OpenApiDocument _openApiDocument;

        private readonly IReadOnlyDictionary<string, FormulaValue> _globalValues;

        private readonly ConnectorLogger _connectorLogger;

        private readonly PowerFxConfig _config;

        public SwaggerTabularService(PowerFxConfig config, OpenApiDocument openApiDocument, IReadOnlyDictionary<string, FormulaValue> globalValues, HttpClient httpClient, ConnectorLogger configurationLogger = null)
            : base(GetDataSetName(globalValues), GetTableName(globalValues), httpClient)
        {
            _config = config;
            _openApiDocument = openApiDocument;
            _globalValues = globalValues;
            _connectorLogger = configurationLogger;
            _connectionId = TryGetString("connectionId", globalValues, out string connectorId) ? connectorId : throw new InvalidOperationException("Cannot determine connectionId.");
        }

        //// TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        internal ConnectorFunction MetadataService => _metadataService ??= GetMetadataService();

        public override async Task<RecordType> GetSchemaAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConnectorSettings connectorSettings = new ConnectorSettings(Namespace)
            {
                IncludeInternalFunctions = true,
                Compatibility = ConnectorCompatibility.SwaggerCompatibility
            };

            // Swagger based tabular connectors
            _tabularFunctions = _config.AddActionConnector(connectorSettings, _openApiDocument, _connectorLogger, _globalValues);

            BaseRuntimeConnectorContext runtimeConnectorContext = new RawRuntimeConnectorContext(_httpClient);
            FormulaValue schema = await MetadataService.InvokeAsync(Array.Empty<FormulaValue>(), runtimeConnectorContext, cancellationToken).ConfigureAwait(false);

            return schema is StringValue str ? GetSchema(str.Value) : null;
        }

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        internal ConnectorFunction GetItems => _getItems ??= GetItemsFunction();

        public override async Task<ICollection<DValue<RecordValue>>> GetItemsAsync(IServiceProvider serviceProvider, ODataParameters odataParameters, CancellationToken cancellationToken)
        {
            BaseRuntimeConnectorContext runtimeConnectorContext = serviceProvider.GetService<BaseRuntimeConnectorContext>() ?? throw new InvalidOperationException("Cannot determine runtime connector context.");

            List<NamedValue> optionalParameters = new List<NamedValue>();
            if (odataParameters != default)
            {
                if (odataParameters.Count)
                {
                    optionalParameters.Add(new NamedValue("$count", FormulaValue.New(true)));
                }

                if (!string.IsNullOrEmpty(odataParameters.Filter))
                {
                    optionalParameters.Add(new NamedValue("$filter", FormulaValue.New(odataParameters.Filter)));
                }

                if (!string.IsNullOrEmpty(odataParameters.OrderBy))
                {
                    optionalParameters.Add(new NamedValue("$orderby", FormulaValue.New(odataParameters.OrderBy)));
                }

                if (odataParameters.Top > 0)
                {
                    optionalParameters.Add(new NamedValue("$top", FormulaValue.New(odataParameters.Top)));
                }
            }

            FormulaValue[] parameters = optionalParameters.Any() ? new FormulaValue[] { FormulaValue.NewRecordFromFields(optionalParameters.ToArray()) } : Array.Empty<FormulaValue>();

            // Notice that there is no paging here, just get 1 page
            // Use WithRawResults to ignore _getItems return type which is in the form of ![value:*[dynamicProperties:![]]] (ie. without the actual type)
            FormulaValue rowsRaw = await GetItems.InvokeAsync(parameters, runtimeConnectorContext.WithRawResults(), CancellationToken.None).ConfigureAwait(false);

            return rowsRaw is StringValue sv ? GetResult(sv.Value) : Array.Empty<DValue<RecordValue>>();
        }

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}

        private static string GetDataSetName(IReadOnlyDictionary<string, FormulaValue> globalValues) =>
            TryGetString("dataset", globalValues, out string dataset)
            ? dataset
            : TryGetString("server", globalValues, out string server) && TryGetString("database", globalValues, out string database)
            ? $"{server},{database}"
            : throw new InvalidOperationException("Cannot determine dataset name.");

        private static string GetTableName(IReadOnlyDictionary<string, FormulaValue> globalValues) =>
            TryGetString("table", globalValues, out string table)
            ? table
            : throw new InvalidOperationException("Cannot determine table name.");

        private static bool TryGetString(string name, IReadOnlyDictionary<string, FormulaValue> globalValues, out string str)
        {
            if (globalValues.TryGetValue(name, out FormulaValue fv) && fv is StringValue sv)
            {
                str = sv.Value;
                return true;
            }

            str = null;
            return false;
        }

        private const string MetadataServiceRegex = @"/\$metadata\.json/datasets/{[^{}]+}(,{[^{}]+})?/tables/{[^{}]+}$";

        private const string GetItemsRegex = @"/datasets/{[^{}]+}(,{[^{}]+})?/tables/{[^{}]+}/items$";

        private const string NameVersionRegex = @"V(?<n>[0-9]{0,2})$";

        private ConnectorFunction GetMetadataService()
        {
            ConnectorFunction[] functions = _tabularFunctions.Where(tf => tf.RequiredParameters.Length == 0 && new Regex(MetadataServiceRegex).IsMatch(tf.OperationPath)).ToArray();

            if (functions.Length == 0)
            {
                throw new PowerFxConnectorException("Cannot determine metadata service function.");
            }

            if (functions.Length > 1)
            {
                // When GetTableTabularV2, GetTableTabular exist, return highest version
                return functions[functions.Select((cf, i) => (index: i, version: int.Parse("0" + new Regex(NameVersionRegex).Match(cf.Name).Groups["n"].Value, CultureInfo.InvariantCulture))).OrderByDescending(x => x.version).First().index];
            }

            return functions[0];
        }

        private ConnectorFunction GetItemsFunction()
        {
            ConnectorFunction[] functions = _tabularFunctions.Where(tf => tf.RequiredParameters.Length == 0 && new Regex(GetItemsRegex).IsMatch(tf.OperationPath)).ToArray();

            if (functions.Length == 0)
            {
                throw new PowerFxConnectorException("Cannot determine GetItems function.");
            }

            if (functions.Length > 1)
            {
                throw new PowerFxConnectorException("Multiple GetItems functions found.");
            }

            return functions[0];
        }

        private class RawRuntimeConnectorContext : BaseRuntimeConnectorContext
        {
            private readonly HttpMessageInvoker _invoker;

            internal RawRuntimeConnectorContext(HttpMessageInvoker invoker)
            {
                _invoker = invoker;
            }

            internal override bool ReturnRawResults => true;

            public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

            public override HttpMessageInvoker GetInvoker(string @namespace) => _invoker;
        }
    }
}
