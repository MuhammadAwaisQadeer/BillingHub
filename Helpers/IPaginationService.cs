namespace Client_Invoice_System.Helpers
{
    public interface IPaginationService<T> where T : class
    {
        void SetPageSize(int pageSize);
        Task<(List<T> PagedData, int TotalCount, int TotalPages)> GetPagedDataAsync(IQueryable<T> data, int currentPage);

    }
}