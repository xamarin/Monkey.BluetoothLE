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
using Microsoft.Devices.Sensors;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Xna.Framework;

namespace Robotics.Mobile.Robotroller.WP
{
    public partial class MainPage : PhoneApplicationPage, IGyro
    {
        Motion motion;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Xamarin.Forms.Forms.Init();

            InitGyroscope();

            var a = new Robotics.Mobile.Core.Bluetooth.LE.Adapter();

            var app = new Robotics.Mobile.Robotroller.App(a, this);

            Content = app.GetMainPage().ConvertPageToUIElement(this);
        }

        private void InitGyroscope ()
        {
            if (Motion.IsSupported == true)
            {
                Debug.WriteLine("Motion supported");
                motion = new Motion();

                motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                motion.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<MotionReading>>(motion_CurrentValueChanged);
                motion.Start(); 
            }
            else
            {
                Debug.WriteLine("Motion not supported");
            }
        }

        void motion_CurrentValueChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            //Debug.WriteLine("R: " + Roll + " P: " + Pitch + " Y:" + Yaw);

            _roll = e.SensorReading.Attitude.Roll;
            _pitch = e.SensorReading.Attitude.Pitch;
            _yaw = e.SensorReading.Attitude.Yaw;

            if(GyroUpdated != null) 
                GyroUpdated(this, new EventArgs());

        } 

        public double Roll
        {
            get { return _roll; }
            set { _roll = value; }
        }
        private double _roll = 0.0;

        public double Pitch
        {
            get { return _pitch; }
            set { _pitch = value; }
        }
        private double _pitch = 0.0;

        public double Yaw
        {
            get { return _yaw; }
            set { _yaw = value; }
        }
        private double _yaw = 0.0;

        public event EventHandler GyroUpdated;
    }
}