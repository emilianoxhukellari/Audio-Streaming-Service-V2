using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AudioEngine.Services
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
    public class AudioEngineConfigurationService : IAudioEngineConfigurationService
    {
        private readonly IConfiguration _configuration;
        public int PortCommunication { get; private set; }
        public int PortStreaming { get; private set; }
        public string Host { get; private set; }
        public X509Certificate X509Certificate { get; private set; }

        public int DesktopSongSearchLimit
        {
            get => int.Parse(_configuration["AudioEngineConfiguration:SongSearchLimit"]!);

            set
            {
                Trace.WriteLine($"AAAAAAAAAAAAAAAAAAAAAAAAA: {value}");
                _configuration["AudioEngineConfiguration:SongSearchLimit"] = Convert.ToString(value);
                UpdateAppSetting("AudioEngineConfiguration:SongSearchLimit", value.ToString());
            }

        }

        public void UpdateAppSetting(string key, string value)
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

        public AudioEngineConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            PortCommunication = int.Parse(_configuration["AudioEngineConfiguration:PortCommunication"]!);
            PortStreaming = int.Parse(_configuration["AudioEngineConfiguration:PortStreaming"]!);
            Host = _configuration["AudioEngineConfiguration:Host"]!;
            X509Certificate = new X509Certificate(_configuration["X509Certificate:RelativePath"]!, _configuration["X509Certificate:Password"]!);

            if (Host == null)
            {
                throw new InvalidDataException("Cannot find host path.");
            }
        }
    }
}
