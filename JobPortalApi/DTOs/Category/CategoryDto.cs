namespace JobPortalApi.DTOs.Category
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }
}
