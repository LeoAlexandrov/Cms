using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Infrastructure.Data;
using AleProjects.Cms.Infrastructure.Auth;
using System.Collections.Generic;


namespace AleProjects.Cms.Application.Services
{

	public class UserManagementService(CmsDbContext dbContext, IAuthorizationService authService, IRoleClaimPolicies policies)
	{
		private readonly CmsDbContext _dbContext = dbContext;
		private readonly IAuthorizationService _authService = authService;
		private readonly IRoleClaimPolicies _policies = policies;


		public bool NoUsers()
		{
			return !_dbContext.Users.Any(); ;
		}

		public async Task<DtoUserLiteResult[]> GetList(ClaimsPrincipal user)
		{
			DtoUserLiteResult[] result;

			var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

			if (authResult.Succeeded) 
			{
				result = await _dbContext.Users
					.OrderBy(u => u.Id)
					.OrderBy(u => u.Login)
					.Select(u => new DtoUserLiteResult(u))
					.ToArrayAsync();
			}
			else
			{
				string login = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

				result = await _dbContext.Users
					.Where(u => u.Login == login)
					.Select(u => new DtoUserLiteResult(u))
					.ToArrayAsync();
			}

			return result;
		}

		public async Task<Result<DtoUserResult>> GetById(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageUser");

			if (!authResult.Succeeded)
				return Result<DtoUserResult>.Forbidden();

			var u = await _dbContext.Users.FindAsync(id);

			if (u == null)
				return Result<DtoUserResult>.NotFound();

			return Result<DtoUserResult>.Success(new(u, u.Login == user.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value));
		}

		public async Task<Result<DtoUserResult>> GetByApiKey(string apikey)
		{
			var u = await _dbContext.Users.FirstOrDefaultAsync(u => u.ApiKey == apikey);

			if (u == null)
				return Result<DtoUserResult>.NotFound();

			return Result<DtoUserResult>.Success(new(u, false));
		}

		public string[] UserRoles(ClaimsPrincipal user)
		{
			string role = user.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
			int idx = Array.IndexOf(this._policies.Roles, role);

			return idx >= 0 ? this._policies.Roles.Skip(idx).ToArray() : [];
		}

		public async Task<Result<DtoUserResult>> CreateUser(DtoCreateUser dto, ClaimsPrincipal user)
		{
			if (user != null)
			{
				var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

				if (!authResult.Succeeded)
					return Result<DtoUserResult>.Forbidden();

				var roles = UserRoles(user);

				if (!roles.Contains(dto.Role))
					return Result<DtoUserResult>.BadParameters("Role", "Must be one of the followwing: " + string.Join(", ", roles));
			}

			User result = new()
			{
				Login = dto.Login,
				Role = dto.Role,
				IsEnabled = true
			};

			_dbContext.Users.Add(result);

			try
			{
				await _dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return Result<DtoUserResult>.Conflict("Login", "User with this login already exists");

				throw;
			}


			return Result<DtoUserResult>.Success(new(result, false));
		}

		public async Task<Result<DtoUserResult>> UpdateUser(int id, DtoUpdateUser dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageUser");

			if (!authResult.Succeeded)
				return Result<DtoUserResult>.Forbidden();

			var result = await _dbContext.Users.FindAsync(id);

			if (result == null)
				return Result<DtoUserResult>.NotFound();

			var roles = UserRoles(user);

			if (!roles.Contains(dto.Role))
				return Result<DtoUserResult>.BadParameters("Role", "Must be one of the followwing: " + string.Join(", ", roles));

			if (result.Role == _policies.Roles[0] && (result.Role != dto.Role || !dto.IsEnabled))
			{
				var n = await _dbContext.Users.CountAsync(u => u.Role == _policies.Roles[0] && u.IsEnabled);

				if (n < 2)
				{
					Dictionary<string, string[]> errors = [];

					if (!dto.IsEnabled)
						errors.Add("IsEnabled", ["Can't be disabled"]);

					if (result.Role != dto.Role)
						errors.Add("Role", ["Can't be changed"]);

					return Result<DtoUserResult>.BadParameters(errors);
				}
			}

			result.Role = dto.Role;
			result.Name = dto.Name;
			result.Email = dto.Email;
			result.IsEnabled = dto.IsEnabled;
			result.Locale = dto.Locale;

			if (dto.ResetApiKey)
				result.ApiKey = RandomString.Create(32);

			authResult = await _authService.AuthorizeAsync(user, id, "IsAdmin");

			if (authResult.Succeeded)
				result.IsDemo = false;

			await _dbContext.SaveChangesAsync();

			return Result<DtoUserResult>.Success(new(result, false));
		}

		public async Task<Result<DtoDeleteUserResult>> DeleteUser(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageUser");

			if (!authResult.Succeeded)
				return Result<DtoDeleteUserResult>.Forbidden();

			var u = await _dbContext.Users.FindAsync(id);

			if (u == null)
				return Result<DtoDeleteUserResult>.NotFound();

			if (u.Role == _policies.Roles[0])
			{
				var n = await _dbContext.Users.CountAsync(u => u.Role == _policies.Roles[0] && u.IsEnabled);

				if (n < 2)
					return Result<DtoDeleteUserResult>.BadParameters("Id", "Can't be deleted");
			}

			_dbContext.Users.Remove(u);

			await _dbContext.SaveChangesAsync();

			return Result<DtoDeleteUserResult>.Success(
				new()
				{
					Signout = u.Login == user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
				});
		}

	}
}