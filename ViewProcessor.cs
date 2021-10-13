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
    public class ViewProcessor
    {
        private DocumentClient _client;
        private Uri _collectionUri;
        private ILogger _log;

        private string _databaseName = "MoviesDB";
        private string _collectionName = "Popularity";


        public ViewProcessor(DocumentClient client, ILogger log)
        {
            _log = log;
            _client = client;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName);
        }

        public async Task updateTop10View(Item item)
        {
            _log.LogInformation("Updating Top10 view");

            Top10ItemsView top10 = null;

            _log.LogInformation("Creating request Option");
            var optionsAll = new RequestOptions() { PartitionKey = new PartitionKey("top10") };

            int attempts = 0;

            while (attempts < 10)
            {   
                _log.LogInformation("attenting to save: "+ attempts);

                try
                {
                    var uriAll = UriFactory.CreateDocumentUri(_databaseName, _collectionName, "top10");

                    _log.LogInformation($"Materialized view: {uriAll.ToString()}");

                    top10 = await _client.ReadDocumentAsync<Top10ItemsView>(uriAll, optionsAll);     

                     _log.LogInformation("top10: " + top10.ToString());

                }
                catch (DocumentClientException ex)
                {
                    if (ex.StatusCode != HttpStatusCode.NotFound)
                        throw ex;
                }

                if (top10 == null)
                {
                    _log.LogInformation("top10:  is null");
                    top10 = new Top10ItemsView();
                    top10.itemId = "top10";
                    top10.id = "top10";
                    top10.top10 = new List<Item>();
                }

                 _log.LogInformation("step 1 ");
                top10.addIfPopular(item);
                top10.TimeStamp =  DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK");
            
                _log.LogInformation("step 2 ");
                try 
                {
                    _log.LogInformation("step3 ");
                    await UpsertDocument(top10, optionsAll);

                    _log.LogInformation("New Top10: "+ top10.ToString());
                    return;
                }
                catch (DocumentClientException de) 
                {
                    if (de.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        attempts += 1;
                        _log.LogWarning($"Optimistic concurrency pre-condition check failed. Trying again ({attempts}/10)");                        
                    }
                    else
                    {
                        throw;
                    }
                }     
                   
            }

            throw new ApplicationException("Could not insert document after retring 10 times, due to concurrency violations");
        }


        private async Task<ResourceResponse<Document>> UpsertDocument(object document, RequestOptions options)
        {
            int attempts = 0;

            while (attempts < 3)
            {
                try
                {
                    var result = await _client.UpsertDocumentAsync(_collectionUri, document, options);                      
                    _log.LogInformation($"{options.PartitionKey} RU Used: {result.RequestCharge:0.0}");
                    return result;                                  
                }
                catch (DocumentClientException de)
                {
                    if (de.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _log.LogWarning($"Waiting for {de.RetryAfter} msec...");
                        await Task.Delay(de.RetryAfter);
                        attempts += 1;
                    }
                    else
                    {
                        throw;
                    }
                }
            }            

            throw new ApplicationException("Could not insert document after being throttled 3 times");
        }
    }
}