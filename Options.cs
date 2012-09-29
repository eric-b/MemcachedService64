using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;

namespace MemcachedService64
{
    /// <summary>
    /// <para>Options propres au service (et non à memcached).</para>
    /// <para>Pour passer des paramètres à memcached, aucune de ces options ne doit être définie.</para>
    /// </summary>
    class Options
    {
        [Option(null, "install", DefaultValue = false, MutuallyExclusiveSet = "install", HelpText = "Installer le service Windows")]
        public bool Install { get; set; }

        [Option(null, "uninstall", DefaultValue = false, MutuallyExclusiveSet = "install", HelpText = "Désinstaller le service Windows")]
        public bool Uninstall { get; set; }
    }
}
