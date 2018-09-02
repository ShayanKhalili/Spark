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

    struct Words
    {
        public string title;
        public string url;
        public int score;

        public Words(string newsTitle, string newsUrl, int newsScore)
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
            List<Words> news = Search("cancer").Result;
            Console.WriteLine(news[0].title);
            Console.WriteLine();
        }

        private static async Task<List<Words>> Search(string searchTerm)
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

            List<Words> newsList = new List<Words>();

            for (int i = 0; i < 3; i++)
            {
                string altId = papersDict["outputs"][i]["id"].ToString();
                int score = papersDict["outputs"][i]["score"];
                var detailsText = await client.GetStringAsync("https://api.altmetric.com/v1/fetch/id/" + altId + "?key=1e25ee802e58e41ec820679c9ff92b09");
                dynamic details = serializer.DeserializeObject(detailsText);

                if (details["posts"].ContainsKey("news"))
                {
                    for (int j = 0; j < Math.Min(3, details["posts"]["news"].Length); j++)
                    {
                        Words newsArticle = new Words(details["posts"]["news"][j]["title"], details["posts"]["news"][j]["url"], score);
                        newsList.Add(newsArticle);
                    }
                }

                if (details["posts"].ContainsKey("blogs"))
                {
                    Words blogPost = new Words(details["posts"]["blogs"][0]["title"], details["posts"]["blogs"][0]["url"], score);
                    newsList.Add(blogPost);
                }

                if (details["posts"].ContainsKey("video"))
                {
                    Words video = new Words(details["posts"]["video"][0]["title"], details["posts"]["video"][0]["url"], score);
                    newsList.Add(video);
                }
            }
            
            client.Dispose();

            return newsList;
        }
    }
    
}