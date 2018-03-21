using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

public class OrderSubmittedHandler :
    IHandleMessages<OrderSubmitted>
{
    static ILog log = LogManager.GetLogger<OrderSubmittedHandler>();

    public Task Handle(OrderSubmitted message, IMessageHandlerContext context)
    {
        log.Info($"Order {message.OrderId} worth {message.Value} submitted");

        #region StoreUserData

        var order = new Order
                        {
                            OrderId = message.OrderId,
                            Value = message.Value
                        };

        if (context.SynchronizedStorageSession is NHibernateStorageSession)
        {
            var nhibernateSession = context.SynchronizedStorageSession.Session();
            
            nhibernateSession.Save(order);
        }
        

        #endregion

        #region Reply

        var orderAccepted = new OrderAccepted
        {
            OrderId = message.OrderId,
        };
        return context.Reply(orderAccepted);

        #endregion
    }
}