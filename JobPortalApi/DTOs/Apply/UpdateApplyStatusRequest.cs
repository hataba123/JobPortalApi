namespace JobPortalApi.DTOs.Apply
{
    public class UpdateApplyStatusRequest
    {
        public string? Status { get; set; }
        public string? ToStatus { get; set; }
        public string? Reason { get; set; }

        public string RequestedStatus => ToStatus ?? Status ?? string.Empty;
    }
}
