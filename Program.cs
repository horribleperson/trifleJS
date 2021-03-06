﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using TrifleJS.Properties;

namespace TrifleJS
{
    class Program
    {
        public static API.Context context;
        public static string[] args;
        public static bool verbose = false;

        /// <summary>
        /// Help message
        /// </summary>
        static void Help()
        {
            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine("TrifleJS.exe");
            Console.WriteLine("==========================");
            Console.WriteLine("http://triflejs.org/");
            Console.WriteLine();
            Console.WriteLine("A headless Internet Explorer emulator with JavaScript API running on V8 engine.");
            Console.WriteLine();
            Console.WriteLine("(c) Steven de Salas 2013 - MIT Licence");
            Console.WriteLine();
            Console.WriteLine("Usage: triflejs.exe [options] somescript.js [argument [argument [...]]]");
            Console.WriteLine();
            Console.WriteLine("Options: ");
            Console.WriteLine("  --debug                   Prints additional warning and debug messages.");
            Console.WriteLine("  --render:<url>            Opens a url, renders into a file and quits.");
            Console.WriteLine("  --emulate:<version>       Emulates an earlier version of IE (IE7, IE8, IE9 etc).");
            Console.WriteLine();
            Console.WriteLine("  -h, --help                Show this message and quits");
            Console.WriteLine("  -t, --test                Runs a System Test and quits");
            Console.WriteLine("  -v, --version             Prints out TrifleJS version and quits");
            Console.WriteLine();
            Console.WriteLine("Without arguments, TrifleJS will launch in interactive mode (REPL)");
            Console.WriteLine();
        }

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            // Define environment
            bool isExecuted = false;
            bool isVersionSet = false;
            List<string> configLoop = new List<string>(args);
            List<string> commandLoop = new List<string>();
            API.Phantom.OutputEncoding = "UTF-8";
            API.Phantom.CookiesEnabled = true;
            Program.args = args;
#if DEBUG
            Program.verbose = true;
#endif
            
            // No arguments? Run in interactive mode.
            if (args.Length < 1) {
                Interactive();
                return;
            }

            // Config Loop (Set IE version etc)
            foreach (string arg in configLoop)
            {
                string[] parts = arg.Split(':');
                switch (parts[0])
                {
                    case "-?":
                    case "/?":
                    case "-h":
                    case "--help":
                        Help();
                        return;
                    case "--debug":
                        Program.verbose = true;
                        break;
                    case "-v":
                    case "--version":
                        var v = API.Phantom.Version;
                        Console.WriteLine("{0}.{1}.{2}", v["major"], v["minor"], v["patch"]);
                        return;
                    case "--emulate":
                        string version = arg.Replace("--emulate:", "");
                        try
                        {
                            Browser.Emulate(version.ToUpper());              
                            isVersionSet = true;
                        }
                        catch {
                            Console.Error.WriteLine(String.Format("Unrecognized IE Version \"{0}\". Choose from \"IE7\", \"IE8\", \"IE9\", \"IE10\".", version));
                        }
                        break;
                    default:
                        commandLoop.Add(arg);
                        break;
                }
            }

            // Default to IE9
            if (!isVersionSet)
            {
                Browser.Emulate("IE9");
            }

            // Command Loop - Execution
            foreach (string arg in commandLoop)
            {
                string[] parts = arg.Split(':');
                switch (parts[0]) 
                { 
                    case "-t":
                    case "--test":
                        Test();
                        return;
                    case "--render":
                        string url = arg.Replace("--render:", "");
                        Render(url);
                        return;
                    default:
                        // If no switch is defined then we are dealing 
                        // with javascript files that need executing
                        if (arg == parts[0] && !isExecuted)
                        {
                            Open(arg);
                            isExecuted = true;
                        }
                        else if (parts[0].StartsWith("--")) {
                            Help();
                            Exit(0);
                        }
                        break;
                }
            }

            Exit(0);

        }

