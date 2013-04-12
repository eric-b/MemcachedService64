using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using NLog;

namespace MemcachedService64.Service
{
    class MemcachedService : ServiceBase
    {
        /// <summary>
        /// Nom du service (correspond au nom de cet assemblage).
        /// </summary>
        internal static string _serviceName;

        /// <summary>
        /// Chemin absolu de l'application.
        /// </summary>
        internal static string _applicationPath;

        /// <summary>
        /// Assemblage du service.
        /// </summary>
        internal static Assembly _applicationAssembly;

        private Logger _logger;

        /// <summary>
        /// Hôte memcached.
        /// </summary>
        private Memcached _memCached;

        #region Ctor
        static MemcachedService()
        {
            _applicationAssembly = Assembly.GetExecutingAssembly();
            _serviceName = _applicationAssembly.GetName().Name;

            const string SCHEMA_FILE = @"file:\";
            var path = Path.GetDirectoryName(_applicationAssembly.CodeBase);
            if (path.StartsWith(SCHEMA_FILE))
                path = path.Remove(0, SCHEMA_FILE.Length);
            _applicationPath = path;
        }

        /// <summary>
        /// Instanciation et initialisation du service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MemcachedService()
        {
            _logger = LogManager.GetLogger(this.GetType().Name);
            _logger.Debug(string.Format("Instanciation du service {0} ({1})...\r\nExécution 64 bits: {2}\r\nPf: {3}", _serviceName, _applicationPath, IntPtr.Size == 8, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
            this.ServiceName = _serviceName;
            EventLog.Source = _serviceName;
            this.EventLog.Log = "Application";
            this.AutoLog = true;

            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanStop = true;

            _memCached = new Memcached();
        }
        #endregion

        #region ServiceBase


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _memCached.Dispose();
                    _logger.Debug("Libération du service.");
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Démarrage du service.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            try
            {
                _logger.Info(string.Format("Pré-Démarrage du service. Args ({1}): {0}", String.Join(" ", args), args != null ? args.Length : -1));

                if (!Environment.UserInteractive && ((args == null) || (args.Length == 0)))
                    args = AppSettings.GetMemcachedArguments();
                _logger.Info("Démarrage du service. Args: " + String.Join(" ", args));

                _memCached.Start(args);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.ToString());
                throw;
            }
            finally
            {
                base.OnStart(args);
            }
        }

        /// <summary>
        /// Arrêt total du service.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                _logger.Debug("Arrêt du service");
                _memCached.Stop();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.ToString());
                throw;
            }
            finally
            {
                base.OnStop();
            }
        }




        #endregion

        /// <summary>
        /// <para>Démarre le service.</para>
        /// <remarks>Cette méthode est prévue pour un contexte interactif et appelle la méthode <see cref="OnStart"/>.</remarks>
        /// </summary>
        /// <param name="args"></param>
        internal void SetUp(string[] args)
        {
            OnStart(args);
        }



        /// <summary>
        /// <para>Arrête le service.</para>
        /// <remarks>Cette méthode est prévue pour un contexte interactif et appelle la méthode <see cref="OnStop"/>.</remarks>
        /// </summary>
        internal void TearDown()
        {
            OnStop();
        }
    }
}
