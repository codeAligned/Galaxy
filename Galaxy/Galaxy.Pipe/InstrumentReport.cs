using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Runtime;
using System.Text;
using Galaxy.DatabaseService;
using Galaxy.PricingService;
using log4net;

namespace Pipe
{
    public class InstrumentReport : IProcess
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IDbManager _dbManager;
        public bool IsRunning { get; set; } = true;
        private readonly string _dailyReportPath = ConfigurationManager.AppSettings["InstruReportPath"];
        private readonly string _histoBookPath = ConfigurationManager.AppSettings["HistoBookPath"];
        private readonly string _smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
        private readonly string _fromAdress = ConfigurationManager.AppSettings["FromAdress"];
        private readonly string _adressPassword = ConfigurationManager.AppSettings["AdressPassword"];
        private readonly int _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
        private readonly DateTime _previousDay;
        private readonly Dictionary<string, VolParam> _volParamDico;

        public InstrumentReport(IDbManager dbManager)
        {
            _dbManager = dbManager;
            
            _previousDay = Option.PreviousWeekDay(DateTime.Today);

            _volParamDico = new Dictionary<string, VolParam>();
            LoadVolParams();
        }

        public void Start()
        {
            // Check if closing snapshot batch as run
            if (!_dbManager.TestClosePrice(_previousDay))
            {
                log.Error("Close price missing. Check database insertion");
                return;
            }

            var instruPositionDico = new Dictionary<string, InstrumentPosition>();
          
            Dictionary<string, double> closePriceDico = _dbManager.GetAllClosePrices(_previousDay);

            LoadExpiredDeals(instruPositionDico);
            LoadCurrentDeals(instruPositionDico);

            ComputeRiskMetrics(instruPositionDico, closePriceDico);

            BookPosition[] bookPosArray = ComputeBookPosition(instruPositionDico);

            string fileName = "UnderlyingReport_" + _previousDay.ToString("yyyy-MM-dd") + ".csv";
            string reportPath = Path.Combine(_dailyReportPath, fileName);

            CreateInstruPositionFile(reportPath,instruPositionDico);
            CreateHistoBookPositionFile(bookPosArray,_histoBookPath);

            string mailBody = FormatMailBody(bookPosArray);
            string mailSubject = $"Daily Report {_previousDay.ToString("yyyy-MM-dd")}";
            string[] emails = _dbManager.GetDailyReportMailingList();

            if (emails.Any())
            {
                Email.Send(mailBody, reportPath,mailSubject, emails, _fromAdress, _adressPassword, _smtpServer, _smtpPort);
            }

            IsRunning = false;
        }

        private void LoadExpiredDeals(Dictionary<string, InstrumentPosition> posDico)
        {
            Deal[] expiredDeals = _dbManager.GetAllExpiredDeals();
            foreach (var deal in expiredDeals)
            {
                InstrumentPosition pos;
                if (!posDico.TryGetValue(deal.InstrumentId, out pos))
                {
                    posDico.Add(deal.InstrumentId, new InstrumentPosition(deal));
                    continue;
                }
                Pnl.ComputeInstrumentPosition(pos, deal);
            }
        }

        public void LoadCurrentDeals(Dictionary<string, InstrumentPosition> posDico)
        {
            Deal[] currentDeals = _dbManager.GetAllDeals();

            foreach (var deal in currentDeals)
            {
                InstrumentPosition pos;
                if (!posDico.TryGetValue(deal.InstrumentId, out pos))
                {
                    InstrumentPosition newPos = new InstrumentPosition(deal);
                    posDico.Add(deal.InstrumentId, newPos);                 
                    continue;
                }
                Pnl.ComputeInstrumentPosition(pos, deal);
            }
        }

