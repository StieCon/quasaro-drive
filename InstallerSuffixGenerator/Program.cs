using InstallerSuffixGenerator.Version14;
using System;
using System.Windows.Forms;

namespace InstallerSuffixGenerator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Version14Form dialog = new Version14Form();
            Application.Run(dialog);
        }
    }
}
