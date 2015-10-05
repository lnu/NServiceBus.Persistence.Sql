﻿using System.IO;
using System.Text;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;
using NServiceBus.SqlPersistence;

class SubscriptionInstaller : INeedToInstallSomething
{

    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.GetFeatureEnabled<StorageType.Subscriptions>())
        {
            return;
        }
        if (settings.GetDisableInstaller<StorageType.Subscriptions>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Subscriptions>();
        var endpointName = settings.EndpointName();
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer);
        }

        SqlHelpers.Execute(connectionString, builder.ToString(), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Subscriptions>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }
}