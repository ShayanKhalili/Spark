using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Search_Engine
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, string> articles = Search("cancer").Result;
            
        }

        private static async Task<Dictionary<string, string>> Search(string searchTerm)
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
            
            Console.WriteLine(searchResults);
            
            client.Dispose();
            
            Dictionary<string, string> dict = new Dictionary<string, string>();
            return dict;
        }
    }
    
}