using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Robotics.Micro.Core.WindowsIoTTests
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static Test LastTest = null; // keep in memory

        public MainPage()
        {
            this.InitializeComponent();
            testList.ItemsSource = new Test[]
            {
                new TestButtonDirectToLed (),
                new TestPushButton (),
            };
        }

        private void testList_ItemClick(object sender, ItemClickEventArgs e)
        {
            
        }

        private void testList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            var t = e.AddedItems.OfType<Test>().FirstOrDefault();
            if (t != null && t != LastTest)
            {
                if (LastTest != null)
                {
                    LastTest.Stop();
                }

                Task.Run(() => { t.Start(); });
                LastTest = t;
                title.Text = "Running " + t.Title;
            }
        }
    }
}
