using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Qualificator.Data
{
    public class CreateDbWithMigrationHistoryIfNotExists<TContext, TMigrationsConfiguration> :
        IDatabaseInitializer<TContext>
        where TContext : DbContext
        where TMigrationsConfiguration : DbMigrationsConfiguration<TContext>
    {
        private readonly Regex _pattern = new Regex("ProviderManifestToken=\"([^\"]*)\"");
        private readonly TMigrationsConfiguration _config;

        public CreateDbWithMigrationHistoryIfNotExists()
        {
            _config = Activator.CreateInstance<TMigrationsConfiguration>();
        }

        public void InitializeDatabase(TContext context)
        {
            if (context.Database.Exists()) return;
            context.Database.Create();

            var operations = GetInsertHistoryOperations();
            if (!operations.Any()) 
                return;

            var providerManifestToken = GetProviderManifestToken(operations.First().Model);
            var sqlGenerator = _config.GetSqlGenerator(GetProviderInvariantName(context.Database.Connection));
            var statements = sqlGenerator.Generate(operations, providerManifestToken);

            statements.ToList()
                .ForEach(x => context.Database.ExecuteSqlCommand(x.Sql));
        }

        private IList<InsertHistoryOperation> GetInsertHistoryOperations()
        {
            return _config.MigrationsAssembly
                .GetTypes()
                .Where(x => typeof(DbMigration).IsAssignableFrom(x))
                .Select(migration => (IMigrationMetadata)Activator.CreateInstance(migration))
                .Select(metadata => new InsertHistoryOperation(
                    "__MigrationHistory",
                    metadata.Id,
                    Convert.FromBase64String(metadata.Target)))
                .ToList();
        }

        private string GetProviderManifestToken(byte[] model)
        {
            var targetDoc = Decompress(model);
            return _pattern.Match(targetDoc.ToString()).Groups[1].Value;
        }

        private static XDocument Decompress(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    return XDocument.Load(gzipStream);
                }
            }
        }

        private static string GetProviderInvariantName(DbConnection connection)
        {
            var type = DbProviderServices.GetProviderFactory(connection).GetType();
            var assemblyName = new AssemblyName(type.Assembly.FullName);

            foreach (DataRow providerRow in (InternalDataCollectionBase)DbProviderFactories.GetFactoryClasses().Rows)
            {
                var typeName = (string)providerRow[3];
                var rowProviderFactoryAssemblyName = (AssemblyName)null;

                Type.GetType(
                    typeName, 
                    a => 
                    {
                        rowProviderFactoryAssemblyName = a;
                        return (Assembly)null;
                    }, 
                    (assembly, str, b) => (Type)null);

                if (rowProviderFactoryAssemblyName == null) 
                    continue;

                if (!string.Equals(
                    assemblyName.Name,
                    rowProviderFactoryAssemblyName.Name,
                    StringComparison.OrdinalIgnoreCase)) 
                    continue;

                if (DbProviderFactories.GetFactory(providerRow).GetType() == type)
                    return (string)providerRow[2];
            }
            throw new Exception("Couldn't get the provider invariant name");
        }
    }
}
