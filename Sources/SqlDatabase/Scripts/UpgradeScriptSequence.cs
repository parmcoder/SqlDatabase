﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlDatabase.IO;
using SqlDatabase.Scripts.UpgradeInternal;

namespace SqlDatabase.Scripts
{
    internal sealed class UpgradeScriptSequence : IUpgradeScriptSequence
    {
        public IList<IFileSystemInfo> Sources { get; set; } = new List<IFileSystemInfo>();

        public IScriptFactory ScriptFactory { get; set; }

        public IModuleVersionResolver VersionResolver { get; set; }

        public IList<ScriptStep> BuildSequence()
        {
            var scripts = new UpgradeScriptCollection();
            scripts.LoadFrom(Sources, ScriptFactory);

            // folder is empty
            if (scripts.ModuleNames.Count == 0)
            {
                return new ScriptStep[0];
            }

            foreach (var moduleName in scripts.ModuleNames.ToArray())
            {
                scripts.BuildModuleSequence(moduleName, VersionResolver.GetCurrentVersion(moduleName));
            }

            // no updates
            if (scripts.ModuleNames.Count == 0)
            {
                return new ScriptStep[0];
            }

            // no modules
            if (scripts.ModuleNames.Count == 1 && string.IsNullOrEmpty(scripts.ModuleNames.First()))
            {
                return scripts.GetSteps(scripts.ModuleNames.First());
            }

            scripts.LoadDependencies();

            foreach (var moduleName in scripts.ModuleNames)
            {
                scripts.ValidateModuleDependencies(moduleName, VersionResolver);
            }

            // only one module to update
            if (scripts.ModuleNames.Count == 1)
            {
                return scripts.GetSteps(scripts.ModuleNames.First());
            }

            return BuildSequence(scripts);
        }

        private IList<ScriptStep> BuildSequence(UpgradeScriptCollection scripts)
        {
            var sequence = new List<ScriptStep>();

            var versionByModule = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);
            foreach (var moduleName in scripts.ModuleNames)
            {
                versionByModule.Add(moduleName, VersionResolver.GetCurrentVersion(moduleName));
            }

            while (scripts.ModuleNames.Count > 0)
            {
                var nextStep = default(ScriptStep);
                string nextStepModuleName = null;

                foreach (var moduleName in scripts.ModuleNames)
                {
                    nextStep = scripts.GetNextStep(moduleName);
                    if (scripts.TestStep(versionByModule, moduleName, nextStep))
                    {
                        nextStepModuleName = moduleName;
                        break;
                    }
                }

                if (nextStepModuleName == null)
                {
                    ThrowSequenceNotFound(scripts, sequence);
                }
                else
                {
                    sequence.Add(nextStep);
                    scripts.RemoveNextStep(nextStepModuleName);
                    versionByModule[nextStepModuleName] = nextStep.To;
                }
            }

            return sequence;
        }

        private void ThrowSequenceNotFound(UpgradeScriptCollection scripts, IList<ScriptStep> sequence)
        {
            var message = new StringBuilder("Not possible to build upgrade sequence. Current sequence: ");

            if (sequence.Count == 0)
            {
                message.Append("is empty");
            }
            else
            {
                message.Append(sequence[0].From);
                foreach (var step in sequence)
                {
                    message.AppendFormat(" => {0}", step.To);
                }
            }

            message.Append(". Next candidate");
            if (scripts.ModuleNames.Count > 1)
            {
                message.Append("s: ");
            }
            else
            {
                message.Append(": ");
            }

            var comma = false;
            foreach (var moduleName in scripts.ModuleNames)
            {
                if (comma)
                {
                    message.Append(", ");
                }

                message.Append(scripts.GetNextStep(moduleName).Script.DisplayName);
                comma = true;
            }

            message.Append(".");

            throw new InvalidOperationException(message.ToString());
        }
    }
}
