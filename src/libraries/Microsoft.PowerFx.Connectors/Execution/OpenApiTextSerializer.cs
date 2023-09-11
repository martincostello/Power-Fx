﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.PowerFx.Connectors.Execution
{
    internal class OpenApiTextSerializer : FormulaValueSerializer
    {
        private readonly StringBuilder _writer;

        public OpenApiTextSerializer(IConvertToUTC utcConverter, bool schemaLessBody)
            : base(utcConverter, schemaLessBody)
        {
            _writer = new StringBuilder(1024);
        }

        protected override void EndArray()
        {            
        }

        protected override void EndObject(string name = null)
        {            
        }

        protected override void StartArray(string name = null)
        {         
        }

        protected override void StartArrayElement(string name)
        {         
        }

        protected override void StartObject(string name = null)
        {         
        }

        protected override void WriteBooleanValue(bool booleanValue)
        {
            _writer.Append(booleanValue ? "true" : "false");
        }

        protected override void WriteDateTimeValue(DateTime dateTimeValue)
        {            
            _writer.Append(dateTimeValue.ToString("o", CultureInfo.InvariantCulture));           
        }

        protected override void WriteDateValue(DateTime dateValue)
        {
            _writer.Append(dateValue.Date.ToString("o", CultureInfo.InvariantCulture).Substring(0, 10));
        }

        protected override void WriteNullValue()
        {            
        }

        protected override void WriteNumberValue(double numberValue)
        {
            _writer.Append(numberValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void WriteDecimalValue(decimal decimalValue)
        {
            _writer.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void WritePropertyName(string name)
        {            
        }

        protected override void WriteStringValue(string stringValue)
        {
            _writer.Append(stringValue);
        }     

        internal override string GetResult()
        {
            return _writer.ToString();
        }

        internal override void StartSerialization(string refId)
        {            
        }

        internal override void EndSerialization()
        {
        }
    }
}
