using System;
using Galaxy.DatabaseService;

namespace Galaxy.PricingService
{
    public class Pnl
    {
        public static void ComputeInstrumentPosition(InstrumentPosition pos, Deal newDeal)
        {
            // Check if instrument qty and new newDeal qty have the same sign (+ buy - sell)
            if (newDeal.Quantity * pos.Quantity > 0)
            {
                // Compute harmonic mean
                pos.AvgPrice = (pos.AvgPrice * pos.Quantity + newDeal.ExecPrice * newDeal.Quantity) / (pos.Quantity + newDeal.Quantity);
                // sum quantity
                pos.Quantity += newDeal.Quantity;
            }
            // case sign are different:
            else
            {
                //Close all the long position by short newDeal
                if (Math.Abs(newDeal.Quantity) == Math.Abs(pos.Quantity) && newDeal.Quantity < pos.Quantity)
                {
                    pos.RealisedPnl += Math.Abs(newDeal.Quantity) * (newDeal.ExecPrice - pos.AvgPrice) * pos.LotSize;
                    pos.AvgPrice = 0;
                    pos.Quantity = 0;
                }
                // Close all the short position by long newDeal
                else if (Math.Abs(newDeal.Quantity) == Math.Abs(pos.Quantity) && newDeal.Quantity > pos.Quantity)
                {
                    pos.RealisedPnl += Math.Abs(newDeal.Quantity) * (pos.AvgPrice - newDeal.ExecPrice) * pos.LotSize;
                    pos.AvgPrice = 0;
                    pos.Quantity = 0;
                }
                //Close part of the long position by short newDeal
                else if (Math.Abs(newDeal.Quantity) < Math.Abs(pos.Quantity) && newDeal.Quantity < pos.Quantity)
                {
                    pos.RealisedPnl += Math.Abs(newDeal.Quantity) * (newDeal.ExecPrice - pos.AvgPrice) * pos.LotSize;
                    pos.Quantity = pos.Quantity + newDeal.Quantity;
                }
                //Close part of the short position by long newDeal
                else if (Math.Abs(newDeal.Quantity) < Math.Abs(pos.Quantity) && newDeal.Quantity > pos.Quantity)
                {
                    pos.RealisedPnl += Math.Abs(newDeal.Quantity) * (pos.AvgPrice - newDeal.ExecPrice) * pos.LotSize;
                    pos.Quantity = pos.Quantity + newDeal.Quantity;
                }
                else
                {
                    pos.RealisedPnl += Math.Abs(pos.Quantity) * (newDeal.ExecPrice - pos.AvgPrice) * pos.LotSize;
                    pos.Quantity = pos.Quantity + newDeal.Quantity;
                    pos.AvgPrice = newDeal.ExecPrice;
                }
            }
        }

        public static void ComputeBookPosition(BookPosition pos, InstrumentPosition instruPos)
        {
            pos.RealisedPnl += instruPos.RealisedPnl;
            pos.UnrealisedPnl += instruPos.UnrealisedPnl;
            pos.FairUnrealisedPnl += instruPos.FairUnrealisedPnl;
            if (instruPos.InstruType == "OPTION")
            {
                pos.Cash += instruPos.RealisedPnl - instruPos.Value * instruPos.AvgPrice;
            }
            if (instruPos.InstruType == "FUTURE")
            {
                pos.Cash += instruPos.RealisedPnl;
            }
        }

        public static void ComputeBookRisk(BookPosition pos, InstrumentPosition instruPos)
        {
            pos.Delta += instruPos.Delta;
            pos.Gamma += instruPos.Gamma;
            pos.Vega += instruPos.Vega;
            pos.Theta += instruPos.Theta;
        }
    }
}
