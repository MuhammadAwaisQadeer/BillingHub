namespace Client_Invoice_System.Helpers
{
    public class PaginationService<T> : IPaginationService<T> where T : class
    {
        private int _pageSize = 10;

        public async Task<(List<T> PagedData, int TotalCount, int TotalPages)> GetPagedDataAsync(IQueryable<T> data, int currentPage)
        {
            var totalRecords = data.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / _pageSize);
            var pagedData = data.Skip((currentPage - 1) * _pageSize)
                                .Take(_pageSize)
                                .ToList();

            return (pagedData, totalRecords, totalPages);
        }

        public void SetPageSize(int pageSize)
        {
            _pageSize = pageSize;
        }
    }
}