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
using Microsoft.AspNetCore.Mvc.ApplicationModels;


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

		public async Task<GetUserResult> GetById(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageUser");

			if (!authResult.Succeeded)
				return GetUserResult.AccessForbidden();

			var u = await _dbContext.Users.FindAsync(id);

			if (u == null)
				return GetUserResult.UserNotFound();

			return GetUserResult.Success(new(u, u.Login == user.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value));
		}

		public async Task<GetUserResult> GetByApiKey(string apikey)
		{
			var u = await _dbContext.Users.FirstOrDefaultAsync(u => u.ApiKey == apikey);

			if (u == null)
				return GetUserResult.UserNotFound();

			return GetUserResult.Success(new(u, false));
		}

		public string[] UserRoles(ClaimsPrincipal user)
		{
			string role = user.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
			int idx = Array.IndexOf(this._policies.Roles, role);

			return idx >= 0 ? this._policies.Roles.Skip(idx).ToArray() : [];
		}

		public async Task<CreateUserResult> CreateUser(DtoCreateUser dto, ClaimsPrincipal user)
		{
			if (user != null)
			{
				var authResult = await _authService.AuthorizeAsync(user, "IsAdmin");

				if (!authResult.Succeeded)
					return CreateUserResult.AccessForbidden();

				var roles = UserRoles(user);

				if (!roles.Contains(dto.Role))
					return CreateUserResult.BadUserParameters(ModelErrors.Create("Role", "Must be one of the followwing: " + string.Join(", ", roles)));
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
				await dbContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
					return CreateUserResult.UserConflict(ModelErrors.Create("Login", "User with this login already exists"));

				throw;
			}


			return CreateUserResult.Success(new(result, false));
		}

		public async Task<UpdateUserResult> UpdateUser(int id, DtoUpdateUser dto, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageUser");

			if (!authResult.Succeeded)
				return UpdateUserResult.AccessForbidden();

			var result = await _dbContext.Users.FindAsync(id);

			if (result == null)
				return UpdateUserResult.UserNotFound();

			var roles = UserRoles(user);

			if (!roles.Contains(dto.Role))
				return UpdateUserResult.BadUserParameters(ModelErrors.Create("Role", "Must be one of the followwing: " + string.Join(", ", roles)));

			if (result.Role == _policies.Roles[0] && (result.Role != dto.Role || !dto.IsEnabled))
			{
				var n = await dbContext.Users.CountAsync(u => u.Role == _policies.Roles[0] && u.IsEnabled);

				if (n < 2)
				{
					ModelErrors errors = [];

					if (!dto.IsEnabled)
						errors.Add("IsEnabled", "Can't be disabled.");

					if (result.Role != dto.Role)
						errors.Add("Role", "Can't be changed.");

					return UpdateUserResult.BadUserParameters(errors);
				}
			}

			result.Role = dto.Role;
			result.Name = dto.Name;
			result.Email = dto.Email;
			result.IsEnabled = dto.IsEnabled;

			if (dto.ResetApiKey)
				result.ApiKey = RandomString.Create(32);

			authResult = await _authService.AuthorizeAsync(user, id, "IsAdmin");

			if (authResult.Succeeded)
				result.IsDemo = false;

			await dbContext.SaveChangesAsync();

			return UpdateUserResult.Success(new(result, false));
		}

		public async Task<DeleteUserResult> DeleteUser(int id, ClaimsPrincipal user)
		{
			var authResult = await _authService.AuthorizeAsync(user, id, "CanManageUser");

			if (!authResult.Succeeded)
				return DeleteUserResult.AccessForbidden();

			var u = await _dbContext.Users.FindAsync(id);

			if (u == null)
				return DeleteUserResult.UserNotFound();

			if (u.Role == _policies.Roles[0])
			{
				var n = await dbContext.Users.CountAsync(u => u.Role == _policies.Roles[0] && u.IsEnabled);

				if (n < 2)
					return DeleteUserResult.BadUserParameters(ModelErrors.Create("Id", "Can't be deleted"));
			}

			dbContext.Users.Remove(u);

			await dbContext.SaveChangesAsync();

			return DeleteUserResult.Success(u.Login == user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
		}

	}
}