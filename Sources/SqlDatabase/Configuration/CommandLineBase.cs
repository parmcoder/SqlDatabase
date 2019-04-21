﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SqlDatabase.Commands;
using SqlDatabase.Scripts;

namespace SqlDatabase.Configuration
{
    internal abstract class CommandLineBase : ICommandLine
    {
        public SqlConnectionStringBuilder Connection { get; set; }

        public TransactionMode Transaction { get; set; }

        public IList<string> Scripts { get; } = new List<string>();

        public IDictionary<string, string> Variables { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string ConfigurationFile { get; set; }

        public void Parse(CommandLine args)
        {
            foreach (var arg in args.Args)
            {
                var isParsed = (arg.IsPair && TryParseKnownPair(arg)) || ParseArg(arg);
                if (!isParsed)
                {
                    throw new InvalidCommandLineException("Unknown argument [{0}].".FormatWith(arg));
                }
            }

            if (Connection == null)
            {
                throw new InvalidCommandLineException("Argument {0} is not specified.".FormatWith(Arg.Database));
            }

            if (Scripts.Count == 0)
            {
                throw new InvalidCommandLineException("Argument {0} is not specified.".FormatWith(Arg.Scripts));
            }

            Validate();
        }

        public abstract ICommand CreateCommand(ILogger logger);

        internal Database CreateDatabase(ILogger logger, IConfigurationManager configuration)
        {
            var database = new Database
            {
                ConnectionString = Connection.ToString(),
                Log = logger,
                Configuration = configuration.SqlDatabase,
                Transaction = Transaction
            };

            var configurationVariables = configuration.SqlDatabase.Variables;
            foreach (var name in configurationVariables.AllKeys)
            {
                database.Variables.SetValue(VariableSource.ConfigurationFile, name, configurationVariables[name].Value);
            }

            foreach (var entry in Variables)
            {
                database.Variables.SetValue(VariableSource.CommandLine, entry.Key, entry.Value);
            }

            var invalidNames = database
                .Variables
                .GetNames()
                .OrderBy(i => i)
                .Where(i => !SqlScriptVariableParser.IsValidVariableName(i))
                .Select(i => "[{0}]".FormatWith(i))
                .ToList();

            if (invalidNames.Count == 1)
            {
                throw new InvalidOperationException("The variable name {0} is invalid.".FormatWith(invalidNames[0]));
            }

            if (invalidNames.Count > 1)
            {
                throw new InvalidOperationException("The following variable names are invalid: {0}.".FormatWith(string.Join(", ", invalidNames)));
            }

            return database;
        }

        protected internal virtual void Validate()
        {
        }

        protected virtual bool ParseArg(Arg arg)
        {
            return false;
        }

        private bool TryParseKnownPair(Arg arg)
        {
            if (Arg.Database.Equals(arg.Key, StringComparison.OrdinalIgnoreCase))
            {
                SetConnection(arg.Value);
                return true;
            }

            if (Arg.Scripts.Equals(arg.Key, StringComparison.OrdinalIgnoreCase))
            {
                SetScripts(arg.Value);
                return true;
            }

            if (Arg.Transaction.Equals(arg.Key, StringComparison.OrdinalIgnoreCase))
            {
                SetTransaction(arg.Value);
                return true;
            }

            if (arg.Key.StartsWith(Arg.Variable, StringComparison.OrdinalIgnoreCase))
            {
                SetVariable(arg.Key.Substring(Arg.Variable.Length), arg.Value);
                return true;
            }

            if (Arg.Configuration.Equals(arg.Key, StringComparison.OrdinalIgnoreCase))
            {
                SetConfigurationFile(arg.Value);
                return true;
            }

            return false;
        }

        private void SetConnection(string connectionString)
        {
            try
            {
                Connection = new SqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidCommandLineException(Arg.Database, "Invalid connection string value.", ex);
            }
        }

        private void SetScripts(string value)
        {
            Scripts.Add(value);
        }

        private void SetTransaction(string modeName)
        {
            if (!Enum.TryParse<TransactionMode>(modeName, true, out var mode))
            {
                throw new InvalidCommandLineException(Arg.Transaction, "Unknown transaction mode [{0}].".FormatWith(modeName));
            }

            Transaction = mode;
        }

        private void SetVariable(string name, string value)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidCommandLineException(Arg.Variable, "Invalid variable name [{0}].".FormatWith(name));
            }

            if (Variables.ContainsKey(name))
            {
                throw new InvalidCommandLineException(Arg.Variable, "Variable with name [{0}] is duplicated.".FormatWith(name));
            }

            Variables.Add(name, value);
        }

        private void SetConfigurationFile(string configurationFile)
        {
            ConfigurationFile = configurationFile;
        }
    }
}