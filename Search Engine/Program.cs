using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Search_Engine
{
    class Program
    {
        static void Main(string[] args)
        {
            //Dictionary<string, string> articles = Search
        }

        public async Task<Dictionary<string, string>> Search(string searchTerm)
        {
            WebRequest request = WebRequest.Create("https://www.altmetric.com/explorer.login");
            WebResponse response = await request.GetResponseAsync();
            //Match matchObject = Regex.Match()
            Dictionary<string, string> dict = new Dictionary<string, string>();
            return dict;
        }
    }
    
}