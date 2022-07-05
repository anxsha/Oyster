using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Oyster.HelperClasses.Pagination {
/// <summary>
/// Provides a generic implementation of a paginated list.
/// Its instance stores the current index of the page,
/// the number of all pages available and elements
/// from that page.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class PaginatedList<T> : List<T>
{
  /// <summary>
  /// The current page index, elements from which are stored as items in the instance.
  /// </summary>
  public int PageIndex { get; private set; }
  /// <summary>
  /// The number of pages needed for the entire source collection
  /// with the given page size.
  /// </summary>
  public int TotalPages { get; private set; }

  public PaginatedList(IEnumerable<T> items, int count, int pageIndex, int pageSize)
  {
    PageIndex = pageIndex;
    TotalPages = (int)Math.Ceiling(count / (double)pageSize);

    this.AddRange(items);
  }
  
  public PaginatedList() {
  }

  public bool HasPreviousPage => PageIndex > 1;

  public bool HasNextPage => PageIndex < TotalPages;
/// <summary>
/// Asynchronously creates and returns a new instance of a
/// <see cref="PaginatedList{T}"/>.
/// 
/// </summary>
/// <param name="source">The whole collection used for pagination.</param>
/// <param name="pageIndex">Index of the page of the elements to be obtained.</param>
/// <param name="pageSize">Number of items stored in one page.</param>
/// <returns></returns>
  public static async Task<PaginatedList<T>> CreateAsync(
    IQueryable<T> source, int pageIndex, int pageSize)
  {
    var count = await source.CountAsync();
    var items = await source.Skip(
        (pageIndex - 1) * pageSize)
      .Take(pageSize).ToListAsync();
    return new PaginatedList<T>(items, count, pageIndex, pageSize);
  }
}
}
