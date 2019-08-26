using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using LanguageDetection;

namespace Search_Engine
{

    class Program
    {

        static void Main(string[] args)
        {
            string searchStr = Console.ReadLine(); 
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<Words> news = Words.Search(searchStr).Result;
            stopwatch.Stop();
            TimeSpan interval = stopwatch.Elapsed;
            stopwatch.Reset();
            for (int i = 0; i < news.Count; i++)
            {
                Console.WriteLine(news[i].title + " " + news[i].url);
            }
            Console.WriteLine(interval);
        }
    }
    
    class Words
    {
        public string title { get; set; }
        public string url { get; set; }
        public int score { get; set; }
        public WordType type { get; set; }
        
        
        

        public enum WordType
        {
            Article,
            Video,
            Blog
        }

        public Words(string newsTitle, string newsUrl, int newsScore, WordType wordType)
        {
            title = newsTitle;
            url = newsUrl;
            score = newsScore;
            type = wordType;
        }

        public static async Task<List<Words>> Search(string searchTerm)
        {
            LanguageDetector detector = new LanguageDetector();
            detector.AddAllLanguages();
            
            HttpClient client = new HttpClient();
            var loginPage = await client.GetStringAsync("https://www.altmetric.com/explorer/login");
        
            Match matchObject = Regex.Match(loginPage, @"name=""authenticity_token"" value=""(?<key>.+)""");
            string token = string.Empty;
            if (matchObject.Success) token = matchObject.Groups["key"].Value;
        
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
            
            Console.WriteLine("A");
        
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            dynamic papersDict = serializer.DeserializeObject(searchResults);
        
            List<Words> newsList = new List<Words>();
        
            List<Task<string>> taskList = new List<Task<string>>();
            List<int> scoreList = new List<int>();

            if (papersDict["outputs"].Length == 0)
            {
                return newsList;
            }
            
            Console.WriteLine("B");
            
            for (int i = 0; i < Math.Min(10, papersDict["outputs"].Length); i++)
            {
                string altId = papersDict["outputs"][i]["id"].ToString();
                int score = papersDict["outputs"][i]["score"];
                scoreList.Add(score);
                taskList.Add(client.GetStringAsync("https://api.altmetric.com/v1/fetch/id/" + altId + "?key=ef2e9b9961415ba4b6510ec82c3e9cba"));
            }
        
            int counter = 0;
            
            while (taskList.Count > 0)
            {
                Console.WriteLine(counter);
                Task<string> firstFinishedTask = await Task.WhenAny(taskList);
                taskList.Remove(firstFinishedTask);
                string detailsText = await firstFinishedTask;
                
                dynamic details = serializer.DeserializeObject(detailsText);

                if (details["posts"].ContainsKey("news") && details["posts"]["news"].Length > 0)
                {
                    for (int j = 0; j < Math.Min(3, details["posts"]["news"].Length); j++)
                    {
                        if (details["posts"]["news"][j].ContainsKey("title") &&
                            details["posts"]["news"][j].ContainsKey("url"))
                        {
                            string title = details["posts"]["news"][j]["title"];

                            if (detector.Detect(title) == "en" && details["posts"]["news"][j]["url"] != null)
                            {
                                var request = new HttpRequestMessage(HttpMethod.Head, details["posts"]["news"][j]["url"]);
                                try
                                {
                                    var validityResponse = await client.SendAsync(request);
                                    if (validityResponse.IsSuccessStatusCode)
                                    {
                                        Words newsArticle = new Words(title, details["posts"]["news"][j]["url"], scoreList[counter], WordType.Article);
                                        newsList.Add(newsArticle);
                                    }
                                }
                                catch (HttpRequestException e)
                                {
                                    
                                }
                            }
                        }
                    }
                }

                if (details["posts"].ContainsKey("blogs") && details["posts"]["blogs"].Length > 0)
                {
                    string title = details["posts"]["blogs"][0]["title"];

                    if (detector.Detect(title) == "en" && details["posts"]["blogs"][0]["url"] != null)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Head, details["posts"]["blogs"][0]["url"]);
                        try
                        {
                            var validityResponse = await client.SendAsync(request);
                            if (validityResponse.IsSuccessStatusCode)
                            {
                                Words blogPost = new Words(title, details["posts"]["blogs"][0]["url"], scoreList[counter], WordType.Blog);
                                newsList.Add(blogPost);
                            }
                        }
                        catch (HttpRequestException e)
                        {
                            
                        }
                    }
                }

                if (details["posts"].ContainsKey("video") && details["posts"]["video"].Length > 0)
                {
                    string title = details["posts"]["video"][0]["title"];

                    if (detector.Detect(title) == "en" && details["posts"]["video"][0]["url"] != null)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Head, details["posts"]["video"][0]["url"]);
                        try
                        {
                            var validityResponse = await client.SendAsync(request);
                            if (validityResponse.IsSuccessStatusCode)
                            {
                                Words video = new Words(title, details["posts"]["video"][0]["url"], scoreList[counter], WordType.Video);
                                newsList.Add(video);
                            }
                        }
                        catch (HttpRequestException e)
                        {
                            
                        } 
                    }
                }
                counter++;
            }
        
            client.Dispose();
        
            return newsList;
        }
    }
    
}