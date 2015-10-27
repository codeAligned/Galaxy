using System.Reflection;
using System.Windows;
using Galaxy.DatabaseService;
using Galaxy.MarketFeedService;
using Galaxy.VolManager.ViewModel;
using Ninject;

namespace Galaxy.VolManager
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
            var volManagerWindow = new VolManagerWin();
            var  volmanagerVm = new VolManagerVM(marketFeed, dbManager);
            volManagerWindow.DataContext = volmanagerVm;
            volManagerWindow.Show();
        }

        private void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
        }
    }
}
