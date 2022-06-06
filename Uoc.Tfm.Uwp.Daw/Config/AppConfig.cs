using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Uoc.Tfm.Uwp.Daw.Config
{
    public class AppConfig
    {
        private readonly IConfigurationRoot _configuration;

        public AppConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Package.Current.InstalledLocation.Path)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public string GetKey(string key) => (string)_configuration.GetValue(typeof(string), key);

        public bool IsPianoRollShown { get; set; } = false;
    }
}
