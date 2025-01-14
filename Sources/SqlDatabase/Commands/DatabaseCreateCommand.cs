﻿using SqlDatabase.Adapter;
using SqlDatabase.Configuration;
using SqlDatabase.Scripts;
using SqlDatabase.Sequence;

namespace SqlDatabase.Commands;

internal sealed class DatabaseCreateCommand : DatabaseCommandBase
{
    public DatabaseCreateCommand(
        ICreateScriptSequence scriptSequence,
        IScriptResolver scriptResolver,
        IDatabase database,
        ILogger log)
        : base(database, log)
    {
        ScriptSequence = scriptSequence;
        ScriptResolver = scriptResolver;
    }

    public ICreateScriptSequence ScriptSequence { get; }

    public IScriptResolver ScriptResolver { get; }

    protected override void Greet(string databaseLocation)
    {
        Log.Info($"Create {databaseLocation}");
    }

    protected override void ExecuteCore()
    {
        var sequences = ScriptSequence.BuildSequence();
        if (sequences.Count == 0)
        {
            throw new ConfigurationErrorsException("scripts to create database not found.");
        }

        ScriptResolver.InitializeEnvironment(Log, sequences);

        foreach (var script in sequences)
        {
            var timer = Stopwatch.StartNew();
            Log.Info($"execute {script.DisplayName} ...");

            using (Log.Indent())
            {
                Database.Execute(script);
            }

            Log.Info($"done in {timer.Elapsed}");
        }
    }
}