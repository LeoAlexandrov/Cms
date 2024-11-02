using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public interface ISchemaManagementService
	{
		Task<DtoSchemaResult[]> Schemata();
		Task<DtoSchemaResult> GetSchema(int id);
		Task<Result<DtoSchemaResult>> CreateSchema(DtoCreateSchema dto, ClaimsPrincipal user);
		Task<Result<DtoSchemaResult>> UpdateSchema(int id, DtoUpdateSchema dto, FragmentSchemaService fss, ClaimsPrincipal user);
		Task<Result<bool>> DeleteSchema(int id, FragmentSchemaService fss, ClaimsPrincipal user);
		Task<Result<bool>> CompileAndReload(FragmentSchemaService fss, ClaimsPrincipal user);
	}
}