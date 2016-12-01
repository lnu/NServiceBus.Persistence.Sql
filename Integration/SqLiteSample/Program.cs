﻿using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;

class Program
{
    static void Main()
    {
        Start().GetAwaiter().GetResult();
    }

    static async Task Start()
    {
        SQLiteConnection.CreateFile("MyDatabase.sqlite");
        var endpointConfiguration = new EndpointConfiguration("SqlPersistence.Sample.SqlLiteSaga");
        endpointConfiguration.UseSerialization<JsonSerializer>();
        endpointConfiguration.EnableInstallers();


        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(async () =>
        {
            var connection = new SQLiteConnection("Data Source=MyDatabase.sqlite");
            await connection.OpenAsync();
            return connection;
        });

        var endpoint = await Endpoint.Start(endpointConfiguration);
        Console.WriteLine("Press 'Enter' to start a saga");
        Console.WriteLine("Press any other key to exit");
        try
        {
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key != ConsoleKey.Enter)
                {
                    return;
                }
                var startSagaMessage = new StartSagaMessage
                {
                    MySagaId = Guid.NewGuid()
                };
                await endpoint.SendLocal(startSagaMessage);
            }
        }
        finally
        {
            await endpoint.Stop();
        }

    }
}