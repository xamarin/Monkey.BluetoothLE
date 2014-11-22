using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Robotics.Mobile.BtLEExplorer.WP.Resources;
using Xamarin.Forms;

namespace Robotics.Mobile.BtLEExplorer.WP
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Xamarin.Forms.Forms.Init();

            var a = new Robotics.Mobile.Core.Bluetooth.LE.Adapter();
            Robotics.Mobile.BtLEExplorer.App.SetAdapter(a);

            Content = Robotics.Mobile.BtLEExplorer.App.GetMainPage().ConvertPageToUIElement(this);

        }
    }
}