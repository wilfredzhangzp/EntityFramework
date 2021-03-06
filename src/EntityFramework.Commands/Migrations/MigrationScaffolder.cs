// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Migrations.History;
using Microsoft.Data.Entity.Relational.Migrations.Infrastructure;
using Microsoft.Data.Entity.Relational.Migrations.Operations;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;

namespace Microsoft.Data.Entity.Commands.Migrations
{
    public class MigrationScaffolder
    {
        private readonly Type _contextType;
        private readonly IModel _model;
        private readonly IMigrationAssembly _migrationAssembly;
        private readonly IModelDiffer _modelDiffer;
        private readonly IMigrationIdGenerator _idGenerator;
        private readonly MigrationCodeGenerator _migrationCodeGenerator;
        private readonly IHistoryRepository _historyRepository;
        private readonly LazyRef<ILogger> _logger;

        public MigrationScaffolder(
            [NotNull] DbContext context,
            [NotNull] IModel model,
            [NotNull] IMigrationAssembly migrationAssembly,
            [NotNull] IModelDiffer modelDiffer,
            [NotNull] IMigrationIdGenerator idGenerator,
            [NotNull] MigrationCodeGenerator migrationCodeGenerator,
            [NotNull] IHistoryRepository historyRepository,
            [NotNull] ILoggerFactory loggerFactory)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(model, nameof(model));
            Check.NotNull(migrationAssembly, nameof(migrationAssembly));
            Check.NotNull(modelDiffer, nameof(modelDiffer));
            Check.NotNull(idGenerator, nameof(idGenerator));
            Check.NotNull(migrationCodeGenerator, nameof(migrationCodeGenerator));
            Check.NotNull(historyRepository, nameof(historyRepository));
            Check.NotNull(loggerFactory, nameof(loggerFactory));

            _contextType = context.GetType();
            _model = model;
            _migrationAssembly = migrationAssembly;
            _modelDiffer = modelDiffer;
            _idGenerator = idGenerator;
            _migrationCodeGenerator = migrationCodeGenerator;
            _historyRepository = historyRepository;
            _logger = new LazyRef<ILogger>(loggerFactory.CreateLogger<MigrationScaffolder>);
        }