        private void ComputeRiskMetrics(Dictionary<string, InstrumentPosition> instruDico, Dictionary<string, double> closePrice)
        {
            double basisPoint = 0.001;

            foreach (var instru in instruDico.Values)
            {
                //retreive vol param
                VolParam param;
                if (!_volParamDico.TryGetValue(instru.VolParamsId, out param))
                {
                    continue;
                }

                string key = Option.ConvertTtCodeToId(instru.FutureId, "STXE");
                double futurepx;
                if (!closePrice.TryGetValue(key, out futurepx))
                {
                    continue;
                }

                double optionPx;
                if (closePrice.TryGetValue(instru.InstruRic, out optionPx))
                {
                    instru.MtmPrice = optionPx;
                }

                double time = Option.GetTimeToExpiration(_previousDay, instru.MaturityDate);
                
                instru.ForwardPrice = Option.GetForwardClose(instru.ForwardId,instru.MaturityDate, futurepx);
                instru.ModelVol = Option.SviVolatility(instru.Strike, instru.ForwardPrice, param.A, param.B, param.Sigma,param.Rho, param.M, time);
                instru.FairPrice = Option.BlackScholes(instru.OptionType, instru.ForwardPrice, instru.Strike, time,instru.ModelVol);
                instru.Delta = instru.Quantity * Option.Delta(instru.OptionType, instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Theta = instru.Quantity * instru.LotSize * Option.Theta(instru.OptionType, instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Rho = Option.Rho(instru.OptionType, instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Vega = instru.Quantity * basisPoint * Option.Vega(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Gamma = (instru.Quantity * Math.Pow(instru.ForwardPrice,2) * Option.Gamma(instru.ForwardPrice, instru.Strike, instru.ModelVol, time)) / basisPoint;
                instru.Vanna = Option.Vanna(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Vomma = Option.Vomma(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Charm = Option.Charm(instru.OptionType, instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Veta = Option.Veta(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Color = Option.Color(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Ultima = Option.Ultima(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
                instru.Speed = Option.Speed(instru.ForwardPrice, instru.Strike, instru.ModelVol, time);
            }
        }

        private BookPosition[] ComputeBookPosition(Dictionary<string, InstrumentPosition> instruPosDico)
        {
            var bookPositionDico = new Dictionary<string, BookPosition>();

            foreach (InstrumentPosition instruPos in instruPosDico.Values)
            {
                BookPosition pos;
                if (!bookPositionDico.TryGetValue(instruPos.Book, out pos))
                {
                    var newPos = new BookPosition(instruPos);
                    bookPositionDico.Add(instruPos.Book, newPos);
                    continue;
                }
                Pnl.ComputeBookPosition(pos, instruPos);
            }

            return bookPositionDico.Values.ToArray();
        }

        private string FormatMailBody(BookPosition[] bookPosDico)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var bookPos in bookPosDico)
            {
                sb.AppendLine($"Book: {bookPos.Book}");
                sb.AppendLine($"Realised Pnl: ${bookPos.RealisedPnl.ToString("N")} Unrealized Pnl: ${bookPos.UnrealisedPnl.ToString("N")} Cash: ${bookPos.Cash.ToString("N")}");
            }
            return sb.ToString();
        }

        private void CreateInstruPositionFile(string filePath, Dictionary<string, InstrumentPosition> posDico)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("InstruRic,Option Type,Strike,Maturity,Quantity,LotSize,AvgPrice,ClosePrice,Realised P&L,Unrealized P&L,ForwardPrice,Model Vol,Delta,Theta,Rho,Vega,Gamma,Vanna,Vomma,Charm,Veta,Color,Ultima,Speed");
                foreach (var pos in posDico.Values)
                {
                    writer.WriteLine($"{pos.InstruRic},{pos.OptionType},{pos.Strike},{pos.MaturityDate.ToString("d")},{pos.Quantity},{pos.LotSize},{pos.AvgPrice},{pos.MtmPrice},{pos.RealisedPnl},{pos.UnrealisedPnl},{pos.ForwardPrice},{pos.ModelVol},{pos.Delta},{pos.Theta},{pos.Rho},{pos.Vega},{pos.Gamma},{pos.Vanna},{pos.Vomma},{pos.Charm},{pos.Veta},{pos.Color},{pos.Ultima},{pos.Speed}");
                }
            }
        }

        private void CreateHistoBookPositionFile(BookPosition[] bookPosition, string folderPath)
        {
            foreach (var bookPos in bookPosition)
            {
                string filePath = Path.Combine(folderPath, bookPos.Book + "_Historical.csv");

                if (!File.Exists(filePath))
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine("Date,Realised P&L,Unrealized P&L,Fair Unrlzd P&L,Rlzd+Unrlzd P&L,Cash");
                    }
                }

                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"{_previousDay.ToString("yyyy-MM-dd")},{bookPos.RealisedPnl},{bookPos.UnrealisedPnl},{bookPos.FairUnrealisedPnl},{bookPos.UnrealisedPnl+ bookPos.RealisedPnl},{bookPos.Cash}");
                }
            }
        }


        private void LoadVolParams()
        {
            VolParam[] volParams = _dbManager.GetAllVolParams(_previousDay);

            _volParamDico.Clear();
            foreach (var param in volParams)
            {
                string key = param.ProductId + "_" + param.MaturityDate.ToString("MMyyyy");

                if (!_volParamDico.ContainsKey(key))
                {
                    _volParamDico.Add(key, param);
                }
            }
        }
    }
}
