using System;
using System.Collections.Generic;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Fields.Converters;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace Sender
{
    public class ClientApp : MessageCracker, IApplication
    {
        private Session _session;

        // This variable is a kludge for developer test purposes.  Don't do this on a production application.
        public IInitiator MyInitiator = null;


        public void Run()
        {
            while (true)
                try
                {
                    var action = QueryAction();
                    if (action == '1')
                    {
                        QueryEnterOrder();
                    }
                    else if (action == '2')
                    {
                        QueryCancelOrder();
                    }
                    else if (action == '3')
                    {
                        QueryReplaceOrder();
                    }
                    else if (action == '4')
                    {
                        QueryMarketDataRequest();
                    }
                    else if (action == 'g')
                    {
                        if (MyInitiator.IsStopped)
                        {
                            Console.WriteLine("Restarting initiator...");
                            MyInitiator.Start();
                        }
                        else
                        {
                            Console.WriteLine("Already started.");
                        }
                    }
                    else if (action == 'x')
                    {
                        if (MyInitiator.IsStopped)
                        {
                            Console.WriteLine("Already stopped.");
                        }
                        else
                        {
                            Console.WriteLine("Stopping initiator...");
                            MyInitiator.Stop();
                        }
                    }
                    else if (action == 'q' || action == 'Q')
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Message Not Sent: " + e.Message);
                    Console.WriteLine("StackTrace: " + e.StackTrace);
                }

            Console.WriteLine("Program shutdown.");
        }

        private void SendMessage(Message m)
        {
            if (_session != null)
            {
                var isSent = _session.Send(m);
            }
            else
                // This probably won't ever happen.
                Console.WriteLine("Can't send message: session not created.");
        }

        private char QueryAction()
        {
            // Commands 'g' and 'x' are intentionally hidden.
            Console.Write("\n"
                          + "1) Enter Order\n"
                          + "2) Cancel Order\n"
                          + "3) Replace Order\n"
                          + "4) Market data test\n"
                          + "Q) Quit\n"
                          + "Action: "
            );

            var validActions = new HashSet<string>("1,2,3,4,q,Q,g,x".Split(','));

            var cmd = Console.ReadLine().Trim();
            if (cmd.Length != 1 || validActions.Contains(cmd) == false)
                throw new Exception("Invalid action");

            return cmd.ToCharArray()[0];
        }

        private void QueryEnterOrder()
        {
            Console.WriteLine("\nNewOrderSingle");

            var m = QueryNewOrderSingle44();

            if (m != null && QueryConfirm("Send order"))
            {
                m.Header.GetString(Tags.BeginString);

                SendMessage(m);
            }
        }

        private void QueryCancelOrder()
        {
            Console.WriteLine("\nOrderCancelRequest");

            var m = QueryOrderCancelRequest44();

            if (m != null && QueryConfirm("Cancel order"))
                SendMessage(m);
        }

        private void QueryReplaceOrder()
        {
            Console.WriteLine("\nCancelReplaceRequest");

            var m = QueryCancelReplaceRequest44();

            if (m != null && QueryConfirm("Send replace"))
                SendMessage(m);
        }

        private void QueryMarketDataRequest()
        {
            Console.WriteLine("\nMarketDataRequest");

            var m = QueryMarketDataRequest44();

            if (m != null && QueryConfirm("Send market data request"))
                SendMessage(m);
        }

        private bool QueryConfirm(string query)
        {
            Console.WriteLine();
            Console.WriteLine(query + "?: ");
            var line = Console.ReadLine()?.Trim();
            return line != null && (line[0].Equals('y') || line[0].Equals('Y'));
        }

        #region IApplication interface overrides

        public void OnCreate(SessionID sessionID)
        {
            _session = Session.LookupSession(sessionID);
        }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine("Logon - " + sessionID);
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine("Logout - " + sessionID);
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("IN:  " + message);
            try
            {
                Crack(message, sessionID);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==Cracker exception==");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void ToApp(Message message, SessionID sessionID)
        {
            try
            {
                var possDupFlag = false;
                if (message.Header.IsSetField(Tags.PossDupFlag))
                    possDupFlag = BoolConverter.Convert(
                        message.Header.GetString(Tags.PossDupFlag)); /// FIXME
                if (possDupFlag)
                    throw new DoNotSend();
            }
            catch (FieldNotFoundException)
            {
            }

            Console.WriteLine();
            Console.WriteLine("OUT: " + message);
        }

        #endregion


        #region MessageCracker handlers

        public void OnMessage(ExecutionReport m, SessionID s)
        {
            Console.WriteLine("Received execution report");
        }

        public void OnMessage(OrderCancelReject m, SessionID s)
        {
            Console.WriteLine("Received order cancel reject");
        }

        #endregion

        #region Message creation functions

        private NewOrderSingle QueryNewOrderSingle44()
        {
            OrdType ordType = null;

            var newOrderSingle = new NewOrderSingle(
                QueryClOrdID(),
                QuerySymbol(),
                QuerySide(),
                new TransactTime(DateTime.Now),
                ordType = QueryOrdType());

            newOrderSingle.Set(new HandlInst('1'));
            newOrderSingle.Set(QueryOrderQty());
            newOrderSingle.Set(QueryTimeInForce());
            if (ordType.getValue() == OrdType.LIMIT || ordType.getValue() == OrdType.STOP_LIMIT)
                newOrderSingle.Set(QueryPrice());
            if (ordType.getValue() == OrdType.STOP || ordType.getValue() == OrdType.STOP_LIMIT)
                newOrderSingle.Set(QueryStopPx());

            return newOrderSingle;
        }

        private OrderCancelRequest QueryOrderCancelRequest44()
        {
            var orderCancelRequest = new OrderCancelRequest(
                QueryOrigClOrdID(),
                QueryClOrdID(),
                QuerySymbol(),
                QuerySide(),
                new TransactTime(DateTime.Now));

            orderCancelRequest.Set(QueryOrderQty());
            return orderCancelRequest;
        }

        private OrderCancelReplaceRequest QueryCancelReplaceRequest44()
        {
            var ocrr = new OrderCancelReplaceRequest(
                QueryOrigClOrdID(),
                QueryClOrdID(),
                QuerySymbol(),
                QuerySide(),
                new TransactTime(DateTime.Now),
                QueryOrdType());

            ocrr.Set(new HandlInst('1'));
            if (QueryConfirm("New price"))
                ocrr.Set(QueryPrice());
            if (QueryConfirm("New quantity"))
                ocrr.Set(QueryOrderQty());

            return ocrr;
        }

        private MarketDataRequest QueryMarketDataRequest44()
        {
            var mdReqID = new MDReqID("MARKETDATAID");
            var subType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT);
            var marketDepth = new MarketDepth(0);

            var marketDataEntryGroup = new MarketDataRequest.NoMDEntryTypesGroup();
            marketDataEntryGroup.Set(new MDEntryType(MDEntryType.BID));

            var symbolGroup = new MarketDataRequest.NoRelatedSymGroup();
            symbolGroup.Set(new Symbol("LNUX"));

            var message = new MarketDataRequest(mdReqID, subType, marketDepth);
            message.AddGroup(marketDataEntryGroup);
            message.AddGroup(symbolGroup);

            return message;
        }

        #endregion

        #region field query private methods

        private ClOrdID QueryClOrdID()
        {
            Console.WriteLine();
            Console.Write("ClOrdID? ");
            return new ClOrdID(Console.ReadLine().Trim());
        }

        private OrigClOrdID QueryOrigClOrdID()
        {
            Console.WriteLine();
            Console.Write("OrigClOrdID? ");
            return new OrigClOrdID(Console.ReadLine().Trim());
        }

        private Symbol QuerySymbol()
        {
            Console.WriteLine();
            Console.Write("Symbol? ");
            return new Symbol(Console.ReadLine().Trim());
        }

        private Side QuerySide()
        {
            Console.WriteLine();
            Console.WriteLine("1) Buy");
            Console.WriteLine("2) Sell");
            Console.WriteLine("3) Sell Short");
            Console.WriteLine("4) Sell Short Exempt");
            Console.WriteLine("5) Cross");
            Console.WriteLine("6) Cross Short");
            Console.WriteLine("7) Cross Short Exempt");
            Console.Write("Side? ");
            var s = Console.ReadLine()?.Trim();

            var c = ' ';
            switch (s)
            {
                case "1":
                    c = Side.BUY;
                    break;
                case "2":
                    c = Side.SELL;
                    break;
                case "3":
                    c = Side.SELL_SHORT;
                    break;
                case "4":
                    c = Side.SELL_SHORT_EXEMPT;
                    break;
                case "5":
                    c = Side.CROSS;
                    break;
                case "6":
                    c = Side.CROSS_SHORT;
                    break;
                case "7":
                    c = 'A';
                    break;
                default: throw new Exception("unsupported input");
            }

            return new Side(c);
        }

        private OrdType QueryOrdType()
        {
            Console.WriteLine();
            Console.WriteLine("1) Market");
            Console.WriteLine("2) Limit");
            Console.WriteLine("3) Stop");
            Console.WriteLine("4) Stop Limit");
            Console.Write("OrdType? ");
            var s = Console.ReadLine().Trim();

            var c = ' ';
            switch (s)
            {
                case "1":
                    c = OrdType.MARKET;
                    break;
                case "2":
                    c = OrdType.LIMIT;
                    break;
                case "3":
                    c = OrdType.STOP;
                    break;
                case "4":
                    c = OrdType.STOP_LIMIT;
                    break;
                default: throw new Exception("unsupported input");
            }

            return new OrdType(c);
        }

        private OrderQty QueryOrderQty()
        {
            Console.WriteLine();
            Console.Write("OrderQty? ");
            return new OrderQty(Convert.ToDecimal(Console.ReadLine().Trim()));
        }

        private TimeInForce QueryTimeInForce()
        {
            Console.WriteLine();
            Console.WriteLine("1) Day");
            Console.WriteLine("2) IOC");
            Console.WriteLine("3) OPG");
            Console.WriteLine("4) GTC");
            Console.WriteLine("5) GTX");
            Console.Write("TimeInForce? ");
            var s = Console.ReadLine().Trim();

            var c = ' ';
            switch (s)
            {
                case "1":
                    c = TimeInForce.DAY;
                    break;
                case "2":
                    c = TimeInForce.IMMEDIATE_OR_CANCEL;
                    break;
                case "3":
                    c = TimeInForce.AT_THE_OPENING;
                    break;
                case "4":
                    c = TimeInForce.GOOD_TILL_CANCEL;
                    break;
                case "5":
                    c = TimeInForce.GOOD_TILL_CROSSING;
                    break;
                default: throw new Exception("unsupported input");
            }

            return new TimeInForce(c);
        }

        private Price QueryPrice()
        {
            Console.WriteLine();
            Console.Write("Price? ");
            return new Price(Convert.ToDecimal(Console.ReadLine().Trim()));
        }

        private StopPx QueryStopPx()
        {
            Console.WriteLine();
            Console.Write("StopPx? ");
            return new StopPx(Convert.ToDecimal(Console.ReadLine().Trim()));
        }

        #endregion
    }
}