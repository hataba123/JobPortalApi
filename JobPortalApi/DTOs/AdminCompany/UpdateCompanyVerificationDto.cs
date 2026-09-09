using System.ComponentModel.DataAnnotations;
using JobPortalApi.Models.Enums;

namespace JobPortalApi.DTOs.AdminCompany
{
    public class UpdateCompanyVerificationDto
    {
        [Required]
        public CompanyVerificationStatus VerificationStatus { get; set; }
    }
}
