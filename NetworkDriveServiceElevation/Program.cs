using System;
using System.ServiceProcess;

namespace NetworkDriveServiceElevation
{
    static class Program
    {
        const int WaitForServiceStateTimeoutSeconds = 10;


        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "RestartWebClient":
                            RestartWebClient();
                            break;

                        default:
                            Console.Error.WriteLine("Unknown command \"" + args[0] + "\"");
                            return -2;
                    }
                    Console.WriteLine("Done");
                }
                else
                {
                    Console.WriteLine("Available commands:");
                    Console.WriteLine();
                    Console.WriteLine("- RestartWebClient");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return -1;
            }
        }


        static void RestartWebClient()
        {
            ServiceController service = FindService("WebClient");

            if (service == null)
                throw new Exception("Service \"WebClient\" not found.");

            if (service.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine("Stopping service \"WebClient\"...");
                try
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(WaitForServiceStateTimeoutSeconds));
                }
                catch { }
            }
            service.Refresh();
            if (service.Status != ServiceControllerStatus.Running)
            {
                Console.WriteLine("Starting service \"WebClient\"...");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(WaitForServiceStateTimeoutSeconds));
            }
        }
        static ServiceController FindService(string name)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController service in services)
            {
                if (service.ServiceName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return service;
                }
            }
            return null;
        }
    }
}
