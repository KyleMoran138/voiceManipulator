using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voiceManipulator {

  public class Cli {

    Dictionary<String, System.Reflection.MethodInfo> commands = new Dictionary<string, System.Reflection.MethodInfo>();
    public Char beforeChar = '#';
    public enum DEBUG_LEVEL {
      none = 0,
      debug = 1,
      verbose = 3
    }
    private static DEBUG_LEVEL debug = DEBUG_LEVEL.debug;
    public String motd = "";
    public String commandNotFoundMsg = "That command was not found.";
    public Boolean isRunning = false;
    public const Int32 maxErrorLevel = 5;
    public Int32 errorLevel = 0;

    public Cli() {
    }

    public static void Error(string msg, DEBUG_LEVEL level = DEBUG_LEVEL.none) {
      output(level,"Error: " + msg);
    }

    public static void Debug(string msg, DEBUG_LEVEL level = DEBUG_LEVEL.debug) {
      output(level, "Debug: " + msg);
    }

    public static void Verbose(string msg, DEBUG_LEVEL level = DEBUG_LEVEL.verbose) {
      output(level, "Verbose: " + msg);
    }

    public static void Output(string msg, DEBUG_LEVEL level = DEBUG_LEVEL.none) {
      output(level, msg);
    }

    public static void output(DEBUG_LEVEL level, string msg, string endl="\n") {
      if(level <= debug) {
        Console.Write(msg + endl);
      }
    }
    
    public void start() {
      if(debug == DEBUG_LEVEL.none) {
        Console.Clear();
      }
      
      isRunning = true;
      Console.WriteLine(motd);
      string lastEntry = "";
      while (isRunning && errorLevel >= 0 && errorLevel <= maxErrorLevel ) {
        Console.Write(beforeChar + " ");
        lastEntry = Console.ReadLine();
        handleCommand(lastEntry);
      }

      //This will do something some day
      if(errorLevel > maxErrorLevel) {
        Error("You did it. You broke the thing that cant break. No cake for you.");
      }

      Verbose("Console ended.\nPress any key to continue...", DEBUG_LEVEL.none);
      Console.ReadKey();
      if(debug == DEBUG_LEVEL.none) {
        Console.Clear();
      }
      
      isRunning = false;
      errorLevel = 0;
    }

    // Adds a command to the CLI's dictionary
    public void addCommand(String name, System.Reflection.MethodInfo method) {
      if (method == null) {
        Error($"Failed to add command ({name}). System.Reflection.MethodInfo was null");
      }
      commands.Add(name.ToLower(), method);
    }

    // Checks if the command exists. If it does then run it and return  else = false. 
    public Boolean executeCommand(String command, String[] args) {
      System.Reflection.MethodInfo commandMethod;
      // If the command is in the command dictionary
      if (commands.ContainsKey(command.ToLower())) {
        commandMethod = commands[command.ToLower()];
        // This is a sanity check. In some unlikeley situations this value could be null
        // In that case pretend the command wasn't called and found. Debug reason
        if (commandMethod == null) {
          Debug("The command was found but returned null method info");
          return false;
        }
        commandMethod.Invoke(null, new[]{ args});
        return true;
      }
      // If the command isn't found return false and debug reason
      Debug("Command was not found in the command dictionary. ");
      return false;
    }

    // Takes raw user input and converts it into a command and args. 
    public Boolean handleCommand(String command) {
      String[] args = command.Split(' ');
      String method = args[0];

      if(!executeCommand(method, args)){
        Error(commandNotFoundMsg);
        return false;
      }

      return true;
    }

    //Prompts the user
    public static String promptUser(String prompt) {
      Output(prompt + ": ");
      return Console.ReadLine();
    }

    public static Boolean promptUserInt(String prompt, out int value, Boolean failedOnce = false){
      int returnValue;
      if(int.TryParse(promptUser(prompt), out returnValue)){
        value = returnValue;
        return true;
      }else if(!failedOnce){
        int secondaryValue;
        failedOnce = true;
        Output("Invalid int");
        value = 0;
        return promptUserInt(prompt, out secondaryValue, failedOnce);
      }else{
        value = 0;
        return false;
      }
    }
    
    //Sets the logging level for the CLI
    public static Boolean setLoggingLevel(int level){
      if(level < 0 || level > (int)DEBUG_LEVEL.verbose){
        return false;
      }
      debug = (DEBUG_LEVEL) level;
      return true;
    }

  }
}
