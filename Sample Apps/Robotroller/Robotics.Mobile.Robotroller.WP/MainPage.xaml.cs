using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Robotics.Mobile.Robotroller.WP.Resources;
using Robotics.Mobile.Core.Bluetooth.LE;
using Xamarin.Forms;

namespace Robotics.Mobile.Robotroller.WP
{
    public partial class MainPage : PhoneApplicationPage, IGyro
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Xamarin.Forms.Forms.Init();

            var a = new Robotics.Mobile.Core.Bluetooth.LE.Adapter();

            var app = new Robotics.Mobile.Robotroller.App(a, this);

            Content = app.GetMainPage().ConvertPageToUIElement(this);
        }

        public double Roll
        {
            get { throw new NotImplementedException(); }
        }

        public double Pitch
        {
            get { throw new NotImplementedException(); }
        }

        public double Yaw
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler GyroUpdated;
    }
}