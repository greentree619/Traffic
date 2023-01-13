using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Traffic
{
    class ProxyRotator
    {
        public String NewProxyViaProxyRotatorAPI()
        {
            string proxyAddress = string.Empty;
            string jsonString = string.Empty;
#if true
            /************************************************************/
            /*  This part is needed to get the proxy from proxy rotator */
            /************************************************************/
            string serviceUri = "http://falcon.proxyrotator.com:51337/?apiKey=vZXAQdrTMBf7W32qb9NPwSnhLmJu8YKk&get=true";            

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceUri);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonString = reader.ReadToEnd();
                //the following class 'prxy' is the object from the json response from proxy rotator
                Newtonsoft.Json.Linq.JObject obj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonString);
                proxyAddress = ((JValue)obj["proxy"]).ToString();
            }
#else 
            string ipTest = "167.88.123.102";
            string apiKey = "vZXAQdrTMBf7W32qb9NPwSnhLmJu8YKk";
            string serviceUri = "http://falcon.proxyrotator.com:51337/detector/";
            string post_data = "apiKey=" + apiKey + "&ip=" + ipTest;

            // create a request
            HttpWebRequest request = (HttpWebRequest)
            WebRequest.Create(serviceUri);
            request.Method = "POST";

            // turn our request string into a byte stream
            byte[] postBytes = System.Text.Encoding.ASCII.GetBytes(post_data);

            // this is important - make sure you specify type this way
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;
            Stream requestStream = request.GetRequestStream();

            // now send it
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonString = reader.ReadToEnd();
                //the following class 'prxy' is the object from the json response from proxy rotator
                Newtonsoft.Json.Linq.JObject obj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonString);
                proxyAddress = ((JValue)obj["proxy"]).ToString();
            }
#endif

            return proxyAddress;
        }

        static public String NewProxy(int pos = 0) {
            String CurDirectory = Directory.GetCurrentDirectory();
            string[] proxyLines = File.ReadAllLines(CurDirectory + "/proxy.txt");
            string[] proxyIps = new string[proxyLines.Length];
            string[] proxyPort = new string[proxyLines.Length];
            int[] proxyPortCount = new int[proxyLines.Length];

            int TotalProxyCount = 0;
            for (int i = 0; i < proxyLines.Length; i++) {
                string[] proxyInfo = proxyLines[i].Split(':');
                proxyIps[i] = proxyInfo[0];
                
                if (!proxyInfo[1].Contains("-"))
                {
                    proxyPortCount[i] = 1;
                    proxyPort[i] = proxyInfo[1];
                    TotalProxyCount++;
                }
                else
                {
                    string[] portRange = proxyInfo[1].Split("-");
                    proxyPortCount[i] = Int32.Parse(portRange[1]) - Int32.Parse(portRange[0]) + 1;
                    proxyPort[i] = portRange[0];
                    TotalProxyCount += proxyPortCount[i];
                }
            }

            pos %= TotalProxyCount;

            int tempPos = 0;
            String ProxyInfo = "";
            for (int i = 0; i < proxyLines.Length; i++)
            {
                if ((tempPos + proxyPortCount[i]) <= pos) tempPos += proxyPortCount[i];
                else
                {
                    if (proxyPortCount[i] == 1)
                    {
                        //found
                        ProxyInfo = proxyIps[i] + ":" + proxyPort[i];
                        break;
                    }
                    else {
                        //found
                        Int32 port = Int32.Parse(proxyPort[i]) + (pos - tempPos);
                        ProxyInfo = proxyIps[i] + ":" + port;
                        break;
                    }
                }
            }
            return ProxyInfo;
        }

        static public String GetRandomAgent()
        {
            String agent = string.Empty;
            string jsonString = string.Empty;
            string serviceUri = "http://falcon.proxyrotator.com:51337/?apiKey=vZXAQdrTMBf7W32qb9NPwSnhLmJu8YKk&userAgent=true";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceUri);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonString = reader.ReadToEnd();
                //the following class 'prxy' is the object from the json response from proxy rotator
                Newtonsoft.Json.Linq.JObject obj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonString);
                agent = ((JValue)obj["randomUserAgent"]).ToString();
            }
            return agent;
        }

        static public String GetPublicAddressAPI()
        {
            String proxyAddress = string.Empty;
            string jsonString = string.Empty;
            string serviceUri = "https://api.ipify.org/?format=json";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceUri);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonString = reader.ReadToEnd();
                //the following class 'prxy' is the object from the json response from proxy rotator
                Newtonsoft.Json.Linq.JObject obj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonString);
                proxyAddress = ((JValue)obj["ip"]).ToString();
            }
            return proxyAddress;
        }

        public class _proxy {
            public string proxy { get; set; }
            public string ip { get; set; }
            public string port { get; set; }
            public string type { get; set; }
            public int lastChecked { get; set; }
            public bool get { get; set; }
            public bool post { get; set; }
            public bool cookies { get; set; }
            public bool referer { get; set; }
            public bool userAgent { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string country { get; set; }
            public int currentThreads { get; set; }
            public int threadsAllowed { get; set; }

        }
    }
}
