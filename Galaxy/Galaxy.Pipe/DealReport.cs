using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Galaxy.DatabaseService;
using Galaxy.PricingService;
using log4net;

namespace Pipe
{
    class DealReport : IProcess
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IDbManager _dbManager;
        public bool IsRunning { get; set; } = true;
        private readonly string _dealReportPath = ConfigurationManager.AppSettings["DealReportPath"];
        private readonly string _smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
        private readonly string _fromAdress = ConfigurationManager.AppSettings["FromAdress"];
        private readonly string _adressPassword = ConfigurationManager.AppSettings["AdressPassword"];
        private readonly int _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
        private readonly DateTime _previousDay;

        public DealReport(IDbManager dbManager)
        {
            _dbManager = dbManager;
            _previousDay = Option.PreviousWeekDay(DateTime.Today);
        }

        public void Start()
        {
            // Check if closing snapshot batch as run
            if (!_dbManager.TestClosePrice(_previousDay))
            {
                log.Error("Close price missing. Check database insertion");
                IsRunning = false;
                return;
            }

            Deal[] currentDeals = _dbManager.GetIntradayDeals(DateTime.Today);

            string fileName = "ByDayReport_" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv";
            string reportPath = Path.Combine(_dealReportPath, fileName);

            if (currentDeals.Length != 0)
            {
                CreateInstruPositionFile(reportPath, currentDeals);
            }

            string mailSubject = $"ByDayReport {DateTime.Today.ToString("dd-MM-yyyy")}";
            string[] emails = _dbManager.GetDailyReportMailingList();

            if (emails.Any())
            {
                Email.Send("", reportPath, mailSubject, emails, _fromAdress, _adressPassword, _smtpServer, _smtpPort);
            }

            IsRunning = false;
        }

        private void CreateInstruPositionFile(string folderPath, Deal[] dealPosition)
        {
            using (StreamWriter writer = new StreamWriter(folderPath))
            {
                writer.WriteLine("DealID,TraderID,Quantity,ExecPrice,BookId,TradeDate,Status,InstrumentID,TransactionFee,ClearingFee,Broker,CounterParty,Comment,ForwardLevel,VolatilityLevel");
                foreach (var deal in dealPosition)
                {
                    writer.WriteLine($"{deal.DealId},{deal.TraderId},{deal.Quantity},{deal.ExecPrice},{deal.BookId},{deal.TradeDate.ToString("G")},{deal.Status},{deal.InstrumentId},{deal.TransactionFee},{deal.ClearingFee},{deal.Broker},{deal.Counterparty},{deal.Comment},{deal.ForwardLevel},{deal.VolatilityLevel}");
                }
            }
        }
    }
}
