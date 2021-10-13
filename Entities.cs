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

namespace Contoso.Movies{

    public class OrderDetail
    {
        [JsonProperty("OrderDetailId")]
        public string OrderDetailId { get; set; }
        [JsonProperty("ProductId")]
        public string ProductId { get; set; }
        [JsonProperty("UnitPrice")]
        public string UnitPrice { get; set; }
        [JsonProperty("Quantity")]
        public string Quantity { get; set; }
        [JsonProperty("Email")]
        public string Email { get; set; }
    }

    public class Orders
    {

        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("OrderId")]
        public string OrderId { get; set; }
        [JsonProperty("OrderDate")]
        public string OrderDate { get; set; }
        [JsonProperty("FirstName")]
        public string FirstName { get; set; }
        [JsonProperty("LastName")]
        public string LastName { get; set; }
        [JsonProperty("Address")]
        public string Address { get; set; }
        [JsonProperty("City")]
        public string City { get; set; }
        [JsonProperty("State")]
        public string State { get; set; }
        [JsonProperty("PostalCode")]
        public string PostalCode { get; set; }
        [JsonProperty("Country")]
        public string Country { get; set; }
        [JsonProperty("Phone")]
        public string Phone { get; set; }
        [JsonProperty("Total")]
        public string Total { get; set; }
        [JsonProperty("SMSOptIn")]
        public string SMSOptIn { get; set; }
        [JsonProperty("PaymentTransactionId")]
        public string PaymentTransactionId { get; set; }
        [JsonProperty("HasBeenShipped")]
        public string HasBeenShipped { get; set; }
        [JsonProperty("OrderDetails")]
        public List<OrderDetail> OrderDetails { get; set; }

