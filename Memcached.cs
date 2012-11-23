using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NLog;

namespace MemcachedService64
{
    /// <summary>
    /// <para>Wrapper pour l'exécutable embarqué memcached.</para>
    /// <para>L'exécutable est extrait dans un dossier temporaire qui sera supprimé lorsque l'instance de cette classe sera libérée.</para>
    /// </summary>
    class Memcached : IDisposable
    {
        private Logger _logger;
        private readonly string _tempDir;
        private readonly string _memCachedPath;
        private Process _memCachedProcess;
        private string[] _defaultArgs;

        #region Ctor
        /// <summary>
        /// Constructeur permettant de définir les arguments à passer en ligne de commande à memcached.
        /// </summary>
        /// <param name="defaultArguments">Arguments par défaut à passer à la commande de memcached (peut être <c>null</c>).</param>
        public Memcached(string[] defaultArguments)
        {
            _logger = LogManager.GetLogger(this.GetType().Name);
            _defaultArgs = defaultArguments;
            _memCachedPath = Resources.Resources.ExtractMemcachedToTemporaryDirectory();
            _tempDir = Path.GetDirectoryName(_memCachedPath);
            _logger.Debug("Exécutable memcached extrait dans le dossier temporaire {0}.", _tempDir);
        }

        /// <summary>
        /// Constructeur par défaut: extraction de memcached dans un dossier temporaire.
        /// </summary>
        public Memcached()
            : this(null)
        {

        }
        #endregion

        /// <summary>
        /// <para>Démarre un nouveau processus memcached avec les arguments par défaut (indiqué dans le constructeur de cet objet).</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Un processus est déjà en cours.</exception>
        public void Start()
        {
            Start(_defaultArgs);
        }

        /// <summary>
        /// <para>Démarre un nouveau processus memcached avec les arguments spécifiés.</para>
        /// </summary>
        /// <param name="args">Arguments à passer à memcached.</param>
        /// <exception cref="InvalidOperationException">Un processus est déjà en cours.</exception>
        public void Start(string[] args)
        {
            if (_memCachedProcess != null)
                throw new InvalidOperationException("Un processus memcached a déjà été démarré.");

            ProcessStartInfo pStartInfo = new ProcessStartInfo(_memCachedPath)
            {
                Arguments = args != null ? string.Join(" ", args) : null,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };
            _memCachedProcess = new Process();
            _memCachedProcess.StartInfo = pStartInfo;
            _memCachedProcess.OutputDataReceived += OnOutputData;
            _memCachedProcess.ErrorDataReceived += OnOutputData;


            _memCachedProcess.Start();
            _logger.Debug("Processus memcached démarré avec les arguments: {0}", pStartInfo.Arguments);
            _memCachedProcess.BeginOutputReadLine();
            _memCachedProcess.BeginErrorReadLine();

        }



        /// <summary>
        /// Redirection de la sortie console de memcached (noter que memcached écrit sur la sortie d'erreur sans qu'il s'agisse d'erreurs).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnOutputData(object sender, DataReceivedEventArgs e)
        {
            _logger.Info(e.Data);
        }

        /// <summary>
        /// <para>Termine le processus qui exécute memcached.</para>
        /// <remarks>Cette méthode ne lève aucune exception.</remarks>
        /// </summary>
        public void Stop()
        {
            if (_memCachedProcess == null)
                return;

            try
            {
                _memCachedProcess.Refresh();

                if (_memCachedProcess.HasExited)
                {
                    _logger.Warn("Le processus memcached était déjà arrêté ({1} - code de sortie: {0}).", _memCachedProcess.ExitCode, _memCachedProcess.ExitTime);
                    return;
                }

                /* Tente au mieux d'informer memcached de sa fin proche (malgré tout, il semble que .Kill() soit toujours nécessaire).
                 * On pourrait utiliser GenerateConsoleCtrlEvent (P/Invoke) mais cela ne fonctionne que si le processus de memcached alloue une console, ce qui n'est pas notre cas.
                 * */
                _memCachedProcess.StandardInput.Close();
                _memCachedProcess.CloseMainWindow();
                _memCachedProcess.WaitForExit(500);
                if (!_memCachedProcess.HasExited)
                {
                    _memCachedProcess.Kill();
                    _logger.Debug("Processus memcached tué.");
                }
                else
                    _logger.Debug("Processus memcached arrêté.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
            finally
            {
                _memCachedProcess.OutputDataReceived -= OnOutputData;
                _memCachedProcess.ErrorDataReceived -= OnOutputData;
                _memCachedProcess.Dispose();
                _memCachedProcess = null;
            }
        }

        #region IDisposable
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(_tempDir))
                {
                    try
                    {
                        Stop();
                        _logger.Debug("Suppression du dossier temporaire");
                        Directory.Delete(_tempDir, true); // il semble que dans certains cas, l'accès soit refusé (peut etre si le processus memcached est encore en cours de destruction?)
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.ToString());
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
