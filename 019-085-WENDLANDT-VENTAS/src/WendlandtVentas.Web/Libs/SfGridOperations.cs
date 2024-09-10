using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Monobits.SharedKernel;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Infrastructure.Data;

namespace WendlandtVentas.Web.Libs
{
    public class SfGridOperations
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _dbContext;

        public SfGridOperations(IMapper mapper, AppDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Filtrado de un listado común
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataSource"></param>
        /// <param name="dm"></param>
        /// <returns></returns>
        public (int Count, List<T> DataResult) FilterDataSource<T>(IEnumerable<T> dataSource, DataManagerRequest dm)
        {
            var operation = new DataOperations();
            dataSource = dataSource.ToList();

            if (dm.Search != null && dm.Search.Any())
                dataSource = operation.PerformSearching(dataSource, dm.Search); //Search
            if (dm.Sorted != null && dm.Sorted.Any()) //Sorting
                dataSource = operation.PerformSorting(dataSource, dm.Sorted);
            if (dm.Where != null && dm.Where.Any()) //Filtering
                dataSource = operation.PerformFiltering(dataSource, dm.Where, dm.Where[0].Operator);

            var dataList = dataSource.ToList();
            var count = dm.RequiresCounts ? dataList.Count : -1;

            if (dm.Skip != 0) dataList = operation.PerformSkip(dataList, dm.Skip).ToList(); //Paging
            if (dm.Take != 0) dataList = operation.PerformTake(dataList, dm.Take).ToList();

            return (count, dataList);
        }

        /// <summary>
        /// Filtrado de una entidad con BaseEntity y mapeo a un view model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dm"></param>
        /// <returns></returns>
        public (int Count, IEnumerable<V> DataResult) FilterDataSource<T, V>(DataManagerRequest dm) where T : BaseEntity
        {
            var dataSource = _dbContext.Set<T>().AsQueryable();
            var operation = new QueryableOperation();
            //dataSource = operation.Execute(dataSource, dm);
            if (dm.Search != null && dm.Search.Any())
                dataSource = operation.PerformSearching(dataSource, dm.Search); //Search
            if (dm.Sorted != null && dm.Sorted.Any()) //Sorting
                dataSource = operation.PerformSorting(dataSource, dm.Sorted);
            if (dm.Where != null && dm.Where.Any()) //Filtering
                dataSource = operation.PerformFiltering(dataSource, dm.Where, dm.Where[0].Operator);

            if (dm.Skip != 0) dataSource = operation.PerformSkip(dataSource, dm.Skip); //Paging
            if (dm.Take != 0) dataSource = operation.PerformTake(dataSource, dm.Take);

            var count = dm.RequiresCounts ? dataSource.Count() : -1;

            // recordar hacer el profile del target V y si es requerido, el mapeo de sus propiedades
            var dataList = _mapper.Map<IEnumerable<T>, IEnumerable<V>>(dataSource);

            return (count, dataList);
        }

        /// <summary>
        /// Filtrado de un query. Útil para Identity users
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataSource"></param>
        /// <param name="dm"></param>
        /// <returns></returns>
        public (int Count, IQueryable<T> DataResult) FilterDataSource<T>(IQueryable<T> dataSource, DataManagerRequest dm)
        {
            var operation = new QueryableOperation();
            //dataSource = operation.Execute(dataSource, dm);
            if (dm.Search != null && dm.Search.Any())
                dataSource = operation.PerformSearching(dataSource, dm.Search); //Search
            if (dm.Sorted != null && dm.Sorted.Any()) //Sorting
                dataSource = operation.PerformSorting(dataSource, dm.Sorted);
            if (dm.Where != null && dm.Where.Any()) //Filtering
                dataSource = operation.PerformFiltering(dataSource, dm.Where, dm.Where[0].Operator);

            if (dm.Skip != 0) dataSource = operation.PerformSkip(dataSource, dm.Skip); //Paging
            if (dm.Take != 0) dataSource = operation.PerformTake(dataSource, dm.Take);

            var count = dm.RequiresCounts ? dataSource.ToList().Count() : -1;
            return (count, dataSource);
        }
    }
}