        public static Orders FromDocument(Document document){

            var result = new Orders(){

                id = document.GetPropertyValue<string>("id"),
                OrderId = document.GetPropertyValue<string>("OrderId"),
                OrderDate = document.GetPropertyValue<string>("OrderDate"),
                FirstName = document.GetPropertyValue<string>("FirstName"),
                LastName = document.GetPropertyValue<string>("LastName"),
                Address = document.GetPropertyValue <string>("Address"),
                City = document.GetPropertyValue<string>("City"),
                State = document.GetPropertyValue<string>("State"),
                PostalCode = document.GetPropertyValue<string>("PostalCode"),
                Country = document.GetPropertyValue<string>("Country"),
                Phone = document.GetPropertyValue<string>("Phone"),
                Total = document.GetPropertyValue<string>("Total"),
                SMSOptIn = document.GetPropertyValue<string>("SMSOptIn"),
                PaymentTransactionId = document.GetPropertyValue<string>("PaymentTransactionId"),
                HasBeenShipped =  document.GetPropertyValue<string>("HasBeenShipped"),
                OrderDetails =  document.GetPropertyValue<List<OrderDetail>>("OrderDetails")     
                
            };

            return result;

        }
       
    }


    public class ItemAggregate{

        [JsonProperty("id")]
        public string id;
        [JsonProperty("BuyCount")]
        public int BuyCount;
        [JsonProperty("ViewDetailsCount")]
        public int ViewDetailsCount;
        [JsonProperty("AddToCartCount")]
        public int AddToCartCount;
        [JsonProperty("VoteCount")]
        public int VoteCount;

      
    }

    public class Item{
        [JsonProperty("id")]
        public string id;
        [JsonProperty("ItemId")]
        public int ItemId;
        [JsonProperty("VoteCount")]
        public int VoteCount;
        [JsonProperty("ProductName")]
        public string ProductName;
        [JsonProperty("Imdbid")]
        public int Imdbid;
        [JsonProperty("Description")]
        public string Description;
        [JsonProperty("ImagePath")]
        public string Imagepath;
        [JsonProperty("UnitPrice")]
        public double UnitPrice;
        [JsonProperty("CategoryId")]
        public int CategoryId;
        [JsonProperty("Popularity")]
        public double Popularity;
        [JsonProperty("OriginalLanguage")]
        public string OriginalLanguage;
        [JsonProperty("ReleaseDate")]
        public string ReleaseDate;
        [JsonProperty("VoteAverage")]
        public string VoteAverage;
        [JsonProperty("ItemAggregate")]
        public List<ItemAggregate> ItemAggregate;
        [JsonProperty("_rid")]
        public string _rid;
        [JsonProperty("_self")]
        public string _self;
        [JsonProperty("_etag")]
        public string _etag;
        [JsonProperty("attachments")]
        public string _attachments;
        [JsonProperty("_ts")]
        public int _ts;


        public int getBuyCount(){

            int result = 0;

            if(ItemAggregate.Count > 0){

               return ItemAggregate.ElementAt<ItemAggregate>(0).BuyCount;

            }

            return result;

        }


        public static Item FromDocument(Document document){

            var result = new Item(){

                id = document.GetPropertyValue<string>("id"),
                ItemId = document.GetPropertyValue<int>("ItemId"),
                VoteCount = document.GetPropertyValue<int>("VoteCount"),
                ProductName = document.GetPropertyValue<string>("ProductName"),
                Imdbid = document.GetPropertyValue<int>("Imdbid"),
                Description = document.GetPropertyValue <string>("Description"),
                Imagepath = document.GetPropertyValue<string>("ImagePath"),
                UnitPrice = document.GetPropertyValue<double>("UnitPrice"),
                CategoryId = document.GetPropertyValue<int>("CategoryId"),
                Popularity = document.GetPropertyValue<double>("Popularity"),
                OriginalLanguage = document.GetPropertyValue<string>("OriginalLanguage"),
                ReleaseDate = document.GetPropertyValue<string>("ReleaseDate"),
                VoteAverage = document.GetPropertyValue<string>("VoteAverage"),
                ItemAggregate = document.GetPropertyValue<List<ItemAggregate>>("ItemAggregate"),
                _rid =  document.GetPropertyValue<string>("_rid"),
                _self =  document.GetPropertyValue<string>("_self"),
                _etag =  document.GetPropertyValue<string>("_etag"),
                _attachments =  document.GetPropertyValue<string>("_attachments"),
                _ts =  document.GetPropertyValue<int>("_ts")      
                
                
            };

            return result;
        }

    }

    public class Top10ItemsView {

        [JsonProperty("Top10Items") ]
        public List<Item> top10;

        [JsonProperty("ItemId") ]
        public string itemId;

        [JsonProperty("id") ]
        public string id;

         [JsonProperty("lastUpdate")]
        public string TimeStamp;

        public void sortTop10(){


            if(top10 != null){

                top10.Sort((l1,l2) => l2.getBuyCount().CompareTo(l1.getBuyCount()));

                while(top10.Count > 10){

                    top10.RemoveAt(top10.Count -1);
                } 
            }          

        }

        public bool updateIfExist(Item item){

            if(top10 == null) {
                top10 = new List<Item>(); 
                return false;
            }

            foreach(Item i in top10){

                if(i.ItemId == item.ItemId){
                    
                    top10.Remove(i);
                    top10.Add(item);
                    return true;
                }

            }

            return false;

        }

        public void addIfPopular(Item item){

            if(updateIfExist(item)){

                sortTop10();
                return;
            }
           

            var top10Size = top10.Count; 


            if(top10Size < 10) {
                
                top10.Add(item);            
                
            }

            else if (top10Size >= 10){

                if(item.getBuyCount() > top10.ElementAt(top10Size -1).getBuyCount()){

                    top10.Add(item);
                   
                }
            } else {
                top10.Add(item);
            }

             sortTop10();

        }

        public string ToString(){

            string result = "\n Top10: \n";

            result = result + "Size: "+top10.Count+"\n";

            if(top10 != null){

                foreach( var item in top10){

                    result = result + "ProductName: "+ item.ProductName + " Popularity:"+item.Popularity+"\n";

                }
                
            }

            return result;

        }


    }

}