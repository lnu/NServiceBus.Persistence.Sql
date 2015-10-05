﻿using System.IO;
using System.Text;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;
using NServiceBus.SqlPersistence;

class SagaInstaller : INeedToInstallSomething
{

    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.GetFeatureEnabled<StorageType.Sagas>())
        {
            return;
        }
        if (settings.GetDisableInstaller<StorageType.Sagas>())
        {
            return;
        }
        var typesToScan = config.TypesToScan;
        var sagaDefinitions = SagaMetaDataReader.GetSagaDefinitions(typesToScan);
        var connectionString = settings.GetConnectionString<StorageType.Sagas>();
        var endpointName = config.Settings.EndpointName();
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(sagaDefinitions, s => writer);
        }
        SqlHelpers.Execute(connectionString, builder.ToString(), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Sagas>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }
}