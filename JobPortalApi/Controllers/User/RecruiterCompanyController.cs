using JobPortalApi.DTOs.AdminCompany;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortalApi.Controllers.User
{
    [ApiController]
    [Route("api/recruiter/company")]
    [Authorize(Roles = "Recruiter")]
    public class RecruiterCompanyController : Controller
    {
        private readonly IRecruiterCompanyService _companyService;

        public RecruiterCompanyController(IRecruiterCompanyService companyService)
        {
            _companyService = companyService;
        }

        // GET: api/recruiter/company
        [HttpGet]
        public async Task<IActionResult> GetMyCompany()
        {
            var recruiterId = GetCurrentUserId();
            var company = await _companyService.GetMyCompanyAsync(recruiterId);

            if (company == null)
                return NotFound("Không tìm thấy công ty mà bạn đang quản lý.");

            return Ok(company);
        }

        // PUT: api/recruiter/company
        [HttpPut]
        public async Task<IActionResult> UpdateMyCompany([FromBody] UpdateCompanyDto dto)
        {
            var recruiterId = GetCurrentUserId();
            var success = await _companyService.UpdateMyCompanyAsync(recruiterId, dto);

            if (!success)
                return NotFound("Không thể cập nhật vì không tìm thấy công ty phù hợp hoặc bạn không có quyền.");

            return NoContent(); // 204
        }

        // DELETE: api/recruiter/company
        [HttpDelete]
        public async Task<IActionResult> DeleteMyCompany()
        {
            var recruiterId = GetCurrentUserId();
            var success = await _companyService.DeleteMyCompanyAsync(recruiterId);

            if (!success)
                return BadRequest("Không thể xoá công ty (có thể do còn bài đăng tuyển dụng hoặc bạn không có quyền).");

            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(User.FindFirst("sub")!.Value); // lấy ID từ JWT token
        }
    }
}
