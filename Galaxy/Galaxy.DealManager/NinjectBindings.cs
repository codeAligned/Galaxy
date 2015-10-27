using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Galaxy.DatabaseService;
using Galaxy.MarketFeedService;
using Ninject.Modules;

namespace Galaxy.DealManager
{
    public class NinjectBindings : NinjectModule
    {
        public override void Load()
        {
            Bind<IMarketFeed>().To<TTApi>();
            Bind<IDbManager>().To<DbManager>();
        }
    }
}
