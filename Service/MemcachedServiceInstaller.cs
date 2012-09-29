using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using NLog;

namespace MemcachedService64.Service
{
    /// <summary>
    /// Gère l'installation automatique du service Windows.
    /// </summary>
    [RunInstaller(true)]
    public sealed class MemcachedServiceInstaller : Installer
    {
        private readonly Logger _logger;

        public MemcachedServiceInstaller()
        {
            _logger = LogManager.GetLogger(this.GetType().Name);
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            #region
            /* Si le service est installé dans /Program Files/, vérifie que le déploiement s'est effectué dans le bon dossier sur plateforme 64 bits.
             * On ne peut se baser sur le contexte courant car l'installation du service peut être initiée par une application 32 bits.
             * */
            if (Is64BitOperatingSystem)
            {
                PortableExecutableKinds peKind;
                ImageFileMachine imgMachine;
                MemcachedService._applicationAssembly.ManifestModule.GetPEKind(out peKind, out imgMachine);
                var x86Pf = GetProgramFilesX86Path(); // .NET >= 4.0 : utiliser Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                var nativePf = Environment.GetEnvironmentVariable("ProgramFiles"); // on ne peut se baser sur Environment.SpecialFolder.ProgramFiles en mode WOW64.
                if (MemcachedService._applicationPath.StartsWith(nativePf, StringComparison.OrdinalIgnoreCase))
                {
                    // Vérification appliquée uniquement si installation dans "Program Files" ou "Program Files (x86)".
                    if (peKind == PortableExecutableKinds.Required32Bit)
                    {
                        // Valide l'installation dans pfx86
                        if (!MemcachedService._applicationPath.StartsWith(x86Pf, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(string.Format("Dossier d'installation du service erroné: {0}.\r\nVeuillez placer l'application dans le dossier {1} (plateforme ciblée: 32 bits).", MemcachedService._applicationPath, x86Pf));
                    }
                    else
                    {
                        // Valide l'installation dans pf
                        if (MemcachedService._applicationPath.StartsWith(x86Pf, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(string.Format("Dossier d'installation du service erroné: {0}.\r\nVeuillez placer l'application dans le dossier {1} (plateforme 64 bits).", MemcachedService._applicationPath, nativePf));
                    }
                }
            }
            #endregion

            // memcached 64 bits uniquement: limite donc ce process à une plateforme 64 bits:
            if (IntPtr.Size != 8)
                throw new InvalidOperationException("Ce service doit être installé sur une plateforme 64 bits.");

            
            serviceInstaller.DisplayName = string.Format("MemcachedService64 ({0})", Path.GetFileName(MemcachedService._applicationPath)); // permet d'identifier les éventuelles différentes instances de ce service en fonction du nom du dossier content cet assemblage...
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            //.NET >= 4.0: serviceInstaller.DelayedAutoStart = true;
            try
            {
                serviceInstaller.Description = ((AssemblyDescriptionAttribute)AssemblyDescriptionAttribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyDescriptionAttribute))).Description;
            }
            catch { }

            serviceInstaller.ServiceName = MemcachedService._serviceName;

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }

        /// <summary>
        /// Retourne le chemin d'accès au dossier Program Files (x86).
        /// </summary>
        /// <returns></returns>
        static string GetProgramFilesX86Path()
        {
            if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        /// <summary>
        /// Installation standalone du service.
        /// </summary>
        /// <param name="undo"></param>
        /// <param name="args"></param>
        public static void Install(bool undo, string[] args)
        {
            // Source: https://groups.google.com/forum/?hl=en&fromgroups=#!topic/microsoft.public.dotnet.languages.csharp/TUXp6lRxy6Q
            var logger = LogManager.GetLogger("MemcachedServiceInstaller");
            try
            {
                logger.Info(undo ? "uninstalling" : "installing");
                using (AssemblyInstaller inst = new
AssemblyInstaller(typeof(MemcachedServiceInstaller).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        if (undo)
                        {
                            inst.Uninstall(state);
                        }
                        else
                        {
                            inst.Install(state);
                            inst.Commit(state);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.ToString());
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch (Exception ex2)
                        {
                            logger.Error(ex2.ToString());
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Retourne <c>true</c> si un OS 64 bits est identifié, quelque soit le mode dans lequel le processus courant s'exécute.
        /// </summary>
        static bool Is64BitOperatingSystem
        {
            get
            {
                if (IntPtr.Size == 8)
                    return true; // mode 64 bits possible uniquement sur une plateforme 64 bits...

                // Détermine si mode WOW64...
                bool isWow64;
                return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && NativeMethods.IsWow64Process(NativeMethods.GetCurrentProcess(), out isWow64) && isWow64;
            }
        }

        static bool ModuleContainsFunction(string moduleName, string methodName)
        {
            IntPtr hModule = NativeMethods.GetModuleHandle(moduleName);
            if (hModule != IntPtr.Zero)
                return NativeMethods.GetProcAddress(hModule, methodName) != IntPtr.Zero;
            return false;
        }
    }
}
