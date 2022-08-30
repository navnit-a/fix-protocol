using System;
using QuickFix;
using QuickFix.Transport;

namespace Sender
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var settings = new SessionSettings("config\\sample_initiator.cfg");
                var application = new ClientApp();
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                ILogFactory logFactory = new ScreenLogFactory(settings);
                var initiator = new SocketInitiator(application, storeFactory, settings, logFactory);

                // this is a developer-test kludge.  do not emulate.
                application.MyInitiator = initiator;

                initiator.Start();
                application.Run();
                initiator.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            ////FIX.4.1:ARCA->TW
            //var order = new QuickFix.FIX44.NewOrderSingle(
            //    new ClOrdID("1234"),
            //    new Symbol("AAPL"),
            //    new Side(Side.BUY),
            //    new TransactTime(DateTime.Now),
            //    new OrdType(OrdType.LIMIT));

            //order.Price = new Price(new decimal(22.4));
            //order.Account = new Account("18861112");

            //var sessionID = new SessionID("FIX.4.1", "ARCA", "TW");

            //try
            //{
            //    bool sendToTarget = Session.SendToTarget(order, sessionID);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    throw;
            //}
        }
    }
}