using System;
using Acceptor;
using QuickFix;

namespace Receiver
{
    internal class Program
    {
        private const string HttpServerPrefix = "http://127.0.0.1:5080/";

        [STAThread]
        private static void Main(string[] args)
        {
            // Initiator is the FIX term for client -
            // we use an Initiator when we are connecting to another party.

            // Acceptor is the FIX term for server -
            // we use an Acceptor when other parties are connecting to us.

            var settings = new SessionSettings("config\\sample_acceptor.cfg");
            IApplication myApp = new MyQuickFixApp();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);
            var srv = new HttpServer(HttpServerPrefix, settings);
            
            // Acceptor - switch to SocketInitiator for Initiator
            var acceptor = new ThreadedSocketAcceptor(
                myApp,
                storeFactory,
                settings,
                logFactory);

            acceptor.Start();
            srv.Start();

            Console.WriteLine("View Executor status: " + HttpServerPrefix);
            Console.WriteLine("press <enter> to quit");
            Console.Read();

            srv.Stop();
            acceptor.Stop();
        }
    }
}