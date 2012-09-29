using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace MemcachedService64
{
    static class AppSettings
    {
        /// <summary>
        /// Parse les options pour memcached.
        /// </summary>
        /// <returns></returns>
        public static string[] GetMemcachedArguments()
        {
            /* Conventions:
             * La clé des paramètre n'est pas figée. 
             * Seules les clés qui sont préfixées par '-' sont prises en compte.
             * Le texte après un espace est ignoré (permet de commenter la clé).
             * La valeur est copiée telle quelle. Des guillemets peuvent être inclus dans la valeur (non géré par cette méthode).
             * Les valeurs vides sont ignorées.
             * 
             * Le paramètre "custom options" permet de spécifier d'autres paramètres à memcached (valeur copiée en fin de résultat de cette méthode).
             * Note: les guillemets ne sont pas supportés dans le paramètre "custom options" (les différentes valeurs ne doivent donc pas contenir d'espace
             * utilisé comme séparateur).
             * */

            const string CUSTOM_OPTIONS = "custom options";
            List<string> args = new List<string>(6);
            var appSettings = ConfigurationManager.AppSettings;
            var keys = appSettings.AllKeys.Where(t => t.StartsWith("-")).ToArray();
            foreach (var k in keys)
            {
                var value = appSettings[k];
                if (string.IsNullOrEmpty(value))
                    continue;
                var index = k.IndexOf(' ');
                args.Add(index != -1 ? k.Substring(0, index) : k);
                args.Add(value);
            }
            var custom = appSettings[CUSTOM_OPTIONS];
            if (!string.IsNullOrEmpty(custom))
                args.AddRange(custom.Trim().Split(' '));


            return args.ToArray();
        }
    }

}
