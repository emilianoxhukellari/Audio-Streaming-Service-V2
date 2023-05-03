using Client_Application.Client.Core;
using System.Windows;

namespace Client_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        public Controller _controller = new Controller();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _controller.Run();
        }
    }
}
