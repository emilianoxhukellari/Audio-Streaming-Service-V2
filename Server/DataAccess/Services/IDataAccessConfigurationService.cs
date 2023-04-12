namespace DataAccess.Services
{
    public interface IDataAccessConfigurationService
    {
        string AudioFilesRelativePath { get; }
        int DesktopAppClientCountLimit { get; set; }
        int DesktopAppSongSearchLimit { get; set; }
        string ImageFilesRelativePath { get; }
        int WebAppSongSearchLimit { get; set; }

    }
}