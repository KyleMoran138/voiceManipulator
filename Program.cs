using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Mixer;
using NAudio.Wave.SampleProviders;
using Google.Cloud.Speech.V1;
using System.Threading;

namespace voiceManipulator {
  public class Program {

    private const String credential_path = "./apiConfig.json";
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

      console.addCommand("filesetpath", typeof(Program).GetMethod("setFileLocation"));
      console.addCommand("getdata", typeof(Program).GetMethod("getGoogleResponse"));
      console.addCommand("setlogging", typeof(Program).GetMethod("setLoggingValue"));
      console.addCommand("playword", typeof(Program).GetMethod("playSingleWord"));
      //console.addCommand("gui", typeof(Program).GetMethod("guiFindFile"));

      console.start();

    }


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

    public static Boolean getGoogleResponse(String[] args) {
      Cli.Verbose("Getting goodle data...");
      lastResponse = SpeechClient.Recognize(new RecognitionConfig() {
        Encoding = RecognitionConfig.Types.AudioEncoding.Flac,
        SampleRateHertz = 44100,
        LanguageCode = "en",
        EnableWordTimeOffsets = true
      }, RecognitionAudio.FromFile(FilePath));
      Cli.Verbose("Data retreved...");
      Cli.Verbose(lastResponse.ToString());
      return true;
    }

    public static Boolean setLoggingValue(String[] args){
      int loggingValue;
      if(args.Length < 2){
        if(!Cli.promptUserInt("Enter logging level", out loggingValue)) return false;
      }else{
        if(!int.TryParse(args[1], out loggingValue)) return false;
      }
      if(Cli.setLoggingLevel(loggingValue)){
        Console.WriteLine($"Logging level set to {loggingValue.ToString()}");
        return true;
      }
      return false;
    }

    public static Boolean processGoogleResponse(String[] args){
      getResponseData();
      return true;
    }

    public static void playSingleWord(String[] args) {
      int wordToPlay = 0;
      var responseData = getResponseData();
      if (args.Length < 2) {
        
        if(!Cli.promptUserInt("Select # to play", out wordToPlay)) return;

      }else{
        if(!int.TryParse(args[1], out wordToPlay)) return;
      }

      if (wordToPlay - 1 >= 0 && wordToPlay - 1 <= responseData.words.Count) {
        playWord(responseData.words[wordToPlay - 1], FilePath);
      }
    }

    //Function removed to allow .net CORE
    //public static Boolean guiFindFile(String[] args) {
    //
    //  string selectedPath = "";
    //  var t = new Thread((ThreadStart)(() => {
    //    OpenFileDialog fbd = new OpenFileDialog();
    //    if (fbd.ShowDialog() == DialogResult.Cancel)
    //      return;
    //
    //    selectedPath = fbd.InitialDirectory + fbd.FileName;
    //  }));
    //
    //  t.SetApartmentState(ApartmentState.STA);
    //  t.Start();
    //  t.Join();
    //
    //  if (setFilePath(selectedPath)) {
    //      Console.WriteLine("File path set.");
    //  }
    //  else {
    //      Console.WriteLine("File path filed to set.");
    //  }
    //  return true;
    //}

    /// Data processing functions

    public static AudioDataFromGoogle getResponseData() {
      var result = lastResponse.Results[0].Alternatives[0];
      AudioDataFromGoogle returnValue = new AudioDataFromGoogle(result.Words, result.Transcript, result.Confidence);
      if(returnValue.isValid) return returnValue;
      return null;
    }

    private static Boolean setFilePath(String filePath) {
      FilePath = filePath;
      if (FilePath != null)
        return true;
      return false;
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

    public static void playWord(WordInfo word, string audioFile) {
      var file = new AudioFileReader(audioFile);
      var trimmed = new OffsetSampleProvider(file);
      
      double startTime = word.StartTime.ToTimeSpan().TotalMilliseconds;
      double stopTime = word.EndTime.ToTimeSpan().TotalMilliseconds;
      double playTime = stopTime - startTime;
      if (word.Word.Length < 5 && playTime > 500) {
        Cli.Verbose("Happened");
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


  }

  public class AudioDataFromGoogle{
    public Google.Protobuf.Collections.RepeatedField<Google.Cloud.Speech.V1.WordInfo> words;
    public string transcript;
    public float confidence;
    public Boolean isValid = false;

    public AudioDataFromGoogle(Google.Protobuf.Collections.RepeatedField<Google.Cloud.Speech.V1.WordInfo> words, string transcript, float confidence){

      if(confidence > 0.9 && transcript != "" && words.Count >= 1){
        isValid = true;
        this.words = words;
        this.transcript = transcript;
        this.confidence = confidence;
        Cli.Debug("AudioDataFromGoogle:" + this.ToString());
      }

    }

  }

}

