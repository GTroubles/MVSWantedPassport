using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MVSWP
{
    class Program
    {
        public static void Main()
        {
            try
            {
                Data.Before();
                DataTable getData = GetData();
                Data.ClearTemp();

                string find = "description = ''";
                DataRow[] foundRows = getData.Select(find);

                if (foundRows.Count() >= 1)
                {
                    for (int i = 0; i < foundRows.Count(); i++)
                    {
                        Data.LoadInfoToTemp(DownloadJson(getData.Rows[i]["url"].ToString()));
                    }
                }
                if (Data.CheckProcedure() > 0)
                {
                    Console.WriteLine("Старт процедуры!");
                    Data.LoadInfoToOriginal();
                    Console.WriteLine("Конец процедуры!");
                    Data.After();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
            Console.ReadKey();
        }
        public static DataTable GetData()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://data.gov.ua/api/3/action/package_show?id=ab09ed00-4f51-4f6c-a2f7-1b2fb118be0f");
            request.Method = "GET";
            WebResponse response = request.GetResponse();

            List<byte> list = new List<byte>();
            var bytes = new byte[1024];

            Stream stream = response.GetResponseStream();
            int bytesRead;
            do
            {
                bytesRead = stream.Read(bytes, 0, 1024);
                list.AddRange(bytes.Take(bytesRead));
            } while (bytesRead > 0);

            MemoryStream ms = new MemoryStream(list.ToArray());
            JObject obj = JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()));
            JToken resources = obj["result"].SelectToken("resources");
            for (int i = 0; i < resources.Count(); i++)
            {
                resources[i].SelectToken("archiver").Parent.Remove();
                resources[i].SelectToken("qa").Parent.Remove();
            }

            DataTable dt = JsonConvert.DeserializeObject<DataTable>(resources.ToString());

            return dt;
        }

        private static MemoryStream DownloadJson(string url)
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                WebResponse webResponse = webRequest.GetResponse();
                List<byte> list = new List<byte>();
                var bytes = new byte[1024];
                Stream stream = webResponse.GetResponseStream();

                int bytesRead = 0;
                do
                {
                    bytesRead = stream.Read(bytes, 0, 1024);
                    list.AddRange(bytes.Take(bytesRead));
                } while (bytesRead > 0);

                MemoryStream ms = new MemoryStream(list.ToArray());
                return ms;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
