using System.Collections.Concurrent;
using System.Configuration;
using System.Reflection;
using System.Threading;
using Galaxy.MarketFeedService;
using Galaxy.DatabaseService;
using log4net;
using Ninject;


namespace Pipe
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string _closePriceSnap = ConfigurationManager.AppSettings["InsertClosePrice"];
        private static readonly string _newInstruSnap = ConfigurationManager.AppSettings["InsertNewInstrument"];
        private static readonly string _instruReportSnap = ConfigurationManager.AppSettings["InstrumentReport"];
        private static readonly string _dealReportSnap = ConfigurationManager.AppSettings["DealReport"];

        private static bool _up = true; 

        private static ConcurrentDictionary<string, Instrument> _instruInfoSafeDico; 

        private static bool _snapshotInProgress  = true;

        static void Main(string[] args)
        {
            IKernel kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
            IMarketFeed marketFeed = kernel.Get<IMarketFeed>();
            IDbManager dbManager = kernel.Get<IDbManager>();

            log.Info("Pipe started");

            if (args == null || args.Length != 1)
            {
                log.Error("Wrong arguments specified");
            }
            else
            {
                IProcess process;

                if (args[0] == _closePriceSnap)
                {
                    log.Info("Close price snapshot in progress...");
                    process = new CloseSnapshot(marketFeed, dbManager);
                }
                else if (args[0] == _newInstruSnap)
                {
                    log.Info("New instruments snapshot in progress...");
                   process = new NewInstruSnapshot(marketFeed, dbManager);
                }
                else if (args[0] == _instruReportSnap)
                {
                    log.Info("Instrument Reporting in progress...");
                    process = new InstrumentReport(dbManager);
                }
                else if (args[0] == _dealReportSnap)
                {
                    log.Info("Deal Reporting in progress...");
                    process = new DealReport(dbManager);
                }
                else
                {
                    log.Error($"Argument: {args[0]} doesn't exist");
                    _snapshotInProgress = false;
                    return;
                }

                process.Start();
                while (process.IsRunning)
                {
                    Thread.Sleep(1000);
                }
            }

            log.Info("Pipe closed");
        }
    }
}