        protected virtual string ProductVersion =>
            typeof(MigrationScaffolder).GetTypeInfo().Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public virtual ScaffoldedMigration ScaffoldMigration(
            [NotNull] string migrationName,
            [NotNull] string rootNamespace)
        {
            Check.NotEmpty(migrationName, nameof(migrationName));
            Check.NotEmpty(rootNamespace, nameof(rootNamespace));

            if (_migrationAssembly.Migrations.Any(
                m => _idGenerator.GetName(m.Id).Equals(migrationName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(Strings.DuplicateMigrationName(migrationName));
            }

            var lastMigration = _migrationAssembly.Migrations.LastOrDefault();
            var migrationNamespace = lastMigration?.GetType().Namespace ?? rootNamespace + ".Migrations";
            var modelSnapshot = _migrationAssembly.ModelSnapshot;
            var lastModel = modelSnapshot?.Model;
            var upOperations = _modelDiffer.GetDifferences(lastModel, _model);
            var downOperations = upOperations.Any()
                ? _modelDiffer.GetDifferences(_model, lastModel)
                : new List<MigrationOperation>();
            var migrationId = _idGenerator.CreateId(migrationName);
            var modelSnapshotNamespace = modelSnapshot?.GetType().Namespace ?? migrationNamespace;
            var modelSnapshotName = modelSnapshot?.GetType().Name ?? _contextType.Name + "ModelSnapshot";

            var migrationCode = _migrationCodeGenerator.Generate(
                migrationNamespace,
                migrationName,
                upOperations,
                downOperations);
            var migrationMetadataCode = _migrationCodeGenerator.GenerateMetadata(
                migrationNamespace,
                _contextType,
                migrationName,
                migrationId,
                ProductVersion,
                _model);
            var modelSnapshotCode = _migrationCodeGenerator.GenerateSnapshot(
                modelSnapshotNamespace,
                _contextType,
                modelSnapshotName,
                _model);

            return new ScaffoldedMigration(
                _migrationCodeGenerator.Language,
                lastMigration?.Id,
                migrationCode,
                migrationId,
                migrationMetadataCode,
                GetSubnamespace(rootNamespace, migrationNamespace),
                modelSnapshotCode,
                modelSnapshotName,
                GetSubnamespace(rootNamespace, modelSnapshotNamespace));
        }

        protected virtual string GetSubnamespace([NotNull] string rootNamespace, [NotNull] string @namespace) =>
            @namespace == rootNamespace
                ? string.Empty
                : @namespace.StartsWith(rootNamespace + '.')
                    ? @namespace.Substring(rootNamespace.Length + 1)
                    : @namespace;

        // TODO: DRY (file names)
        public virtual MigrationFiles RemoveMigration([NotNull] string projectDir, [NotNull] string rootNamespace)
        {
            Check.NotEmpty(projectDir, nameof(projectDir));
            Check.NotEmpty(rootNamespace, nameof(rootNamespace));

            var files = new MigrationFiles();

            var modelSnapshot = _migrationAssembly.ModelSnapshot;
            if (modelSnapshot == null)
            {
                throw new InvalidOperationException(Strings.NoSnapshot);
            }

            var language = _migrationCodeGenerator.Language;

            IModel model = null;
            var migrations = _migrationAssembly.Migrations;
            if (migrations.Count != 0)
            {
                var migration = migrations.Last();
                model = migration.Target;

                if (!_modelDiffer.HasDifferences(model, modelSnapshot.Model))
                {
                    if (_historyRepository.GetAppliedMigrations().Any(
                        e => e.MigrationId.Equals(migration.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(Strings.UnapplyMigration(migration.Id));
                    }

                    var migrationFileName = migration.Id + language;
                    var migrationFile = TryGetProjectFile(projectDir, migrationFileName);
                    if (migrationFile != null)
                    {
                        _logger.Value.LogInformation(Strings.RemovingMigration(migration.Id));
                        // TODO: Test version control. If broken, delete and write files in the commands
                        File.Delete(migrationFile);
                        files.MigrationFile = migrationFile;
                    }
                    else
                    {
                        _logger.Value.LogWarning(Strings.NoMigrationFile(migrationFileName, migration.GetType().FullName));
                    }

                    var migrationMetadataFileName = migration.Id + ".Designer" + language;
                    var migrationMetadataFile = TryGetProjectFile(projectDir, migrationMetadataFileName);
                    if (migrationMetadataFile != null)
                    {
                        File.Delete(migrationMetadataFile);
                        files.MigrationMetadataFile = migrationMetadataFile;
                    }
                    else
                    {
                        _logger.Value.LogVerbose(Strings.NoMigrationMetadataFile(migrationMetadataFileName));
                    }

                    model = migrations.Count > 1 ? migrations[migrations.Count - 2].Target : null;
                }
                else
                {
                    _logger.Value.LogVerbose(Strings.ManuallyDeleted);
                }
            }

            var modelSnapshotName = modelSnapshot.GetType().Name;
            var modelSnapshotFileName = modelSnapshotName + language;
            var modelSnapshotFile = TryGetProjectFile(projectDir, modelSnapshotFileName);
            if (model == null)
            {
                if (modelSnapshotFile != null)
                {
                    _logger.Value.LogInformation(Strings.RemovingSnapshot);
                    File.Delete(modelSnapshotFile);
                    files.ModelSnapshotFile = modelSnapshotFile;
                }
                else
                {
                    _logger.Value.LogWarning(
                        Strings.NoSnapshotFile(modelSnapshotFileName, modelSnapshot.GetType().FullName));
                }
            }
            else
            {
                var modelSnapshotNamespace = modelSnapshot.GetType().Namespace;
                var modelSnapshotCode = _migrationCodeGenerator.GenerateSnapshot(
                    modelSnapshotNamespace,
                    _contextType,
                    modelSnapshotName,
                    model);

                if (modelSnapshotFile == null)
                {
                    modelSnapshotFile = Path.Combine(
                        GetDirectory(projectDir, null, GetSubnamespace(rootNamespace, modelSnapshotNamespace)),
                        modelSnapshotFileName);
                }

                _logger.Value.LogInformation(Strings.RevertingSnapshot);
                File.WriteAllText(modelSnapshotFile, modelSnapshotCode);
            }

            return files;
        }

        public virtual MigrationFiles Write([NotNull] string projectDir, [NotNull] ScaffoldedMigration migration)
        {
            Check.NotEmpty(projectDir, nameof(projectDir));
            Check.NotNull(migration, nameof(migration));

            var lastMigrationFileName = migration.LastMigrationId + migration.Language;
            var migrationDirectory = GetDirectory(projectDir, lastMigrationFileName, migration.MigrationSubnamespace);
            var migrationFile = Path.Combine(migrationDirectory, migration.MigrationId + migration.Language);
            var migrationMetadataFile = Path.Combine(migrationDirectory, migration.MigrationId + ".Designer" + migration.Language);
            var modelSnapshotFileName = migration.ModelSnapshotName + migration.Language;
            var modelSnapshotDirectory = GetDirectory(projectDir, modelSnapshotFileName, migration.ModelSnapshotSubnamespace);
            var modelSnapshotFile = Path.Combine(modelSnapshotDirectory, modelSnapshotFileName);

            // TODO: Test version control. If broken, determine paths in Scaffold and write files in the commands
            Directory.CreateDirectory(migrationDirectory);
            File.WriteAllText(migrationFile, migration.MigrationCode);
            File.WriteAllText(migrationMetadataFile, migration.MigrationMetadataCode);
            Directory.CreateDirectory(modelSnapshotDirectory);
            File.WriteAllText(modelSnapshotFile, migration.ModelSnapshotCode);

            return new MigrationFiles
                {
                    MigrationFile = migrationFile,
                    MigrationMetadataFile = migrationMetadataFile,
                    ModelSnapshotFile = modelSnapshotFile
                };
        }

        protected virtual string GetDirectory(
            [NotNull] string projectDir,
            [CanBeNull] string siblingFileName,
            [NotNull] string subnamespace)
        {
            Check.NotEmpty(projectDir, nameof(projectDir));
            Check.NotEmpty(subnamespace, nameof(subnamespace));

            if (siblingFileName != null)
            {
                var siblingPath = TryGetProjectFile(projectDir, siblingFileName);
                if (siblingPath != null)
                {
                    return Path.GetDirectoryName(siblingPath);
                }
            }

            return Path.Combine(projectDir, Path.Combine(subnamespace.Split('.')));
        }

        protected virtual string TryGetProjectFile([NotNull] string projectDir, [NotNull] string fileName) =>
            Directory.EnumerateFiles(projectDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }
}
