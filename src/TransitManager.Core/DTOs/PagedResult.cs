using System;
using System.Collections.Generic;

namespace TransitManager.Core.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; } // Changed from PageNumber to match other code (check usages)
        public int PageSize { get; set; }
        public int TotalPages { get; set; } // Often better to have a setter for serialization or calculation
    }
}