using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MemcachedService64.Resources
{
    static class Resources
    {
        private const string DEFAULT_NAMESPACE = "MemcachedService64";
        private const string NS_PREFIX = DEFAULT_NAMESPACE + ".Resources.";
        private const string MEMCACHED_FOLDER = NS_PREFIX + "memcached_1_4_5_amd64.";


        /// <summary>
        /// Exécutable memcached.exe (64 bits)
        /// </summary>
        public const string
            MEMCACHED_EXE = "memcached.exe",
            MEMCACHED_EXE_PATH = MEMCACHED_FOLDER + MEMCACHED_EXE;

        /// <summary>
        /// DLL pthreadGC2.dll (64 bits)
        /// </summary>
        public const string
            MEMCACHED_DLL = "pthreadGC2.dll",
            MEMCACHED_DLL_PATH = MEMCACHED_FOLDER + MEMCACHED_DLL;




        /// <summary>
        /// <para>Retourne le flux d'une ressource embarquée.</para>
        /// </summary>
        /// <param name="path">Chemin (espace de nom inclu).</param>
        /// <returns></returns>
        public static Stream GetResourceStream(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (!path.StartsWith(NS_PREFIX))
                path = NS_PREFIX + path;

            return GetResourceStream(Assembly.GetExecutingAssembly(), path);
        }

        /// <summary>
        /// Retourne le flux d'une ressource embarquée.
        /// </summary>
        /// <param name="assembly">Assemblage contenant la ressource embarquée.</param>
        /// <param name="path">Chemin (espace de nom inclu).</param>
        /// <returns></returns>
        public static Stream GetResourceStream(Assembly assembly, string path)
        {
            return assembly.GetManifestResourceStream(path);
        }

        /// <summary>
        /// Extrait les fichiers de memcached dans un dossier temporaire et retourne le chemin absolu de l'exécutable.
        /// </summary>
        /// <returns>Chemin absolu de l'exécutable.</returns>
        public static string ExtractMemcachedToTemporaryDirectory()
        {
            var tempDir = Path.GetTempFileName();
            File.Delete(tempDir);
            Directory.CreateDirectory(tempDir);
            var memCachedPath = Path.Combine(tempDir, Resources.MEMCACHED_EXE);
            using (var str = GetResourceStream(Resources.MEMCACHED_EXE_PATH))
            {
                using (var fs = File.OpenWrite(memCachedPath))
                    CopyStream(str, fs);
            }
            using (var str = GetResourceStream(Resources.MEMCACHED_DLL_PATH))
            {
                using (var fs = File.OpenWrite(Path.Combine(tempDir, Resources.MEMCACHED_DLL)))
                    CopyStream(str, fs);
            }
            return memCachedPath;
        }

        private static void CopyStream(Stream input, Stream output)
        {
            const int BUFFER_LENGTH = 1024; 
            byte[] buffer = new byte[BUFFER_LENGTH];
            int read;
            while ((read = input.Read(buffer, 0, BUFFER_LENGTH)) != 0)
                output.Write(buffer, 0, read);
        }
    }

}
