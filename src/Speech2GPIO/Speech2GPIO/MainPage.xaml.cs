using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Gpio;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Speech2GPIO
{
    public sealed partial class MainPage : Page
    {
        // PIN Numbers
        private const int RedLedPin = 5;
        private const int GreenLedPin = 27;

        // Voice Command Tags
        public const string TagOnRedLight = "RED_ON";
        public const string TagOnGreenLight = "GREEN_ON";
        public const string TagOffRedLight = "RED_OFF";
        public const string TagOffGreenLight = "GREEN_OFF";
        public const string TagOnBothLights = "BOTH_ON";
        public const string TagOffBothLights = "BOTH_OFF";

        private GpioController _gpio;
        private SpeechRecognizer _recognizer;

        private static GpioPin _redPin;
        private static GpioPin _greenPin;

        private static readonly object LedLock = new object();

        public MainPage()
        {
            this.InitializeComponent();

            Unloaded += MainPage_Unloaded;

            InitializeGpio();
            InitializeSpeechRecognizer();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _redPin.Dispose();
            _greenPin.Dispose();
            _recognizer.Dispose();

            _gpio = null;
            _redPin = null;
            _greenPin = null;
            _recognizer = null;
        }

        private async void InitializeSpeechRecognizer()
        {
            _recognizer = new SpeechRecognizer();

            _recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;

            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>()
            {
                "Turn on red light", "Turn on the red light", "Turn red light on", "Turn the red light on",
                "Switch on red light", "Switch on the red light", "Switch red light on", "Switch the red light on"

            }, TagOnRedLight));
            
            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>()
            {
                "Turn on green light", "Turn on the green light", "Turn green light on", "Turn the green light on",
                "Switch on green light", "Switch on the green light", "Switch green light on", "Switch the green light on"
            }, TagOnGreenLight));

            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>()
            {
                "Turn off red light", "Turn off the red light", "Turn red light off", "Turn the red light off",
                "Switch off red light", "Switch off the red light", "Switch red light off", "Switch the red light off"
            }, TagOffRedLight));

            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>()
            {
                "Turn off green light", "Turn off the green light", "Turn green light off", "Turn the green light off",
                "Switch off green light", "Switch off the green light", "Switch green light off", "Switch the green light off"
            }, TagOffGreenLight));

            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>()
            {
                "Turn on both lights", "Turn on both the lights", "Turn both lights on", "Turn both the lights on"
            }, TagOnBothLights));

            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>()
            {
               "Turn off both lights", "Turn off both the lights", "Turn both lights off", "Turn both the lights off"
            }, TagOffBothLights));

            // Compile grammer
            var compilationResult = await _recognizer.CompileConstraintsAsync();

            Debug.WriteLine("Status: " + compilationResult.Status);

            // If successful, display the recognition result.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                await _recognizer.ContinuousRecognitionSession.StartAsync();
            }
        }
 

        private void InitializeGpio()
        {
            // Initialize GPIO controller
            _gpio = GpioController.GetDefault();

            // // Initialize GPIO Pins
            _redPin = _gpio.OpenPin(RedLedPin);
            _greenPin = _gpio.OpenPin(GreenLedPin);

            _redPin.SetDriveMode(GpioPinDriveMode.Output);
            _greenPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Toggle the Red LED AND GREEN LED to say "Hello"
            WriteGpioPin(_redPin, true);
            WriteGpioPin(_greenPin, true);
            await Task.Delay(1000);
            WriteGpioPin(_redPin, false);
            WriteGpioPin(_greenPin, false);
        }

        private static void WriteGpioPin(GpioPin pin, bool isOn)
        {
            var value = isOn ? GpioPinValue.High : GpioPinValue.Low;

            lock (LedLock)
            {
                try
                {
                    pin.Write(value);
                }
                catch
                {
                   
                }
            }
        }

        private void RecognizerResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result == null)
            {
                return;
            }

            Debug.WriteLine("Speech recognition status: " + args.Result.Status);

            if (args.Result.Status != SpeechRecognitionResultStatus.Success)
            {
                return;
            }

            Debug.WriteLine("Speech Recognised: " + args.Result.Text + " (Confidence: " + args.Result.Confidence + ")");

            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                ConfidenceText.Text = args.Result.Confidence.ToString();
                OutputText.Text = args.Result.Text;
            });
            

            if (args.Result.Confidence == SpeechRecognitionConfidence.Low) return;

            if (args.Result.Constraint == null)
            {
                return;
            }

            switch (args.Result.Constraint.Tag)
            {
                case TagOnRedLight: WriteGpioPin(_redPin, true); break;
                case TagOnGreenLight: WriteGpioPin(_greenPin, true); break;
                case TagOffRedLight: WriteGpioPin(_redPin, false); break;
                case TagOffGreenLight: WriteGpioPin(_greenPin, false); break;
                case TagOnBothLights:
                    WriteGpioPin(_greenPin, true); 
                    WriteGpioPin(_redPin, true); break;
                case TagOffBothLights:
                    WriteGpioPin(_greenPin, false); 
                    WriteGpioPin(_redPin, false); break;

            }
        }
    }
}
