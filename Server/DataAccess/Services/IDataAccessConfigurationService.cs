namespace DataAccess.Services
{
    public interface IDataAccessConfigurationService
    {
        string AudioFilesRelativePath { get; }
        string ImageFilesRelativePath { get; }
    }
}