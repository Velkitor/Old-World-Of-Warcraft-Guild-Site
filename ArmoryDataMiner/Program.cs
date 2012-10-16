using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Web;
using System.Threading;
using System.IO;
using System.Data;

namespace ArmoryDataMiner
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Miner m = new Miner();

            m.MinerMain();
            
        }
    }
    public enum ReqType { CHARACTER_SHEET, UNKNOWN }
    class UrlQueueItem
    {
        public string url { protected set; get; }
        public ReqType rType { protected set; get; }
        public string rTypeString
        {
            get
            {
                switch(rType){
                    case ReqType.CHARACTER_SHEET:
                        return "characterSheet";
                    default:
                        return "";
                }
            }
        }

        public UrlQueueItem(ReqType rType, string url)
        {
            this.url = url;
            this.rType = rType;
        }
    }
    class Miner
    {
        public const string baseUrl = "http://www.wowarmory.com/";
        public const string api_Guild = "guild-info.xml?";
        public const string api_CharacterSheet = "character-sheet.xml?";
        static bool _503 = false;
        WebClient client = new WebClient();
        Queue<UrlQueueItem> UrlQueue = new Queue<UrlQueueItem>();
        object myLock = new object();

        public Miner() 
        {
            client.Headers.Set("User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 3.5.20404)");
            client.BaseAddress = baseUrl;
        }

        public void MinerMain()
        {
            Console.WriteLine("Armory Miner v0.1");
            bool worked = false, sleep=false;
            int sleeps = 0, sleepTime=0;
            Random r = new Random(1234567);
            while (true)
            {
                if (_503)
                {
                    Thread.Sleep((int)(1000f * 60f * (r.NextDouble() * 3f + 1f)));
                    _503 = false;
                    continue;
                }
                worked = false; sleep = false;
                lock (myLock)
                {
                    #region Work URL Queue
                    //Do we have something to work on?
                    if (UrlQueue.Count > 0)
                    {
                        UrlQueueItem qi = UrlQueue.Dequeue();
                        switch (qi.rType)
                        {
                            case ReqType.CHARACTER_SHEET:
                                try
                                {
                                    WorkCharacterSheet(qi);
                                    worked = true;
                                }
                                catch { }
                                break;
                        }
                    }

                    #endregion
                    #region Request Queue
                    else
                    {
                        try
                        {
                            RequestQueue(10);
                        }
                        catch { }
                        
                        if (UrlQueue.Count == 0)
                            sleep = true;
                    }
                    #endregion
                }
                if (worked)
                {
                    sleepTime = 2000 + r.Next(3333, 33333);
                    Console.WriteLine("Sleeping for " + ((float)sleepTime / 1000f).ToString("F2") + " seconds.");
                    Thread.Sleep(sleepTime);
                }
                else if (sleep)
                {
                    Console.WriteLine("No items to work on.  Sleeping for 15 min");
                    sleeps++;
                    Thread.Sleep(1000 * 60 * 15);
                    //4 hours
                    if (sleeps >= 16)
                    {
                        //Try and update the request queue
                        lock (myLock)
                        {
                            //Not critical if this automated process fails
                            try
                            {
                                string dq = client.DownloadString("http://overlordsofjustice.com/BuildRequestQueue.aspx?k=L0ngnubby");
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        void RequestQueue(int count)
        {            
            Console.WriteLine("Requesting 10 more items to work on.");
            lock (myLock)
            {
                string page = "";
                page = client.DownloadString("http://overlordsofjustice.com/RequestQueue.aspx?n=10");
                string[] tok = page.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), tok2;
                UrlQueueItem qi;
                ReqType rType;
                foreach (string s in tok)
                {
                    tok2 = s.Split(new char[]{'|'},StringSplitOptions.RemoveEmptyEntries);
                    if (tok2.Length > 1)
                    {
                        switch (tok2[0])
                        {
                            case "characterSheet":
                                rType = ReqType.CHARACTER_SHEET;
                                break;
                            default:
                                rType = ReqType.UNKNOWN;
                                break;
                        }
                        if (rType != ReqType.UNKNOWN)
                        {
                            qi = new UrlQueueItem(rType, tok2[1]);
                            UrlQueue.Enqueue(qi);
                            Console.WriteLine("Queueing: " + s);
                        }
                    }
                }
            }
        }
        void WorkCharacterSheet(UrlQueueItem item)
        {
            Console.WriteLine("[CS]Working on: " + item.url);
            XmlDocument data = new XmlDocument();
            string server = "", cn = "";
            string[] tok = item.url.Split(new string[] { "&amp;", "&" }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length > 1)
            {
                server = tok[0].Substring(2);
                cn = tok[1].Substring(3);
            }
            
            if (!Directory.Exists("./Armory/" + server))
            {
                Console.WriteLine("[CS] Directory Created: ./Armory/" + server + "/");
                Directory.CreateDirectory("./Armory/" + server);
            }
            string FN ="./Armory/" + server + "/" + cn + ".xml";
            lock (myLock)
            {
                try
                {
                    Console.WriteLine(api_CharacterSheet + item.url);
                    string url = baseUrl + api_CharacterSheet + item.url + "&rhtml=n";
                    Console.WriteLine(url);
                    string xml = client.DownloadString(url);
                    //Console.WriteLine(xml);
                    data.LoadXml(xml);
                    
                    data.Save(FN);
                    //Console.WriteLine(data.OuterXml);
                }
                catch (Exception e) 
                {
                    Console.WriteLine("Character sheet req: Exception:");
                    if (e.Message.Contains("503")) _503 = true;
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                        Console.WriteLine(e.InnerException.Message);
                    try
                    {
                        string dq = client.DownloadString("http://overlordsofjustice.com/DequeueCharacterSheet.aspx?k=L0ngnubby&r=" + server + "&cn=" + cn);
                        Console.WriteLine("[CS]" + dq);
                    }
                    catch (Exception ex) 
                    {
                        Console.WriteLine("[CS] Exception:");
                        Console.WriteLine(ex.Message);
                        if (ex.InnerException != null)
                            Console.WriteLine(ex.InnerException.Message);
                    }
                }
                //No char info tag
                //characterInfo errCode="noCharacter"
                bool noCharacter = false;
                try
                {
                    XmlNodeList xList = data.GetElementsByTagName("characterInfo");
                    foreach (XmlNode xNode in xList)
                    {
                        try
                        {
                            if (xNode.Attributes["errCode"] != null)
                            {
                                noCharacter = true;
                                break;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                if (noCharacter)
                {
                    string dq = client.DownloadString("http://overlordsofjustice.com/DequeueCharacterSheet.aspx?k=L0ngnubby&r=" + server + "&cn=" + cn);
                    Console.WriteLine("[CS] " + dq);
                }
                else
                {
                    client.UploadFile(new Uri("http://overlordsofjustice.com/PostCharacterSheet.aspx?k=L0ngnubby&r=" + server + "&cn=" + cn), "POST", FN);
                    File.Delete(FN);
                }
                //TODO: Upload the xml to ooj.com
                //client.UploadFileAsync
            }

        }
    }
}
