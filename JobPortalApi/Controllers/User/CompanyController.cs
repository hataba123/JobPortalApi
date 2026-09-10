using JobPortalApi.DTOs.Company;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Controllers.User
{
    [Route("api/companies")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] PagedQuery query,
            [FromQuery] string? industry,
            [FromQuery] string? location,
            [FromQuery] string? employees)
        {
            var companies = await _companyService.GetAllPagedAsync(query, industry, location, employees);
            return Ok(companies);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var company = await _companyService.GetByIdAsync(id);
            return company == null ? NotFound() : Ok(company);
        }
    }
}
