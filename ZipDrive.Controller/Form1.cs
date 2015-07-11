using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpCompress;
using SharpCompress.Reader.Tar;
using SharpCompress.Archive.Rar;
using SharpCompress.Archive;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpCompress.Reader.Rar;
using SharpCompress.Reader;
using SharpCompress.Common;
using System.Threading;

namespace ZipDrive.Controller
{

    /// <summary>
    /// The mounted drive.
    /// </summary>
    public struct ZipDrive
    {
        public IArchive Archive;

        public string Name;
    }

    public partial class Form1 : Form
    {

        internal const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        // Delegate type to be used as the Handler Routine for SCCH
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
        public Form1()
        {
            InitializeComponent();
        }

        private void mountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ArrayList driveLetters = new ArrayList(26); // Allocate space for alphabet
            for (int i = 65; i < 91; i++) // increment from ASCII values for A-Z
            {
                driveLetters.Add(Convert.ToChar(i)); // Add uppercase letters to possible drive letters
            }

            foreach (string drive in Directory.GetLogicalDrives())
            {
                driveLetters.Remove(drive[0]); // removed used drive letters from possible drive letters
            }

            foreach (char drive in driveLetters)
            {
                comboBox1.Items.Add(drive); // add unused drive letters to the combo box
            }

        }

        List<Process> processes = new List<Process>();

        private void DeleteFolder(string folder)
        {
            System.IO.DirectoryInfo downloadedMessageInfo = new DirectoryInfo(folder);

            foreach (FileInfo file in downloadedMessageInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in downloadedMessageInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public string ExtractAllToTempDirectory(string zip)
        {
            if (!Directory.Exists(Path.GetTempPath() + @"ExtractTemp"))
                Directory.CreateDirectory(Path.GetTempPath() + @"ExtractTemp");
            else
            {
                DeleteFolder(Path.GetTempPath() + @"ExtractTemp");
                Directory.CreateDirectory(Path.GetTempPath() + @"ExtractTemp");
            }
                
            using (Stream stream = File.OpenRead(zip))
            {
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        Console.WriteLine(reader.Entry.Key);
                        reader.WriteEntryToDirectory(Path.GetTempPath() + @"ExtractTemp",
                                                     ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    }
                }
            }
            return Path.GetTempPath() + @"ExtractTemp";
        }

        bool Mounting = false;

        void Mount(object o)
        {
            Mounting = true;
            string filename, dletter, name;
            string[] ar = (string[])o;
            filename = ar[0];
            dletter = ar[1];
            name = ar[2];
            string temp = ExtractAllToTempDirectory(filename);
            System.IO.DirectoryInfo tempInfo = new DirectoryInfo(temp);
            int c = tempInfo.GetFiles().Length + tempInfo.GetDirectories().Length;
            if (c < 1)
            {
                Console.WriteLine("Archive seems to be empty.");
                return;
            }
            string args = "mount -d \"{0}\" -f \"{1}\" -n \"{2}\"";
            string fargs = String.Format(args, dletter, temp, name);
            Console.WriteLine("Arguments: {0}", fargs);
            //return;
            Process process = new Process();
            Console.WriteLine(fargs);

            process.StartInfo = new ProcessStartInfo(@"E:\VSProjects\CSharp\ZipDrive\ZipDrive.Driver\bin\x86\Debug\ZipDrive.Driver.exe", fargs);
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardInput = true;
            //process.StartInfo.CreateNoWindow = true;
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.OutputDataReceived += process_OutputDataReceived;
            process.Exited += process_Exited;
            process.Start();
            processes.Add(process);
            Mounting = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(Mount);
            t.Start(new string[] {
              openFileDialog1.FileName,
              comboBox1.Text,
              textBox2.Text
            });
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Output Received: {0}", e.Data);
        }

        void process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Process Object \"{0}\" exited.", sender);
            processes.Remove((Process)sender);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                string[] sp = openFileDialog1.FileName.Split('\\');
                string filename = sp[sp.Length - 1];
                int index = filename.IndexOf('.');
                filename = filename.Substring(0, index);
                textBox2.Text = filename;
            }
        }

        private bool CloseProcess(Process p)
        {
            if (AttachConsole((uint)p.Id))
            {
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                        return false;
                    p.WaitForExit();
                }
                finally
                {
                    FreeConsole();
                    SetConsoleCtrlHandler(null, false);
                }
                return true;
            }
            return false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Process p in processes)
            {
                
                try
                {
                    Console.WriteLine(CloseProcess(p));
                    p.WaitForExit();
                }
                catch { Console.WriteLine("ff"); }

                //p.Dispose();

            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button1.Enabled = !Mounting;
        }
    }
}
