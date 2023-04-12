using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using static DataAccess.Services.IssueManagerService;

namespace DataAccess.Services
{
    public interface IIssueManagerService
    {
        Task<int> GetNumberOfSubmittedIssuesByUserAsync(ClaimsPrincipal user);
        Task<int> GetNumberOfIssues(IssueType issueType);
        Task<bool> CreateIssueAsync(string title, string issueType, string description, ClaimsPrincipal user);
        Task<Issue?> GetIssueAsync(int issueId);
        Task<IdentityUser?> GetIssueResolverAsync(int issueId);
        Task<List<Issue>> GetIssuesFromPatternAsync(string pattern, IssueRetrieveMode issueRetrieveMode);
        Task<IdentityUser?> GetIssueSubmitterAsync(int issueId);
        Task<List<Issue>> GetRecentIssuesAsync(int maxCount, IssueRetrieveMode issueRetrieveMode);
        Task<bool> SolveIssueAsync(int issueId, string solutionDescription, ClaimsPrincipal user);
        Task<bool> UnresolveIssueAsync(int issueId);
    }
}