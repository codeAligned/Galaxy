using Galaxy.DatabaseService;
using Galaxy.MarketFeedService;
using Ninject.Modules;

namespace Pipe
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
