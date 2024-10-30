using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public interface IUserManagementService
	{
		bool NoUsers();
		Task<DtoUserLiteResult[]> GetList(ClaimsPrincipal user);
		Task<Result<DtoUserResult>> GetById(int id, ClaimsPrincipal user);
		Task<Result<DtoUserResult>> GetByApiKey(string apikey);
		string[] UserRoles(ClaimsPrincipal user);
		Task<Result<DtoUserResult>> CreateUser(DtoCreateUser dto, ClaimsPrincipal user);
		Task<Result<DtoUserResult>> UpdateUser(int id, DtoUpdateUser dto, ClaimsPrincipal user);
		Task<Result<DtoDeleteUserResult>> DeleteUser(int id, ClaimsPrincipal user);
	}
}