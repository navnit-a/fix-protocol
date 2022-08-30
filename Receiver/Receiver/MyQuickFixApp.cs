using System;
using QuickFix;
using QuickFix.Fields;

namespace Receiver
{
    public class MyQuickFixApp : MessageCracker, IApplication
    {
        public void ToAdmin(Message message, SessionID sessionID)
        {
            // ToAdmin - all outbound admin level messages pass through this callback.
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            // FromAdmin - every inbound admin level message will pass through this method,
            // such as heartbeats, logons, and logouts.
        }

        public void ToApp(Message message, SessionID sessionID)
        {
            // ToApp - all outbound application level messages pass through this
            // callback before they are sent. If a tag needs to be added to every outgoing message,
            // this is a good place to do that.
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            //FromApp - every inbound application level message will pass through this method,
            //such as orders, executions, security definitions, and market data.
            Crack(message, sessionID);
        }

        public void OnCreate(SessionID sessionID)
        {
            Console.Write(sessionID);
            // OnCreate - this method is called whenever a new session is created.
        }

        public void OnLogout(SessionID sessionID)
        {
            // OnLogout - notifies when a session is offline - either from an exchange of logout messages
            // or network connectivity loss.
        }

        public void OnLogon(SessionID sessionID)
        {
            // OnLogon - notifies when a successful logon has completed.
        }

        public void OnMessage(QuickFix.FIX44.NewOrderSingle ord, SessionID sessionID)
        {
            ProcessOrder(ord.Price, ord.OrderQty, ord.Account);
        }

        protected void ProcessOrder(Price price, OrderQty quantity, Account account)
        {
            //...
        }
    }
}
