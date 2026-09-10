using JobPortalApi.DTOs.AdminCompany;
using JobPortalApi.Services.Interface.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.DTOs.Shared;

namespace JobPortalApi.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/companies")]
    public class AdminCompanyController : Controller
    {
        private readonly ICompanyService _companyService;

        public AdminCompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PagedQuery query)
            => Ok(await _companyService.GetAllCompaniesAsync(query));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var c = await _companyService.GetCompanyByIdAsync(id);
            if (c != null) Response.Headers.ETag = $"\"{c.Version}\"";
            return c == null ? NotFound() : Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto)
        {
            var newC = await _companyService.CreateCompanyAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = newC.Id }, newC);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyDto dto, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = ReadRequiredVersion(ifMatch);
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            var updated = await _companyService.UpdateCompanyAsync(id, dto, version);
            var entity = updated ? await _companyService.GetCompanyByIdAsync(id) : null;
            if (entity != null) Response.Headers.ETag = $"\"{entity.Version}\"";
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = ReadRequiredVersion(ifMatch);
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            var deleted = await _companyService.DeleteCompanyAsync(id, version);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPatch("{id}/verification")]
        public async Task<IActionResult> UpdateVerification(
            Guid id,
            [FromBody] UpdateCompanyVerificationDto dto,
            [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var version = ReadRequiredVersion(ifMatch);
            if (version == null) return StatusCode(StatusCodes.Status428PreconditionRequired);
            var company = await _companyService.UpdateVerificationStatusAsync(id, dto, version);
            if (company != null) Response.Headers.ETag = $"\"{company.Version}\"";
            return company == null ? NotFound() : Ok(company);
        }

        private static byte[]? ReadRequiredVersion(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : ConcurrencyToken.Decode(value.Trim());
    }
}
