using BLEExplorer.Pages;
using Robotics.Mobile.Core.Bluetooth.LE;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace BLEExplorer
{
    public partial class App : Application
    {
        static IAdapter Adapter;

        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new DeviceList(Adapter));
        }

        public static void SetAdapter(IAdapter adapter)
        {
            Adapter = adapter;
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}