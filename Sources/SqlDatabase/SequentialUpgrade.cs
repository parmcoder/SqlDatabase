﻿using System.Collections.Generic;
using System.Text;
using SqlDatabase.Scripts;

namespace SqlDatabase
{
    internal sealed class SequentialUpgrade
    {
        public IUpgradeDatabase Database { get; set; }

        public IUpgradeScriptSequence ScriptSequence { get; set; }

        public ILogger Log { get; set; }

        public void Execute()
        {
            Log.Info("get database version");
            var version = Database.GetCurrentVersion();
            Log.Info("current database version is {0}".FormatWith(version));

            var sequences = ScriptSequence.BuildSequence(version);
            if (sequences.Count == 0)
            {
                Log.Info("the database is up-to-date.");
                return;
            }

            ShowMigrationSequence(sequences);

            Database.BeforeUpgrade();

            foreach (var step in sequences)
            {
                Log.Info("execute {0}".FormatWith(step.Script.DisplayName));
                Database.Execute(step.Script, step.From, step.To);
            }
        }

        private void ShowMigrationSequence(IList<ScriptStep> sequence)
        {
            var message = new StringBuilder()
                .AppendFormat("sequence: {0}", sequence[0].From);

            foreach (var step in sequence)
            {
                message.AppendFormat(" => {0}", step.To);
            }

            Log.Info(message.ToString());
        }
    }
}