using System.Diagnostics;
using System.Reflection;
using System.Windows;
using log4net;
using System;

namespace QuasaroDRV.AppInfoWindows
{
    /// <summary>
    /// Interaktionslogik für About.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        ILog log;

        public AboutWindow()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                lbVersion.Content = fvi.ProductVersion;
            }
            catch(Exception ex)
            {
                this.log.Error(ex.GetType().Name + " while loading assembly version: " + ex.Message, ex);
                lbVersion.Content = "#ERROR#";
            }
        }

        private void btnCredits_Click(object sender, RoutedEventArgs e)
        {
            CreditsWindow crWindow = new CreditsWindow();
            crWindow.Owner = this;
            crWindow.ShowDialog();
        }
    }
}
