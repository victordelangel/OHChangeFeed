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

namespace Contoso.Movies{


    public class OrderProcessor{
        
        private DocumentClient _client;
        private Uri _collectionUri;
        private ILogger _log;

        private string _databaseName = "MoviesDB";
        private string _collectionName = "Item";


        public OrderProcessor(DocumentClient client, ILogger log)
        {
            _log = log;
            _client = client;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName);
        }



        public async Task updateItemsWithOrder(Orders order){
            
            _log.LogInformation("Updating Item Collection with order : " + order.id);


            List<OrderDetail> orderDetails = order.OrderDetails;

            foreach(OrderDetail detail in orderDetails){
                
                string ItemId = detail.ProductId;

                _log.LogInformation("Creating request Option for ItemId" + ItemId);
                var optionsSingle = new RequestOptions() { PartitionKey = new PartitionKey(int.Parse(ItemId)) };

                int attempts = 0;

                while (attempts < 10){

                    _log.LogInformation("attenting to save: "+ attempts + " for ItemId:"+ ItemId);

                    Item item = null;

                    try{

                         var uriSingle = UriFactory.CreateDocumentUri(_databaseName, _collectionName, ItemId);

                        _log.LogInformation($"Uri for Item: {uriSingle.ToString()}");

                        item = await _client.ReadDocumentAsync<Item>(uriSingle, optionsSingle);     

                        _log.LogInformation("item is " + item);

                        if(item == null){

                            _log.LogInformation("Item with id "+ ItemId +" not found");
                        }

                        if(item.ItemAggregate == null){

                            _log.LogInformation("Item "+ItemId+" doesn't have ItemAggregates");

                        } else {

                            ItemAggregate ia = item.ItemAggregate.ElementAt(0);                            

                            ia.BuyCount += int.Parse(detail.Quantity);

                            item.ItemAggregate.RemoveAt(0);

                            item.ItemAggregate.Add(ia);

                            _log.LogInformation ("New buyCount for ItemId: "+ ia.BuyCount);



                        }

                    } catch (DocumentClientException ex) {
                    
                        _log.LogInformation("Error reading Item: " + ItemId + " ex: "+ ex.ToString());

                        if (ex.StatusCode != HttpStatusCode.NotFound)
                            throw ex;
                        }

                    

                    try {
                        _log.LogInformation("Trying to save Item "+ ItemId);
                       
                        await UpsertDocument(item, optionsSingle);

                        _log.LogInformation("New Item saved "+ item.ToString());
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

            } 


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