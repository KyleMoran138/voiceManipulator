using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Mixer;
using NAudio.Wave.SampleProviders;
using Google.Cloud.Speech.V1;
using System.Windows.Forms;
using System.Threading;

namespace voiceManipulator {
    public class Program {

        private const String credential_path = "C:\\Users\\kylem\\workspace\\voiceManipulator\\Speech synthisizer-b601461c21c5.json";
        private static String fp;
        private static String FilePath{
            get {
                return fp;
            }
            set{
                if (System.IO.File.Exists(value)) {
                    fp = value;
                }
                else {
                    fp = null;
                }
            }
        }
        private static SpeechClient SpeechClient = null;
        private static Google.Cloud.Speech.V1.RecognizeResponse lastResponse;

        /**
         * To use the application there must be a file path set.
         *
        */

        public static void Main(String[] args) {
            Cli console = new Cli();
            Boolean speechClientInitialized = initializeSpeechClient();

            if (speechClientInitialized) {
                Console.WriteLine("Speach client initalized");
            }
            else {
                Console.WriteLine("Failed to initalize speech client... [EXIT]");
                // Call system exit
            }

            console.addCommand("filesetlocation", typeof(Program).GetMethod("setFileLocation"));
            console.addCommand("gui", typeof(Program).GetMethod("guiFindFile"));
            console.addCommand("getdata", typeof(Program).GetMethod("getGoogleResponse"));

            console.start();

        }

        public Program() {
            
        }

        /*private void playSingleWord(String[] args) {
            Int32 wordToPlay = -1;
            if (args.Length < 2) {
                String response = Cli.promptUser("Enter word # to play");

                try {
                    wordToPlay = Int32.Parse(response.Split(' ')[0]);
                }catch(Exception exc) {
                    Console.WriteLine("User input bad!\n"+exc.Message);
                }

                var responseData = getResponseData();

                if (wordToPlay - 1 >= 0 && wordToPlay - 1 <= responseData.words.Count) {
                    playWord(responseData.words[wordToPlay - 1], filePath);
                }

            }
        }*/

        // Command functions

        public static Boolean setFileLocation(String[] args) {
            Boolean callResult = false;
            if(args.Length < 2) {
                String fileLocation = Cli.promptUser("Enter file location");
                callResult = setFilePath(fileLocation);
            }
            else {
                callResult = setFilePath(args[1]);
            }
            if (callResult) {
                Console.WriteLine("File path set");
                getGoogleResponse(null);
                return true;
            }
            Console.WriteLine("Failed to set path!");
            return false;
        }

        public static Boolean guiFindFile(String[] args) {

            string selectedPath = "";
            var t = new Thread((ThreadStart)(() => {
                OpenFileDialog fbd = new OpenFileDialog();
                if (fbd.ShowDialog() == DialogResult.Cancel)
                    return;

                selectedPath = fbd.InitialDirectory + fbd.FileName;
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (setFilePath(selectedPath)) {
                Console.WriteLine("File path set.");
            }
            else {
                Console.WriteLine("File path filed to set.");
            }
            return true;
        }

        // Data processing functions
        public static Boolean getGoogleResponse(String[] args) {
            Console.WriteLine("Getting goodle data...");
            lastResponse = SpeechClient.Recognize(new RecognitionConfig() {
                Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
                SampleRateHertz = 44100,
                LanguageCode = "en",
                EnableWordTimeOffsets = true
            }, RecognitionAudio.FromFile(FilePath));
            Console.WriteLine("Data retreved.");
            return true;
        }

        private static Boolean setFilePath(String filePath) {
            FilePath = filePath;
            if (FilePath != null)
                return true;
            return false;
        }

        public static Object getResponseData() {
            var result = lastResponse.Results[0].Alternatives[0];
            var returnValue = new {transcript = result.Transcript, confidence = result.Confidence, words = result.Words};
            if(returnValue.confidence > 0.9 && returnValue.transcript != "" && returnValue.words.Count >= 1)
                return returnValue;
            return null;
        }

        public static void playWord(WordInfo word, string audioFile) {
            var file = new AudioFileReader(audioFile);
            var trimmed = new OffsetSampleProvider(file);
            
            double startTime = word.StartTime.ToTimeSpan().TotalMilliseconds;
            double stopTime = word.EndTime.ToTimeSpan().TotalMilliseconds;
            double playTime = stopTime - startTime;
            if (word.Word.Length < 5 && playTime > 500) {
                Console.WriteLine("Happened");
                stopTime += 30;
                startTime += 600;
                playTime = stopTime - startTime;
            }
            trimmed.SkipOver = TimeSpan.FromMilliseconds(startTime);
            trimmed.Take = TimeSpan.FromMilliseconds(playTime);
            
            var player = new WaveOutEvent();
            player.Init(trimmed);
            player.Play();
        }

        private static Boolean initializeSpeechClient() {
            if (System.Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") != credential_path) {
                //This needs to throw something later. But For now we'll screw the user. 
                //TODO Implement SecurityException
                System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credential_path);
            }
            if (SpeechClient == null) {
                SpeechClient = SpeechClient.Create();
            }
            return true;
        }

    }
}

