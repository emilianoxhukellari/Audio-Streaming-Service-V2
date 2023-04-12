using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace DataAccess.Services
{
    public static class JsonElementExtensions
    {
        public static Dictionary<string, JsonElement> ToDictionary(this JsonElement jsonElement)
        {
            var dictionary = new Dictionary<string, JsonElement>();

            foreach (var property in jsonElement.EnumerateObject())
            {
                dictionary.Add(property.Name, property.Value);
            }

            return dictionary;
        }
    }

    public class DataAccessConfigurationService : IDataAccessConfigurationService
    {
        private readonly IConfiguration _configuration;
        public string AudioFilesRelativePath { get; }
        public string ImageFilesRelativePath { get; }

        public int DesktopAppSongSearchLimit
        {
            get => int.Parse(_configuration["DataAccessConfiguration:DesktopAppSongSearchLimit"]!);

            set
            {
                int validValue = value < 0 ? 0 : value;
                _configuration["DataAccessConfiguration:DesktopAppSongSearchLimit"] = Convert.ToString(validValue);
                UpdateAppSetting("DataAccessConfiguration:DesktopAppSongSearchLimit", validValue.ToString());
            }

        }

        public int WebAppSongSearchLimit
        {
            get => int.Parse(_configuration["DataAccessConfiguration:WebAppSongSearchLimit"]!);

            set
            {
                int validValue = value < 0 ? 0 : value;
                _configuration["DataAccessConfiguration:WebAppSongSearchLimit"] = Convert.ToString(validValue);
                UpdateAppSetting("DataAccessConfiguration:WebAppSongSearchLimit", validValue.ToString());
            }
        }

        public int DesktopAppClientCountLimit
        {
            get => int.Parse(_configuration["DataAccessConfiguration:DesktopAppClientCountLimit"]!);

            set
            {
                int validValue = value < 0 ? 0 : value;
                _configuration["DataAccessConfiguration:DesktopAppClientCountLimit"] = Convert.ToString(validValue);
                UpdateAppSetting("DataAccessConfiguration:DesktopAppClientCountLimit", validValue.ToString());
            }
        }

        public DataAccessConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;

            AudioFilesRelativePath = _configuration["DataAccessConfiguration:AudioFilesRelativePath"]!;

            if (AudioFilesRelativePath == null)
            {
                throw new InvalidDataException("Cannot find relative audio files path.");
            }

            ImageFilesRelativePath = _configuration["DataAccessConfiguration:ImageFilesRelativePath"]!;

            if (ImageFilesRelativePath == null)
            {
                throw new InvalidDataException("Cannot find relative images files path.");
            }
        }

        private void UpdateAppSetting(string key, string value)
        {
            var configJson = File.ReadAllText("appsettings.json");
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);

            var keys = key.Split(':');
            UpdateSetting(config, keys, 0, value);

            var updatedConfigJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("appsettings.json", updatedConfigJson);
        }

        private void UpdateSetting(Dictionary<string, JsonElement> config, string[] keys, int index, string value)
        {
            if (index == keys.Length - 1)
            {
                if (config[keys[index]].ValueKind == JsonValueKind.String)
                {
                    config[keys[index]] = JsonDocument.Parse($"\"{value}\"").RootElement;
                }
                else
                {
                    config[keys[index]] = JsonDocument.Parse(value).RootElement;
                }
            }
            else
            {
                var nestedConfig = config[keys[index]].ToDictionary();
                UpdateSetting(nestedConfig, keys, index + 1, value);
                config[keys[index]] = JsonDocument.Parse(JsonSerializer.Serialize(nestedConfig)).RootElement;
            }
        }
    }
}
