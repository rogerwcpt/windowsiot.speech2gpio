using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Media.SpeechRecognition;
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

            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>() { "Turn on red light" }, TagOnRedLight));
            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>() { "Turn on green light" }, TagOnGreenLight));
            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>() { "Turn off red light" }, TagOffRedLight));
            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>() { "Turn off green light" }, TagOffGreenLight));
            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>() { "Turn on both lights" }, TagOnBothLights));
            _recognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string>() { "Turn off both lights" }, TagOffBothLights));

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

            pin.Write(value);
        }

        private void RecognizerResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            Debug.WriteLine("Speech recognition status: " + args.Result.Status);

            if (args.Result.Status != SpeechRecognitionResultStatus.Success)
            {
                return;
            }

            Debug.WriteLine("Speech Recognised: " + args.Result.Text + " (Confidence: " + args.Result.Confidence + ")");

            if (args.Result.Confidence == SpeechRecognitionConfidence.Low) return;

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
