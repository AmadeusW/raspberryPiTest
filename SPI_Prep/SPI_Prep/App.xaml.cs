using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Spi;
using System.Threading.Tasks;

namespace SPI_Prep
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Write and generate
            InitSpi();
            InitTimer();

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        SpiDevice ADC;
        byte[] range1Query = new byte[3] { 0x01, 0x80, 0x00 };  // if we want to receive 3 bytes, we need to send 3 bytes
        byte[] range2Query = new byte[3] { 0x01, 0x90, 0x00 };  // if we want to receive 3 bytes, we need to send 3 bytes
        byte[] responseBuffer = new byte[3];                    // 2 bytes per measurement + 1 padding

        private void InitTimer()
        {
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            /*

            ADC.TransferFullDuplex(range1Query, responseBuffer);
            System.Diagnostics.Debug.WriteLine($"{responseBuffer[0]}, {responseBuffer[1]}, {responseBuffer[2]}");

            ADC.TransferFullDuplex(range2Query, responseBuffer);
            System.Diagnostics.Debug.WriteLine($"{responseBuffer[0]}, {responseBuffer[1]}, {responseBuffer[2]}");

            */
            ADC.TransferFullDuplex(range1Query, responseBuffer);
            //System.Diagnostics.Debug.WriteLine($"{responseBuffer[0]}, {responseBuffer[1]}, {responseBuffer[2]}");
            var result1 = ConvertToInt(responseBuffer);

            ADC.TransferFullDuplex(range2Query, responseBuffer);
            var result2 = ConvertToInt(responseBuffer);
            //System.Diagnostics.Debug.WriteLine($"{responseBuffer[0]}, {responseBuffer[1]}, {responseBuffer[2]}");

            //System.Diagnostics.Debug.WriteLine($"{result1}, {result2}");

            
            
            System.Diagnostics.Debug.WriteLine($"{transmissionId++}, {result1}, {result2}");
            
        }
        int transmissionId = 0;

        public static int ConvertToInt(byte[] data)
        {
            int result = 0;
            result = data[1] & 0x03;
            result <<= 8;
            result += data[2];
            return result;
        }

        private async Task InitSpi()
        {
            try
            {

                // 0 is the chip select line
                // Datasheet says max clock frequency is 3.6 MHz. We don't want to go any higher
                var settings = new SpiConnectionSettings(0)
                {
                    ClockFrequency = 500 * 1000,
                    Mode = SpiMode.Mode0,
                };
                // There are other things like Mode, we will try to set it if things dont work

                string spiAqs = SpiDevice.GetDeviceSelector("SPI0");                       /* Find the selector string for the SPI bus controller          */
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);            /* Find the SPI bus controller device with our selector string  */
                ADC = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings);  /* Create an SpiDevice with our bus controller and SPI settings */
                System.Diagnostics.Debug.WriteLine("InitSpi successful");

            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitSpi threw " + ex);
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
