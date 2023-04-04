﻿using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public enum IssueRetrieveMode
    {
        Solved,
        NonSolved,
        All
    }
    public class IssueManagerService
    {
        private readonly StreamingDbContext _streamingDbContext;
        private readonly UserDbContext _userDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        public IssueManagerService(StreamingDbContext streamingDbContext, UserManager<IdentityUser> userManager, UserDbContext userDbContext)
        {
            _streamingDbContext = streamingDbContext;
            _userManager = userManager;
            _userDbContext = userDbContext;
        }
        public async Task CreateIssueAsync(string title, string issueType, string description, ClaimsPrincipal user)
        {
            var identityUser = _userManager.GetUserAsync(user);
            var issue = new Issue
            { 
                Title = title,
                TitleNormalized = GetNormalized(title),
                Type = issueType,
                Description = description,
                Date = DateTime.Now,
                SubmitterId = identityUser.Id
            };

            await _streamingDbContext.Issues.AddAsync(issue);
            await _streamingDbContext.SaveChangesAsync();
        }

        public async Task<bool> SolveIssueAsync(int issueId, string solutionDescription, ClaimsPrincipal user)
        {
            var issue = await _streamingDbContext.Issues.FindAsync(issueId);
            var identityUser = _userManager.GetUserAsync(user);
            if(issue is not null && identityUser is not null)
            {
                issue.ResolverId = identityUser.Id;
                issue.SolutionDescription = solutionDescription;
                issue.IsSolved = true;
                await _streamingDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Issue>> GetIssuesFromPatternAsync(string pattern, IssueRetrieveMode issueRetrieveMode)
        {
            List<Issue> issues = new List<Issue>(0);
            if(issueRetrieveMode == IssueRetrieveMode.NonSolved)
            {
                issues = await _streamingDbContext.Issues.Where(i => i.TitleNormalized.Contains(pattern.ToUpper()) && i.IsSolved == false).ToListAsync();  
            }
            else if(issueRetrieveMode == IssueRetrieveMode.Solved) 
            {
                issues = await _streamingDbContext.Issues.Where(i => i.TitleNormalized.Contains(pattern.ToUpper()) && i.IsSolved == true).ToListAsync();
            }
            else if(issueRetrieveMode == IssueRetrieveMode.All)
            {
                issues = await _streamingDbContext.Issues.Where(i => i.TitleNormalized.Contains(pattern.ToUpper())).ToListAsync();
            }
            return issues;
        }

        public async Task<List<Issue>> GetRecentIssuesAsync(int maxCount, IssueRetrieveMode issueRetrieveMode)
        {
            List<Issue> issues = new List<Issue>(0);
            if (issueRetrieveMode == IssueRetrieveMode.NonSolved)
            {
                issues = await _streamingDbContext.Issues
                                        .Where(i => i.IsSolved == false)
                                        .OrderByDescending(i => i.Date)
                                        .Take(maxCount)
                                        .ToListAsync();
            }
            else if (issueRetrieveMode == IssueRetrieveMode.Solved)
            {
                issues = await _streamingDbContext.Issues
                                        .Where(i => i.IsSolved == true)
                                        .OrderByDescending(i => i.Date)
                                        .Take(maxCount)
                                        .ToListAsync();
            }
            else if (issueRetrieveMode == IssueRetrieveMode.All)
            {
                issues = await _streamingDbContext.Issues
                                        .OrderByDescending(i => i.Date)
                                        .Take(maxCount)
                                        .ToListAsync();
            }
            return issues;
        }

        public async Task<Issue?> GetIssueAsync(int issueId)
        {
            return await _streamingDbContext.Issues.FindAsync(issueId);
        }

        public async Task<IdentityUser?> GetIssueResolver(int issueId)
        {
            var issue = await _streamingDbContext.Issues.FindAsync(issueId);
            if (issue != null)
            {
                return await _userDbContext.Users.FindAsync(issue.ResolverId);
            }
            return null;
        }

        public async Task<IdentityUser?> GetIssueSubmitter(int issueId)
        {
            var issue = await _streamingDbContext.Issues.FindAsync(issueId);
            if (issue != null)
            {
                return await _userDbContext.Users.FindAsync(issue.SubmitterId);
            }
            return null;
        }

        private string GetNormalized(string input)
        {
            return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpper();
        }
    }
}