        /// <summary>
        /// Exits the current thread
        /// </summary>
        /// <param name="exitCode"></param>
        public static void Exit(int exitCode)
        {
#if DEBUG
            // Debugging? Wait for input
            Console.WriteLine();
            Console.WriteLine("Press any key to finish...");
            Console.Read();
#endif
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Runs system tests
        /// </summary>
        static void Test() {

            Console.WriteLine();
            Console.WriteLine("============================================");
            Console.WriteLine("TrifleJS -- System Test");
            Console.WriteLine("============================================");
            Console.WriteLine();

            // Initialize and start console read loop;
            using (Program.context = Initialise())
            {
                try
                {
                    // Load libs
                    context.RunScript(Resources.jasmine, "lib/jasmine.js");
                    context.RunScript(Resources.jasmine_console, "lib/jasmine-console.js");

                    // Load Spec
                    context.RunScript(Resources.spec_phantom, "spec/phantom.js");
                    context.RunScript(Resources.spec_webpage, "spec/webpage.js");

                    // Execute
                    context.RunScript(Resources.run_jasmine, "run-jasmine.js");

                    // Keep running until told to stop
                    // This is to make sure asynchronous code gets executed
                    while (true)
                    {
                        API.Window.CheckTimers();
                    }

                }
                catch (Exception ex)
                {
                    // Handle any exceptions
                    API.Context.Handle(ex);
                }
            }
        }

        /// <summary>
        /// Run TrifleJS in interactive mode
        /// </summary>
        static void Interactive()
        {
            // Initialize and start console read loop;
            using (Program.context = Initialise()) 
            {
                while (true)
                {
                    Console.Write("triflejs> ");
                    try
                    {
                        API.Console.log(context.Run(Console.ReadLine(), "REPL"));
                    }
                    catch (Exception ex)
                    {
                        API.Context.Handle(ex);
                    }
                }
            }   
        }

        /// <summary>
        /// Renders a url
        /// </summary>
        /// <param name="url"></param>
        static void Render(string url) 
        {
            Console.WriteLine("Rendering " + url + "...");

            // Check the URL
            Uri uri = Browser.TryParse(url);
            if (uri == null) {
                Console.Error.WriteLine("Unable to open url: " + url);
                return;
            }

            // Continue if ok
            using (var browser = new Browser())
            {
                browser.Size = new Size(1024, 700);
                browser.Navigate(url); //a file or a url
                browser.ScrollBarsEnabled = false;
                browser.RenderOnLoad(String.IsNullOrEmpty(uri.Host) ? "page.png" : uri.Host + ".png");

                while (browser.ReadyState != System.Windows.Forms.WebBrowserReadyState.Complete)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// Opens a javascript file and executes in host environment
        /// </summary>
        /// <param name="filename">Path to a javascript file</param>
        static void Open(string filename) 
        {
            // Check file
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine(String.Format("File does not exist: {0}", filename));
                return;
            }

            //Initialize a context
            using (Program.context = Initialise())
            {
                // Set Library Path
                API.Phantom.LibraryPath = new FileInfo(filename).DirectoryName;

                try
                {
                    // Run the script
                    context.RunFile(filename);

                    // Keep running until told to stop
                    // This is to make sure asynchronous code gets executed
                    while (true) {
                        API.Window.CheckTimers();
                    }

                }
                catch (Exception ex) {
                    // Handle any exceptions
                    API.Context.Handle(ex);
                }
            }
        }

        /// <summary>
        /// Initialises the environment
        /// </summary>
        /// <returns></returns>
        static API.Context Initialise()
        {
            // Create new context
            API.Context context = new API.Context();

            // Setting core global variables
            context.SetParameter("console", new API.Console());
            context.SetParameter("phantom", new API.Phantom());
            context.SetParameter("trifle", new API.Trifle());
            context.SetParameter("window", new API.Window());

            try
            {
                // Initialise host env
                context.RunScript(Resources.init, "init.js");
                context.RunScript(Resources.trifle_Callback, "trifle.Callback.js");
                context.RunScript(Resources.trifle_modules_WebPage, "trifle.modules.WebPage.js");
                context.RunScript(Resources.trifle_modules_FileSystem, "trifle.modules.FileSystem.js");
                context.RunScript(Resources.trifle_modules_System, "trifle.modules.System.js");
            }
            catch (Exception ex)
            {
                API.Context.Handle(ex);
            }

            // Return context
            return context;
        }

    }
}
