using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Search_Engine
{

    struct NewsStruct
    {
        public string title;
        public string url;
        public int score;

        public NewsStruct(string newsTitle, string newsUrl, int newsScore)
        {
            title = newsTitle;
            url = newsUrl;
            score = newsScore;
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            List<NewsStruct> news = Search("cancer").Result;
            Console.WriteLine(news[0].title);
            Console.WriteLine();
        }

        private static async Task<List<NewsStruct>> Search(string searchTerm)
        {
            HttpClient client = new HttpClient();
            var loginPage = await client.GetStringAsync("https://www.altmetric.com/explorer/login");
            
            Match matchObject = Regex.Match(loginPage, @"name=""authenticity_token"" value=""(?<key>.+)""");
            string token = string.Empty;
            if(matchObject.Success) token = matchObject.Groups["key"].Value;
            
            Dictionary<string, string> formFields = new Dictionary<string, string>()
            {
                {"email", "bigdata@stemfellowship.org"},
                {"password", "bigdatachallenge"},
                {"authenticity_token", token},
                {"commit", "Sign in"}
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(formFields);
            var response = await client.PostAsync("https://www.altmetric.com/explorer/login", content);
            var searchResults =
                await client.GetStringAsync("https://www.altmetric.com/explorer/json_data/research_outputs?q=" + searchTerm +
                                      "&scope=all");
            
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            dynamic papersDict = serializer.DeserializeObject(searchResults);

            List<NewsStruct> newsList = new List<NewsStruct>();

            for (int i = 0; i < 5; i++)
            {
                string altId = papersDict["outputs"][i]["id"].ToString();
                int score = papersDict["outputs"][i]["score"];
                var detailsText = await client.GetStringAsync("https://api.altmetric.com/v1/fetch/id/" + altId + "?key=1e25ee802e58e41ec820679c9ff92b09");
                dynamic details = serializer.DeserializeObject(detailsText);

                if (details["posts"].ContainsKey("news"))
                {
                    for (int j = 0; j < Math.Min(3, details["posts"]["news"].Length); j++)
                    {
                        NewsStruct newsArticle = new NewsStruct(details["posts"]["news"][j]["title"], details["posts"]["news"][j]["url"], score);
                        newsList.Add(newsArticle);
                    }
                }
            }
            
            client.Dispose();

            return newsList;
        }
    }
    
}