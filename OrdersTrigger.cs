using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using System.Net;
using Contoso.Movies;


namespace Contoso.Movies
{
    public static class OrdersTrigger
    {
        [FunctionName("OrdersTrigger")]
        public static async Task Run([CosmosDBTrigger(
            databaseName: "MoviesDB",
            collectionName: "Orders",
            ConnectionStringSetting = "cosmosdbohteam5_DOCUMENTDB",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, 
             [CosmosDB(
                databaseName: "MoviesDB",
                collectionName: "Item",
                ConnectionStringSetting = "cosmosdbohteam5_DOCUMENTDB"
            )] DocumentClient client,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);

                var p = new OrderProcessor(client, log);

                foreach (var d in input){

                    var order = Orders.FromDocument(d);

                    var tasks = new List<Task>();

                    tasks.Add(p.updateItemsWithOrder(order)); 

                    await Task.WhenAll(tasks);
                }




            }
        }
    }
}
