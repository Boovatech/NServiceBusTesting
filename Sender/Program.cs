using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport.SQLServer;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;

using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Legacy;

class Program
{
    static Random random;

    static async Task Main()
    {
        random = new Random();
        Console.Title = "Samples.SqlNHibernate.Sender";
        var connection = @"Data Source=DESKTOP-PQG6KUL/xe;User Id=test;Password=kassa;Max Pool Size=100";
        var endpointConfiguration = new EndpointConfiguration("Sender");
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.EnableInstallers();

        var hibernateConfig = new Configuration();
        hibernateConfig.DataBaseIntegration(x =>
        {
            x.ConnectionString = connection;
            x.Dialect<Oracle10gDialect>();
            x.Driver<OracleManagedDataClientDriver>();
        });
        hibernateConfig.SetProperty("default_schema", "test");

        #region SenderConfiguration

        var transport = endpointConfiguration.UseTransport<MsmqTransport>();
        //transport.ConnectionString(connection);
        //transport.DefaultSchema("sender");
        //transport.UseSchemaForQueue("error", "dbo");
        //transport.UseSchemaForQueue("audit", "dbo");

        var persistence = endpointConfiguration.UsePersistence<InMemoryPersistence>();

        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Sagas>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();

        //persistence.UseConfiguration(hibernateConfig);

        #endregion

        var routing = transport.Routing();
        routing.RouteToEndpoint(typeof(OrderAccepted), "Sender");


        //SqlHelper.CreateSchema(connection, "sender");

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press enter to send a message");
        Console.WriteLine("Press any key to exit");

        while (true)
        {
            var key = Console.ReadKey();
            Console.WriteLine();

            if (key.Key != ConsoleKey.Enter)
            {
                break;
            }

            var orderSubmitted = new OrderSubmitted
            {
                OrderId = Guid.NewGuid(),
                Value = random.Next(100)
            };
            await endpointInstance.Publish(orderSubmitted)
                .ConfigureAwait(false);
            Console.WriteLine("Published OrderSubmitted message");
        }
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}