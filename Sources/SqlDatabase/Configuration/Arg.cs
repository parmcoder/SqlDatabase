﻿namespace SqlDatabase.Configuration
{
    internal struct Arg
    {
        internal const string Base64Sign = "+";
        internal const string Sign = "-";

        internal const string Database = "database";
        internal const string Scripts = "from";
        internal const string Variable = "var";
        internal const string Configuration = "configuration";
        internal const string Transaction = "transaction";
        internal const string PreFormatOutputLogs = "preFormatOutputLogs";

        public Arg(string key, string value)
        {
            IsPair = true;
            Key = key;
            Value = value;
        }

        public Arg(string value)
        {
            IsPair = false;
            Key = null;
            Value = value;
        }

        public bool IsPair { get; }

        public string Key { get; }

        public string Value { get; }

        public override string ToString()
        {
            if (IsPair)
            {
                return "{0}={1}".FormatWith(Key, Value);
            }

            return Value;
        }
    }
}