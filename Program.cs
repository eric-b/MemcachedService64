using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using CommandLine;
using MemcachedService64.Service;
using NLog;

namespace MemcachedService64
{
    static class Program
    {
        private static Logger _logger;

        /// <summary>
        /// Point d'entrée.
        /// </summary>
        static void Main(string[] args)
        {
            _logger = LogManager.GetLogger("Program");
            if (!Environment.UserInteractive)
            {
                #region Considère un contexte de service
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new Service.MemcachedService() 
                };
                ServiceBase.Run(ServicesToRun);
                #endregion
            }
            else
            {
                #region Considère un contexte utilisateur (interactif)
                _logger.Trace("Mode interactif");
                int exitCode = 0;
                try
                {
                    #region Parse la ligne de commande
                    Options options = new Options();
                    ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(false, true, Console.Error));
                    bool parserSucceed = parser.ParseArguments(args, options);
                    #endregion

                    if (options.Install || options.Uninstall)
                    {
                        if (!parserSucceed)
                            throw new ArgumentException("Ligne de commande invalide.");
                        Service.MemcachedServiceInstaller.Install(options.Uninstall, args);
                        return;
                    }

                    using (var service = new Service.MemcachedService())
                    {
                        service.SetUp(!parserSucceed ? args.Length != 0 ? args : AppSettings.GetMemcachedArguments() : AppSettings.GetMemcachedArguments());
                        _logger.Info("Service démarré. Appuyez sur ECHAP pour mettre fin au service...");

                        while (!NativeMethods.PeekEscapeKey()) { }
                        _logger.Info("[ESC]");
                        service.TearDown();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Fatal(ex.ToString());
                    exitCode = -1;
                }
                Environment.Exit(exitCode);
                #endregion
            }
        }


    }
}
