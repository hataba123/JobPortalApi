using JobPortalApi.DTOs.Company;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAll()
        {
            var companies = await _companyService.GetAllAsync();
            return Ok(companies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var company = await _companyService.GetByIdAsync(id);
            return company == null ? NotFound() : Ok(company);
        }
    }
}
