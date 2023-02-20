using System;
using System.Configuration;

namespace Client_Application.Config
{
    /// <summary>
    /// App config configuration class.
    /// </summary>
    public static class Config
    {
        public static string GetHost()
        {
            var configHost = ConfigurationManager.AppSettings["Host"];
            return configHost != null ? configHost : "";
        }

        public static int GetPortCommunication()
        {
            var configPortCommunication = ConfigurationManager.AppSettings["PortCommunication"];
            return configPortCommunication != null ? Int32.Parse(configPortCommunication) : 8181;
        }

        public static int GetPortStreaming()
        {
            var configPortStreaming = ConfigurationManager.AppSettings["PortStreaming"];
            return configPortStreaming != null ? Int32.Parse(configPortStreaming) : 8080;
        }

        public static string GetPlaylistsRelativePath()
        {
            var playlistsRelativePath = ConfigurationManager.AppSettings["PlaylistsRelativePath"];
            return playlistsRelativePath != null ? playlistsRelativePath : "";
        }

        public static string GetIconsRelativePath()
        {
            var iconsRelativePath = ConfigurationManager.AppSettings["IconsRelativePath"];
            return iconsRelativePath != null ? iconsRelativePath : "";
        }

        public static string GetClientId()
        {
            var clientId = ConfigurationManager.AppSettings["ClientId"];
            return clientId != null ? clientId : "000000";
        }
    }
}
