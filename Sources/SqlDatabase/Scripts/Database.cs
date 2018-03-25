﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using SqlDatabase.Configuration;

namespace SqlDatabase.Scripts
{
    internal sealed class Database : ICreateDatabase, IUpgradeDatabase
    {
        public Database()
        {
            Variables = new Variables();
        }

        public string ConnectionString { get; set; }

        public AppConfiguration Configuration { get; set; }

        public ILogger Log { get; set; }

        public TransactionMode Transaction { get; set; }

        internal Variables Variables { get; }

        public Version GetCurrentVersion()
        {
            string version;

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandTimeout = 0;
                command.CommandText = (Configuration ?? AppConfiguration.GetCurrent()).GetCurrentVersionScript;

                connection.Open();
                version = Convert.ToString(command.ExecuteScalar());
            }

            try
            {
                return new Version(version);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("The cuurent value [{0}] of database version is invalid.");
            }
        }

        public void BeforeCreate()
        {
            using (var connection = CreateConnection(true))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select @@version";

                connection.Open();
                Log.Info(Convert.ToString(command.ExecuteScalar()));
            }
        }

        public void BeforeUpgrade()
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select @@version";

                connection.Open();
                Log.Info(Convert.ToString(command.ExecuteScalar()));
            }
        }

        public void Execute(IScript script, Version currentVersion, Version targetVersion)
        {
            Variables.CurrentVersion = currentVersion.ToString();
            Variables.TargetVersion = targetVersion.ToString();
            Variables.DatabaseName = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;

            var updateVersion = TextScript.ApplyVariables(
                (Configuration ?? AppConfiguration.GetCurrent()).SetCurrentVersionScript,
                Variables);

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                connection.InfoMessage += OnConnectionInfoMessage;

                command.CommandTimeout = 0;
                connection.Open();

                using (var transaction = Transaction == TransactionMode.PerStep ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null)
                {
                    command.Transaction = transaction;

                    var timer = Stopwatch.StartNew();
                    script.Execute(command, Variables, Log);
                    Log.Info("execution time {0}".FormatWith(timer.Elapsed));

                    Log.Info("change version to {0}".FormatWith(targetVersion));
                    if (!Variables.DatabaseName.Equals(connection.Database, StringComparison.OrdinalIgnoreCase))
                    {
                        command.CommandText = "USE [{0}]".FormatWith(Variables.DatabaseName);
                        command.ExecuteNonQuery();
                    }

                    command.CommandText = updateVersion;
                    command.ExecuteNonQuery();

                    transaction?.Commit();
                }
            }
        }

        public void Execute(IScript script)
        {
            Variables.DatabaseName = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;

            bool useMaster;

            using (var connection = CreateConnection(true))
            using (var command = connection.CreateCommand())
            {
                command.CommandTimeout = 0;
                connection.Open();

                command.CommandText = "SELECT 1 FROM sys.databases WHERE Name=N'{0}'".FormatWith(Variables.DatabaseName);
                var value = command.ExecuteScalar();

                useMaster = value == null || Convert.IsDBNull(value);
            }

            using (var connection = CreateConnection(useMaster))
            using (var command = connection.CreateCommand())
            {
                connection.InfoMessage += OnConnectionInfoMessage;

                command.CommandTimeout = 0;
                connection.Open();

                script.Execute(command, Variables, Log);
            }
        }

        private void OnConnectionInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            Log.Info("output: {0}".FormatWith(e.ToString()));
        }

        private SqlConnection CreateConnection(bool switchToMaster = false)
        {
            var connectionString = ConnectionString;
            if (switchToMaster)
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                builder.InitialCatalog = "master";
                connectionString = builder.ToString();
            }

            var connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}