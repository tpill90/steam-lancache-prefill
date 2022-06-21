using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    public class ConfigInfo
    {
        public string Architecture { get; set; }
        
        public string Language { get; set; }
        public bool LowViolence { get; set; }

        public List<string> OperatingSystemList { get; set; }

        public ConfigInfo()
        {
            // Parameterless for deserialization
        }

        public ConfigInfo(KeyValue keyValues)
        {
            var c = keyValues.Children;

            // Architecture
            var architectureSection = c.FirstOrDefault(e => e.Name == "osarch");
            if (architectureSection != null)
            {
                Architecture = architectureSection.Value;
            }

            // Language 
            var languageSection = c.FirstOrDefault(e => e.Name == "language");
            if (languageSection != null && !String.IsNullOrEmpty(languageSection.Value))
            {
                Language = languageSection.Value;
            }

            // Low Violence
            var lowViolenceSection = c.FirstOrDefault(e => e.Name == "lowviolence");
            if (lowViolenceSection != null && lowViolenceSection.Value == "1")
            {
                LowViolence = true;
            }

            // Operating System
            var osSection = c.FirstOrDefault(e => e.Name == "oslist");
            if (osSection != null)
            {
                OperatingSystemList = osSection.Value.Split(',').ToList();
            }
        }
    }
}
