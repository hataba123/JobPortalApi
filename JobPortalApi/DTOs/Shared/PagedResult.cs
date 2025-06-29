namespace JobPortalApi.DTOs.Shared
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public bool HasNext => PageNumber * PageSize < TotalItems;
        public bool HasPrevious => PageNumber > 1;

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
