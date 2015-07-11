using CommandLine;
using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZipDrive.Driver
{

    public class CParserOptions
    {

        public class MountSubOptions
        {
            [Option('d', "drive", HelpText = "Specifies the drive letter.", Required = true)]
            public String Drive { get; set; }

            [Option('f', "file", HelpText = "File to mount under drive letter.", Required = true)]
            public String Path { get; set; }

            [Option('n', "name", HelpText = "The name of the mounted drive.", Required = true)]
            public String DriveName { get; set; }
        }

        public class UnmountSubOptions
        {
            [Option('d', "drive", HelpText = "Specifies the drive letter.", Required=true)]
            public String Drive { get; set; }
        }

        [VerbOption("mount", HelpText = "Mounts a ZipDrive using specified settings")]
        public MountSubOptions Mount { get; set; }

        [VerbOption("unmount", HelpText = "Unmounts a ZipDrive using specified settings")]
        public UnmountSubOptions Unmount { get; set; }
    }

    public class MainProgram
    {
        static ZipDriveOperations rfs;
        private static void MountDrive(CParserOptions.MountSubOptions options)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            Console.WriteLine("Settings, Drive Letter {0}, Path {1}, DriveName {2}", options.Drive, options.Path, options.DriveName);
            try
            {
                rfs = new ZipDriveOperations(options.Path, options.DriveName);
                char driveLetter = options.Drive[0];
                //Console.WriteLine(options.Path);
                rfs.DriveLetter = driveLetter;
                rfs.Mount(driveLetter + ":\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);
                Console.WriteLine("Success");
            }
            catch (DokanException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Environment.Exit(1);
            }
            //Console.ReadKey();
        }

        public static void UnmountDrive(CParserOptions.UnmountSubOptions options)
        {
            //Console.CancelKeyPress += Console_CancelKeyPress;
            char c = options.Drive.ToUpper()[0];
            Console.WriteLine("Unmounting drive letter \"{0}\".", c);
            try
            {
                bool ba = Dokan.Unmount(c);
                bool bb = Dokan.RemoveMountPoint(c + ":\\");
                Console.WriteLine("Unmounting Results: {0} {1}", ba, bb);
                if (ba)
                {
                    Console.WriteLine("It seemed to have successfully unmounted \"{0}\". Check and make sure it did.", c);
                }
                else
                {
                    Console.WriteLine("It seemed to have errored when unmounting \"{0}\". Is it already unmounted?", c);
                }
            }
            catch { }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                e.Cancel = true;
                try
                {
                    Dokan.Unmount(rfs.DriveLetter);
                    Dokan.RemoveMountPoint(rfs.DriveLetter + ":\\");
                }
                catch (Exception ex) { Console.WriteLine("Uh oh! Closing in 5 seconds. {0}", ex); Thread.Sleep(5000); Environment.Exit(1); }
            }
        }

        public static void Main(string[] args)
        {
            foreach (string arg in args)
                Console.WriteLine(args);
            CParserOptions opts = new CParserOptions();
            string invokeVerb = "";
            object invokeVerbObject = null;
            Console.WriteLine("Reading CMD Line.");
            if (!CommandLine.Parser.Default.ParseArguments(args, opts, (verb, verbObject) =>
            {
                invokeVerb = verb;
                invokeVerbObject = verbObject;
            }))
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);

            //Console.WriteLine("TEst: {0} {1}", invokeVerb, invokeVerbObject);

            if (invokeVerb == "mount")
                MountDrive((CParserOptions.MountSubOptions)invokeVerbObject);

            if (invokeVerb == "unmount")
                UnmountDrive((CParserOptions.UnmountSubOptions)invokeVerbObject);
        }

    }
}
