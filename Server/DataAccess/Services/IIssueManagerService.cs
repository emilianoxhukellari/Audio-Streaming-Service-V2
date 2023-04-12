using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using static DataAccess.Services.IssueManagerService;

namespace DataAccess.Services
{
    public interface IIssueManagerService
    {
        /// <summary>
        /// This method takes the currently logged in user and returns the number of issues that this user has created.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<int> GetNumberOfSubmittedIssuesByUserAsync(ClaimsPrincipal user);

        /// <summary>
        /// This method wll return the number of issues of based on issue type.
        /// </summary>
        /// <param name="issueType"></param>
        /// <returns></returns>
        Task<int> GetNumberOfIssues(IssueType issueType);

        /// <summary>
        /// Call this method to create an issue and store it in the database.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="issueType"></param>
        /// <param name="description"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> CreateIssueAsync(string title, string issueType, string description, ClaimsPrincipal user);

        /// <summary>
        /// Get the issue based on id.
        /// </summary>
        /// <param name="issueId"></param>
        /// <returns></returns>
        Task<Issue?> GetIssueAsync(int issueId);

        /// <summary>
        /// Returns the user that solved the issue.
        /// </summary>
        /// <param name="issueId"></param>
        /// <returns></returns>
        Task<IdentityUser?> GetIssueResolverAsync(int issueId);

        /// <summary>
        /// Returns a list of issues that match the pattern. The pattern must be part of the issue title.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="issueRetrieveMode"></param>
        /// <returns></returns>
        Task<List<Issue>> GetIssuesFromPatternAsync(string pattern, IssueRetrieveMode issueRetrieveMode);

        /// <summary>
        /// Returns the user that created and submitted the issue.
        /// </summary>
        /// <param name="issueId"></param>
        /// <returns></returns>
        Task<IdentityUser?> GetIssueSubmitterAsync(int issueId);

        /// <summary>
        /// Gets a list of recent issues. 
        /// </summary>
        /// <param name="maxCount"></param>
        /// <param name="issueRetrieveMode"></param>
        /// <returns></returns>
        Task<List<Issue>> GetRecentIssuesAsync(int maxCount, IssueRetrieveMode issueRetrieveMode);

        /// <summary>
        /// Solve an issue by giving a description. The state of the issue will be changed to "IsSolved = true".
        /// </summary>
        /// <param name="issueId"></param>
        /// <param name="solutionDescription"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> SolveIssueAsync(int issueId, string solutionDescription, ClaimsPrincipal user);

        /// <summary>
        /// Change the state of the issue t0 "IsSolved = false". This will also remove the description.
        /// </summary>
        /// <param name="issueId"></param>
        /// <returns></returns>
        Task<bool> UnresolveIssueAsync(int issueId);
    }
}