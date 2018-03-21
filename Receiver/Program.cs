using System;
using System.IO;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Legacy;
using NServiceBus.Transport.SQLServer;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.SqlNHibernate.Receiver";
        var connection = @"Data Source=DESKTOP-PQG6KUL/xe;User Id=test;Password=kassa;Max Pool Size=100";
        //var hibernateConfig = new Configuration();
        //hibernateConfig.DataBaseIntegration(x =>
        //{
        //    x.ConnectionString = connection;
        //    x.Dialect<Oracle10gDialect>();
        //    x.Driver<OracleManagedDataClientDriver>();
        //});

        //#region NHibernate

        //hibernateConfig.SetProperty("default_schema", "test");

        //#endregion

        ////SqlHelper.CreateSchema(connection, "receiver");
        //var mapper = new ModelMapper();
        //mapper.AddMapping<OrderMap>();
        //hibernateConfig.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

        //new SchemaExport(hibernateConfig).Execute(false, true, false);

        #region ReceiverConfiguration

        var endpointConfiguration = new EndpointConfiguration("Receiver");
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.AuditProcessedMessagesTo("audit");
        endpointConfiguration.EnableInstallers();
        var transport = endpointConfiguration.UseTransport<MsmqTransport>();
        //transport.ConnectionString(connection);
        //transport.DefaultSchema("receiver");
        //transport.UseSchemaForQueue("error", "dbo");
        //transport.UseSchemaForQueue("audit", "dbo");
        //transport.UseSchemaForQueue("Samples.SqlNHibernate.Sender", "sender");

        transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);

        var routing = transport.Routing();
        routing.RouteToEndpoint(typeof(OrderAccepted), "Sender");
        routing.RegisterPublisher(typeof(OrderSubmitted).Assembly, "Sender");

        var persistence = endpointConfiguration.UsePersistence<InMemoryPersistence>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Sagas>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
        //persistence.UseConfiguration(hibernateConfig);

        #endregion

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}