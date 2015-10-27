using System.Reflection;
using System.Windows;
using Galaxy.DatabaseService;
using Galaxy.DealManager.View;
using Galaxy.DealManager.ViewModel;
using Galaxy.MarketFeedService;
using Ninject;

namespace Galaxy.DealManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();

            IKernel kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
            IMarketFeed marketFeed = kernel.Get<IMarketFeed>();
            IDbManager dbManager = kernel.Get<IDbManager>();
            DealManagerWin dealManagerWindow = new DealManagerWin();
            DealManagerVM dealManagerVm = new DealManagerVM(marketFeed, dbManager);
            dealManagerWindow.DataContext = dealManagerVm;
            dealManagerWindow.Show();
        }

        private void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
        }
    }
}
