namespace Icebreaker.Helpers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Provides extension methods for interacting with table storage provider
    /// </summary>
    public static class TableStorageExtensions
    {
        /// <summary>
        /// Queries a single item based on partition key and row key.
        /// </summary>
        /// <typeparam name="TOutput">Type of output object</typeparam>
        /// <param name="table">Table instance to run against</param>
        /// <param name="partitionKey">Partition key of the object</param>
        /// <param name="rowKey">Row key of the object</param>
        /// <returns>Instance of <typeparamref name="TOutput"/> or default</returns>
        public static async Task<TOutput> QuerySingleItemAsync<TOutput>(this CloudTable table, string partitionKey, string rowKey)
            where TOutput : ITableEntity, new()
        {
            TableQuery<TOutput> teamQuery = new TableQuery<TOutput>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, rowKey))).Take(1);
            var output = new TOutput();
            TableContinuationToken continuationToken = null;
            do
            {
                var results = await table.ExecuteQuerySegmentedAsync(teamQuery, continuationToken);

                output = results.FirstOrDefault();
                continuationToken = results.ContinuationToken;
            }
            while (continuationToken != null);
            return output;
        }
    }
}