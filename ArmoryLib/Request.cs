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
using System.Data.Sql;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Data.SqlClient;

namespace ArmoryLib
{
    public static class RequestXml
    {
        public static string basePath = "./";

        public static TimeSpan OneDay = new TimeSpan(1, 0, 0, 0);
        //These will never change for our guild.
        public const string baseUrl = "http://www.wowarmory.com/";
        public const string userAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 3.5.20404)";
        public const string api_Guild = "guild-info.xml?";
        public const string api_CharacterSheet = "character-sheet.xml?";

        public static XmlDocument RequestXML(string url)
        {
            XmlDocument output = new XmlDocument();
            using (WebClient client = new WebClient())
            {
                client.Headers.Set("User-Agent", userAgent);
                bool complete = false;

                while (!complete)
                {
                    //try
                    //{
                        output.LoadXml(client.DownloadString(url));
                        complete = true;
                    //}
                    //catch { }
                }
            }
            return output;
        }
        public static XmlDocument RequestLocalXML(string path)
        {
            XmlDocument output = new XmlDocument();
            if (File.Exists(path))
            {
                DateTime created = File.GetCreationTime(path);
                //If it is an old XML Delete it and re-request
                if (DateTime.Now - created > OneDay)
                {
                    output = null;
                    //try
                    //{
                        File.Delete(path);

                    //}
                    //catch { }
                }
                else
                {
                    //try
                    //{
                        output.Load(path);
                    //}
                    //catch { output = null; }
                }
            }
            else output = null;//No file here
            return output;
        }
        public static XmlDocument RequestGuildXML(string server, string guild)
        {
            XmlDocument output;
            string armoryRequest = baseUrl + api_Guild + "r=" + server.Trim().Replace(" ", "+") + "&gn=" + guild.Trim().Replace(" ", "+") + "&rhtml=n";

            output = RequestXML(armoryRequest);

            return output;
        }
        public static XmlDocument RequestCharacterSheetXML(string characterUrl)
        {
            //Character Url Example
            //r=Terenas&amp;cn=Kittencannon"
            string server = "", cn = "";
            string[] split = characterUrl.Split(new string[] { "&amp;", "&" }, StringSplitOptions.RemoveEmptyEntries);
            server = split[0].Substring(2);
            cn = split[1].Substring(3);
            XmlDocument output = null;
            string armoryRequest = baseUrl + api_CharacterSheet + "r=" + server.Trim().Replace(" ", "+") + "&cn=" + cn.Trim().Replace(" ", "+") + "&rhtml=n";

            Console.WriteLine(armoryRequest);
            output = RequestXML(armoryRequest);

            return output;
        }

    }
    public static class RequestData
    {
        public static string SqlConnectionString;
        static object cReaderLock = new object();
        static SqlConnection cSqlReader;
        static object cWriterLock = new object();
        static SqlConnection cSqlWriter;

        static RequestData()
        {
        }

        public static TimeSpan maxCacheTime = new TimeSpan(7, 0, 0, 0);

        class CharacterSheetQueueItem
        {
            public string name { protected set; get; }
            public string server { protected set; get; }
            public string ArmoryUrl { protected set; get; }

            public CharacterSheetQueueItem(string Name, string Server, string URL)
            {
                this.name = Name;
                this.server = Server;
                this.ArmoryUrl = URL;
            }
            //Wrapper for RequestXml.RequestCharacterSheet
            public XmlDocument requestXml()
            {
                return RequestXml.RequestCharacterSheetXML(this.ArmoryUrl);
            }
        }

        static bool checkSqlConnections()
        {
            bool connected = false;
            if (SqlConnectionString == null) return false;
            if (cSqlReader == null || cSqlWriter == null)
            {
                cSqlReader = new SqlConnection(SqlConnectionString);
                cSqlWriter = new SqlConnection(SqlConnectionString);
            }
            lock (cReaderLock)
            {
                if (cSqlReader.State == ConnectionState.Broken || cSqlReader.State == ConnectionState.Closed)
                {
                    try
                    {
                        cSqlReader.Open();
                    }
                    catch { connected = false; }
                }
            }
            lock (cWriterLock)
            {
                if (cSqlWriter.State == ConnectionState.Broken || cSqlWriter.State == ConnectionState.Closed)
                {
                    try
                    {
                        cSqlWriter.Open();
                    }
                    catch { connected = false; }
                }
            }
            return connected;
        }

        public static GuildRoster getGuildRoster(string server, string guild)
        {
            checkSqlConnections();
            List<ArmoryCharacter> members = new List<ArmoryCharacter>();
            DateTime LastGuildRosterUpdate;
            #region Check last update time on the guild
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;
                    cmd.Parameters.Add("@REALM", SqlDbType.VarChar, 128).Value = server;
                    cmd.CommandText = "Select lastUpdate from armory_Guilds where guildName = @GN and realm = @REALM";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            rd.Read();
                            LastGuildRosterUpdate = rd.GetDateTime(0);
                        }
                        else
                        {
                            LastGuildRosterUpdate = DateTime.MinValue;
                        }
                    }
                }
            }
            #endregion
            #region Update the member info if the guild information is stale
            //Update the member info
            if (DateTime.Now - LastGuildRosterUpdate > maxCacheTime)
            {
                XmlDocument GuildXML = RequestXml.RequestGuildXML(server, guild);
                PopulateGuildMembers(GuildXML, server, guild);
                lock (cWriterLock)
                {
                    //The guild is not in the database, add it.
                    if (LastGuildRosterUpdate == DateTime.MinValue)
                    {
                        /*try
                        {*/
                            XmlNode xNode = GuildXML.GetElementsByTagName("guildHeader")[0];
                            using (SqlCommand cmd = cSqlWriter.CreateCommand())
                            {
                                cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;
                                cmd.Parameters.Add("@BG", SqlDbType.VarChar, 128).Value = xNode.Attributes["battleGroup"].Value;
                                cmd.Parameters.Add("@REALM", SqlDbType.VarChar, 128).Value = server;
                                cmd.Parameters.Add("@FAC", SqlDbType.Bit).Value = (xNode.Attributes["battleGroup"].Value == "0") ? 0 : 1;
                                cmd.Parameters.Add("@MEMBERS", SqlDbType.Int).Value = Convert.ToInt32(xNode.Attributes["count"].Value);
                                cmd.Parameters.Add("@URL", SqlDbType.VarChar, 512).Value = xNode.Attributes["url"].Value;

                                cmd.CommandText = "INSERT INTO armory_Guilds ([guildName],[battleGroup],[realm],[faction],[members],[url],[lastUpdate])"
                                    + "VALUES (@GN,@BG,@REALM,@FAC,@MEMBERS,@URL,getDate())";

                                cmd.ExecuteNonQuery();
                            }
                        /*}//If this fails the guild just doesn't get inserted.  The armory will be queried again.
                        catch { }*/
                    }//the guild is in the database, update the update time
                    else
                    {
                        using (SqlCommand cmd = cSqlWriter.CreateCommand())
                        {
                            cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;
                            cmd.Parameters.Add("@REALM", SqlDbType.VarChar, 128).Value = server;
                            cmd.CommandText = "UPDATE armory_Guilds SET [lastUpdate] = getDate() WHERE [guildName] = @GN and [realm] = @REALM";

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            #endregion
            #region Pull out all the guild members, and add them to the members list
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;

                    cmd.CommandText = "SELECT * From armory_CharacterRoster WHERE server =@SERVER and guild = @GN";

                    //Execute the query and parse that data
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            while (rd.Read())
                            {
                                members.Add(new ArmoryCharacter(server, rd.GetString(1), rd.GetByte(2), rd.GetByte(3),
                                        rd.GetByte(4), rd.GetBoolean(5), rd.GetInt32(6), rd.GetByte(7), rd.GetString(8)));
                            }
                        }
                    }
                }
            }
            #endregion
            return new GuildRoster(members);
        }
        static void PopulateGuildMembers(XmlDocument guildXml, string server, string guild)
        {
            XmlNodeList memberList = guildXml.GetElementsByTagName("character");
            List<ArmoryCharacter> members = new List<ArmoryCharacter>();

            //Parse out the members
            for (int i = 0; i < memberList.Count; i++)
            {
                //Try to add this member
                /*try
                {*/
                    //Note to self:
                    //ArmoryCharacter(string Name, byte Level, byte ClassID, byte RaceID, bool IsMale, int AchPoints, byte GuildRank, string ArmoryUrl)
                    members.Add(new ArmoryCharacter(memberList[i], server));
                /*}
                catch { }*/
            }

            //Add all of the members to the SQL database
            lock (cWriterLock)
            {
                using (SqlCommand cmd = cSqlWriter.CreateCommand())
                {
                    //First remove the roster guild association
                    cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;
                    cmd.CommandText = "UPDATE armory_CharacterRoster SET guild = '' WHERE server = @SERVER and guild = @GN";

                    cmd.ExecuteNonQuery();

                    //Remove any further guild associations
                    cmd.CommandText = "UPDATE armory_CharacterBaseStats SET guild = '' WHERE server = @SERVER and guild = @GN";

                    cmd.ExecuteNonQuery();

                    bool exists = false;
                    //Insert the members
                    lock (cReaderLock)
                    {
                        using (SqlCommand cmd_read = cSqlReader.CreateCommand())
                        {
                            foreach (ArmoryCharacter c in members)
                            {
                                exists = false;
                                cmd.Parameters.Clear();
                                cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 128).Value = c.name;
                                cmd.Parameters.Add("@LEVEL", SqlDbType.SmallInt).Value = c.level;
                                cmd.Parameters.Add("@CID", SqlDbType.SmallInt).Value = c.classID;
                                cmd.Parameters.Add("@RID", SqlDbType.SmallInt).Value = c.raceID;
                                //NOTE THIS IS OPPOSITE OF THE ARMORY.  I AM STORING AS A BOOL
                                cmd.Parameters.Add("@MALE", SqlDbType.Bit).Value = c.isMale ? 1 : 0;
                                cmd.Parameters.Add("@ACH", SqlDbType.Int).Value = c.achPoints;
                                cmd.Parameters.Add("@GRANK", SqlDbType.SmallInt).Value = c.guildRank;
                                cmd.Parameters.Add("@ARMURL", SqlDbType.VarChar, 512).Value = c.armoryUrl;
                                cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;
                                cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;

                                cmd_read.Parameters.Clear();
                                cmd_read.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;
                                cmd_read.Parameters.Add("@NAME", SqlDbType.VarChar, 128).Value = c.name;
                                //throw new Exception( "c.name: " + c.name);
                                cmd_read.CommandText = "SELECT * FROM armory_CharacterRoster WHERE server = @SERVER and name = @NAME";

                                using (SqlDataReader rd = cmd_read.ExecuteReader())
                                {
                                    exists = rd.HasRows;
                                }

                                if (exists)
                                {
                                    //Update the roster entry
                                    cmd.CommandText = "UPDATE armory_CharacterRoster SET [name] = @NAME,[level] = @LEVEL,[classId] = @CID,[raceId] =@RID,[isMale]= @MALE,[achPoints] = @ACH,"
                                        + " [guildRank]=@GRANK,[armoryUrl]=@ARMURL,[server]=@SERVER,[guild]=@GN,[lastUpdate]=getDate() WHERE [name] = @NAME and server = @SERVER";
                                    cmd.ExecuteNonQuery();

                                    //Update the character base stats (if we have any)
                                    cmd.Parameters.Clear();
                                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 128).Value = c.name;
                                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;
                                    cmd.Parameters.Add("@GN", SqlDbType.VarChar, 512).Value = guild;
                                    cmd.CommandText = "UPDATE armory_CharacterBaseStats SET [guild] = @GN WHERE [name] = @NAME and [server] = @SERVER";

                                    cmd.ExecuteNonQuery();
                                }
                                else
                                {
                                    cmd.CommandText = "INSERT INTO armory_CharacterRoster ([name],[level],[classId],[raceId],[isMale],[achPoints],[guildRank],[armoryUrl],[server],[guild],[lastUpdate])"
                                        + "VALUES (@NAME,@LEVEL,@CID,@RID,@MALE,@ACH,@GRANK,@ARMURL,@SERVER,@GN,getDate())";

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void AddCharacterSheetToQueue(string name, string server, string url)
        {
            bool found = false;
            #region See if it is in our queue
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@RTYPE", SqlDbType.VarChar, 32).Value = "characterSheet";
                    cmd.Parameters.Add("@URL", SqlDbType.VarChar, 128).Value = url.Trim();

                    cmd.CommandText = "Select * From armory_RequestQueue Where reqType = @RTYPE and url = @URL";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                            found = true;
                    }
                }
            }
            #endregion
            #region If it is not, add it to the queue
            if (!found)
            {
                lock (cWriterLock)
                {
                    using (SqlCommand cmd = cSqlWriter.CreateCommand())
                    {
                        cmd.Parameters.Add("@RTYPE", SqlDbType.VarChar, 32).Value = "characterSheet";
                        cmd.Parameters.Add("@URL", SqlDbType.VarChar, 128).Value = url.Trim();

                        cmd.CommandText = "INSERT INTO armory_RequestQueue (reqType, url, requestTime) VALUES ( @RTYPE, @URL, getDate() )";

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            #endregion
        }
        class QueueItem
        {
            public string rType, rUrl;
            public QueueItem(string rType, string rUrl)
            {
                this.rType = rType; this.rUrl = rUrl;
            }
        }
        public static int WorkRequestQueue(int count)
        {
            return WorkRequestQueue(count, 250);
        }
        public static int WorkRequestQueue(int count, int delay)
        {
            Queue<QueueItem> q = new Queue<QueueItem>();
            lock (cReaderLock)
            {
                //Pull up our desired work items
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.CommandText = "Select top " + count + " * From armory_RequestQueue order by requestTime asc";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            while (rd.Read())
                            {
                                //try
                                //{
                                    q.Enqueue(new QueueItem(rd.GetString(1), rd.GetString(2)));
                                //}
                                //catch { }
                            }
                        }
                    }
                }
            }
            foreach (QueueItem qi in q)
            {
                string[] tok;
                switch (qi.rType)
                {
                    case "characterSheet":
                        string name = "", server = "";
                        //r=Terenas&cn=Kittencannon
                        tok = qi.rUrl.TrimStart(new char[] { '?' }).Split(new string[] { "&amp;", "&" }, StringSplitOptions.RemoveEmptyEntries);
                        if (tok.Length > 1)
                        {
                            name = tok[0].Substring(2);
                            server = tok[1].Substring(3);

                            ArmoryCharacterSheet c = getFreshCharacterSheet(name, server, qi.rUrl);
                            if (c != null)
                            {//We got it, clear it from the SQL Q
                                lock (cWriterLock)
                                {
                                    using (SqlCommand cmd = cSqlWriter.CreateCommand())
                                    {
                                        cmd.Parameters.Add("@RTYPE", SqlDbType.VarChar, 32).Value = qi.rType;
                                        cmd.Parameters.Add("@RURL", SqlDbType.VarChar, 128).Value = qi.rUrl;

                                        cmd.CommandText = "DELETE FROM armory_RequestQueue WHERE reqType = @RTYPE and url = @RURL";

                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        Thread.Sleep(delay);
                        break;
                    default:
                        break;
                }
            }
            int QueueSize = 0;
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.CommandText = "SELECT count(*) From armory_RequestQueue";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            rd.Read();
                            QueueSize = rd.GetInt32(0);
                        }
                    }
                }
            }
            return QueueSize;
        }

        public enum ReqType { CHARACTER_SHEET }
        public static string ReqTypeToString(ReqType rType)
        {
            switch (rType)
            {
                case ReqType.CHARACTER_SHEET:
                    return "characterSheet";
                default:
                    return "";
            }
        }
        public static void PurgeRequestQueue(ReqType rType, string url)
        {
            checkSqlConnections();
            string rtype = ReqTypeToString(rType);
            lock (cWriterLock)
            {
                using (SqlCommand cmd = cSqlWriter.CreateCommand())
                {
                    cmd.Parameters.Add("@RTYPE", SqlDbType.VarChar, 32).Value = ReqTypeToString(rType);
                    cmd.Parameters.Add("@URL", SqlDbType.VarChar).Value = url;
                    cmd.CommandText = "DELETE FROM armory_RequestQueue WHERE reqType = @RTYPE and url = @URL";

                    cmd.ExecuteNonQuery();
                }
            }
        }
        //Character Sheet related functions
        public static void SaveBaseStats(string name, string server, BaseStats stats)
        {
            if (stats == null) return;
            bool exists = false;

            checkSqlConnections();
            #region See if we already have a DB entry
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 512).Value = name;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;

                    cmd.CommandText = "Select * From armory_CharacterBaseStats Where name = @NAME and server = @SERVER";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                            exists = true;
                    }
                }
            }
            #endregion
            #region Insert or update this record
            lock (cWriterLock)
            {
                using (SqlCommand cmd = cSqlWriter.CreateCommand())
                {
                    //Set our paramaters
                    #region Paramaters
                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 512).Value = name;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;
                    //HP
                    cmd.Parameters.Add("@HP", SqlDbType.Int).Value = stats.maxHP;
                    //STR
                    cmd.Parameters.Add("@BSTR", SqlDbType.Int).Value = stats.str.baseStr;
                    cmd.Parameters.Add("@ESTR", SqlDbType.Int).Value = stats.str.effectiveStr;
                    cmd.Parameters.Add("@ATKSTR", SqlDbType.Int).Value = stats.str.atkFromStr;
                    cmd.Parameters.Add("@BLSTR", SqlDbType.Int).Value = stats.str.blockFromStr;
                    //AGI
                    cmd.Parameters.Add("@BAGI", SqlDbType.Int).Value = stats.agi.baseAgi;
                    cmd.Parameters.Add("@EAGI", SqlDbType.Int).Value = stats.agi.effectiveAgi;
                    cmd.Parameters.Add("@ARAGI", SqlDbType.Int).Value = stats.agi.armorFromAgi;
                    cmd.Parameters.Add("@ATKAGI", SqlDbType.Int).Value = stats.agi.atkFromAgi;
                    cmd.Parameters.Add("@CRITAGI", SqlDbType.Float).Value = stats.agi.critFromAgi;
                    //STA
                    cmd.Parameters.Add("@BSTA", SqlDbType.Int).Value = stats.sta.baseSta;
                    cmd.Parameters.Add("@ESTA", SqlDbType.Int).Value = stats.sta.effectiveSta;
                    cmd.Parameters.Add("@HPSTA", SqlDbType.Int).Value = stats.sta.hpFromSta;
                    cmd.Parameters.Add("@PETSTA", SqlDbType.Int).Value = stats.sta.petBonusStam;
                    //Int
                    cmd.Parameters.Add("@BINT", SqlDbType.Int).Value = stats.intel.baseInt;
                    cmd.Parameters.Add("@EINT", SqlDbType.Int).Value = stats.intel.effectiveInt;
                    cmd.Parameters.Add("@CRITINT", SqlDbType.Float).Value = stats.intel.critFromInt;
                    cmd.Parameters.Add("@MPINT", SqlDbType.Int).Value = stats.intel.mpFromInt;
                    cmd.Parameters.Add("@PETINT", SqlDbType.Int).Value = stats.intel.petBonusInt;
                    //Spir
                    cmd.Parameters.Add("@BSPIR", SqlDbType.Int).Value = stats.spir.baseSpir;
                    cmd.Parameters.Add("@ESPIR", SqlDbType.Int).Value = stats.spir.effectiveSpir;
                    cmd.Parameters.Add("@HPSPIR", SqlDbType.Int).Value = stats.spir.hpRegenFromSpir;
                    cmd.Parameters.Add("@MPSPIR", SqlDbType.Int).Value = stats.spir.mpRegenFromSpir;
                    //Armor
                    cmd.Parameters.Add("@BARMOR", SqlDbType.Int).Value = stats.armor.baseArmor;
                    cmd.Parameters.Add("@EARMOR", SqlDbType.Int).Value = stats.armor.effectiveArmor;
                    cmd.Parameters.Add("@MITARMOR", SqlDbType.Float).Value = stats.armor.armorMitigation;
                    cmd.Parameters.Add("@PETARMOR", SqlDbType.Int).Value = stats.armor.petArmorBonus;
                    //Second Bar
                    cmd.Parameters.Add("@ESECOND", SqlDbType.Int).Value = stats._2ndBar.effective;
                    cmd.Parameters.Add("@SECONDCASTING", SqlDbType.Int).Value = stats._2ndBar.casting;
                    cmd.Parameters.Add("@SECONDNOTCASTING", SqlDbType.Int).Value = stats._2ndBar.notCasting;
                    cmd.Parameters.Add("@SECONDPERFIVE", SqlDbType.Int).Value = stats._2ndBar.perFive;
                    cmd.Parameters.Add("@SECONDTYPE", SqlDbType.Char).Value = stats._2ndBar.type;

                    #endregion
                    if (exists)
                    {
                        cmd.CommandText = "UPDATE armory_CharacterBaseStats "
                            + "SET maxHP = @HP, baseStr = @BSTR, effectiveStr = @ESTR, atkFromStr = @ATKSTR, blockFromStr = @BLSTR, "
                            + "baseAgi = @BAGI, effectiveAgi = @EAGI, armorFromAgi = @ARAGI, atkFromAgi = @ATKAGI, critFromAgi = @CRITAGI, "
                            + "baseSta = @BSTA, effectiveSta = @ESTA, hpFromSta = @HPSTA, petBonusStam = @PETSTA, "
                            + "baseInt = @BINT, effectiveInt = @EINT, critFromInt = @CRITINT, mpFromInt = @MPINT, petBonusInt = @PETINT, "
                            + "baseSpir = @BSPIR, effectiveSpir = @ESPIR, hpRegenFromSpir = @HPSPIR, mpRegenFromSpir = @MPSPIR, "
                            + "baseArmor = @BARMOR, effectiveArmor = @EARMOR, armorMitigation = @MITARMOR, petArmorBonus = @PETARMOR, "
                            + "lastUpdate = getDate(), "
                            + "effectiveSecondBar = @ESECOND, secondBarCasting = @SECONDCASTING, secondBarNotCasting = @SECONDNOTCASTING, "
                            + "secondBarPerFive = @SECONDPERFIVE, secondBarType = @SECONDTYPE "
                            + " WHERE name = @NAME and server = @SERVER";
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO armory_CharacterBaseStats "
                            + "(name, server, maxHP, baseStr, effectiveStr, atkFromStr, blockFromStr, "
                            + "baseAgi, effectiveAgi, armorFromAgi, atkFromAgi, critFromAgi, "
                            + "baseSta, effectiveSta, hpFromSta, petBonusStam, "
                            + "baseInt, effectiveInt, critFromInt, mpFromInt, petBonusInt, "
                            + "baseSpir, effectiveSpir, hpRegenFromSpir, mpRegenFromSpir, "
                            + "baseArmor, effectiveArmor, armorMitigation, petArmorBonus, lastUpdate, "
                            + "effectiveSecondBar, secondBarCasting, secondBarNotCasting, secondBarPerFive, "
                            + " secondBarType) "
                            + "VALUES (@NAME, @SERVER, @HP, @BSTR, @ESTR, @ATKSTR, @BLSTR, "
                            + "@BAGI, @EAGI, @ARAGI, @ATKAGI, @CRITAGI, "
                            + "@BSTA, @ESTA, @HPSTA, @PETSTA, "
                            + "@BINT, @EINT, @CRITINT, @MPINT, @PETINT, "
                            + "@BSPIR, @ESPIR, @HPSPIR, @MPSPIR, "
                            + "@BARMOR, @EARMOR, @MITARMOR, @PETARMOR, getDate(), " 
                            + "@ESECOND,@SECONDCASTING,@SECONDNOTCASTING,@SECONDPERFIVE,@SECONDTYPE)";
                    }

                    //try
                    //{
                        cmd.ExecuteNonQuery();
                    //}//If there is some error inserting this in to the DB d/w about it
                    //catch { }

                    //Delete this record from the request queue if it exists
                    PurgeRequestQueue(ReqType.CHARACTER_SHEET,"r=" + server + "&cn=" + name);
                }
            }
            #endregion
        }
        public static void SaveSpellData(string name, string server, Spell spell)
        {
            if (spell == null) return;
            bool exists = false;

            checkSqlConnections();
            #region See if we already have a DB entry
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 512).Value = name;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;

                    cmd.CommandText = "Select * From armory_CharacterSpell Where name = @NAME and server = @SERVER";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                            exists = true;
                    }
                }
            }
            #endregion
            #region Insert or update this record
            lock (cWriterLock)
            {
                using (SqlCommand cmd = cSqlWriter.CreateCommand())
                {
                    //Set our paramaters
                    #region Paramaters
                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 512).Value = name;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;
                    //Spell DMG
                    cmd.Parameters.Add("@ARD", SqlDbType.Int).Value = spell.arcane.dmg;
                    cmd.Parameters.Add("@FID", SqlDbType.Int).Value = spell.fire.dmg;
                    cmd.Parameters.Add("@FRD", SqlDbType.Int).Value = spell.frost.dmg;
                    cmd.Parameters.Add("@HOD", SqlDbType.Int).Value = spell.holy.dmg;
                    cmd.Parameters.Add("@NAD", SqlDbType.Int).Value = spell.nature.dmg;
                    cmd.Parameters.Add("@SHD", SqlDbType.Int).Value = spell.shadow.dmg;
                    //Healing
                    cmd.Parameters.Add("@HEAL", SqlDbType.Int).Value = spell.healing;
                    //hit
                    cmd.Parameters.Add("@HITP", SqlDbType.Float).Value = spell.stats.hitPct;
                    cmd.Parameters.Add("@HITR", SqlDbType.Int).Value = spell.stats.hitRating;
                    //Pen
                    cmd.Parameters.Add("@PEN", SqlDbType.Int).Value = spell.stats.penetration;
                    //-Resist
                    cmd.Parameters.Add("@RRES", SqlDbType.Int).Value = spell.stats.reducedResist;
                    //Crit
                    cmd.Parameters.Add("@ARC", SqlDbType.Float).Value = spell.arcane.crit;
                    cmd.Parameters.Add("@FIC", SqlDbType.Float).Value = spell.fire.crit;
                    cmd.Parameters.Add("@FRC", SqlDbType.Float).Value = spell.frost.crit;
                    cmd.Parameters.Add("@HOC", SqlDbType.Float).Value = spell.holy.crit;
                    cmd.Parameters.Add("@NAC", SqlDbType.Float).Value = spell.nature.crit;
                    cmd.Parameters.Add("@SHC", SqlDbType.Float).Value = spell.shadow.crit;
                    //Mana
                    cmd.Parameters.Add("@MANAC", SqlDbType.Float).Value = spell.stats.manaCasting;
                    cmd.Parameters.Add("@MANANC", SqlDbType.Float).Value = spell.stats.manaNotCasting;
                    //haste
                    cmd.Parameters.Add("@HASTEP", SqlDbType.Float).Value = spell.stats.haste;
                    cmd.Parameters.Add("@HASTER", SqlDbType.Int).Value = spell.stats.hasteRating;
                    

                    #endregion
                    if (exists)
                    {
                        cmd.CommandText = "UPDATE armory_CharacterSpell  "
                            + "SET arcaneDmg = @ARD, fireDmg = @FID, frostDmg = @FRD, holyDmg = @HOD, natureDmg = @NAD, shadowDmg = @SHD, "
                            + "bonusHealing = @HEAL, hitPct = @HITP, penetration = @PEN, reducedResist = @RRES, hitRating = @HITR, "
                            + "arcaneCrit = @ARC, fireCrit = @FIC, frostCrit = @FRC, holyCrit = @HOC, natureCrit = @NAC, shadowCrit = @SHC, "
                            + "manaCasting = @MANAC, manaNotCasting = @MANANC, hastePct = @HASTEP, hasteRating = @HASTER, "
                            + "lastUpdate = getDate() "
                            + " WHERE name = @NAME and server = @SERVER";
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO armory_CharacterSpell  "
                            + "(name, server, arcaneDmg, fireDmg, frostDmg, holyDmg, natureDmg, "
                            + "shadowDmg, bonusHealing, hitPct, penetration, reducedResist, "
                            + "hitRating, arcaneCrit, fireCrit, frostCrit, "
                            + "holyCrit, natureCrit, shadowCrit, manaCasting, manaNotCasting, "
                            + "hastePct, hasteRating, lastUpdate) "
                            + "VALUES (@NAME, @SERVER, @ARD, @FID, @FRD, @HOD, @NAD, "
                            + "@SHD, @HEAL, @HITP, @PEN, @RRES, "
                            + "@HITR, @ARC, @FIC, @FRC, "
                            + "@HOC, @NAC, @SHC, @MANAC, @MANANC, "
                            + "@HASTEP, @HASTER, getDate())";
                    }

                    //try
                    //{
                    cmd.ExecuteNonQuery();
                    //}//If there is some error inserting this in to the DB d/w about it
                    //catch { }

                    //Delete this record from the request queue if it exists
                    PurgeRequestQueue(ReqType.CHARACTER_SHEET, "r=" + server + "&cn=" + name);
                }
            }
            #endregion
        }
        public static ArmoryCharacterSheet getFreshCharacterSheet(string name, string server, string url)
        {
            XmlDocument x;
            return getFreshCharacterSheet(name, server, url, out x);
        }
        public static ArmoryCharacterSheet getFreshCharacterSheet(string name, string server, string url, out XmlDocument rawSheet)
        {
            ArmoryCharacterSheet output = null;
            //Request the new character sheet xml
            XmlDocument sheet = RequestXml.RequestCharacterSheetXML(url);
            BaseStats baseStats = null;
            Spell spellData = null;
            int MaxHP;

            XmlNodeList xList, xList2;
            #region BaseStats
            xList = sheet.GetElementsByTagName("health");
            if (xList.Count > 0)
            {
                MaxHP = Convert.ToInt32(sheet.GetElementsByTagName("health")[0].Attributes["effective"].Value);
            }
            else MaxHP = 0;
            xList = sheet.GetElementsByTagName("baseStats");
            xList2 = sheet.GetElementsByTagName("secondBar");
            if (xList.Count > 0 && xList2.Count > 0)
            {
                baseStats = new BaseStats(xList[0], MaxHP, xList2[0]);
            }
            #endregion
            if (baseStats != null)
            {
                output = new ArmoryCharacterSheet(baseStats, name, server, url);
                //We got new data, so save it to the DB.
                SaveBaseStats(name, server, baseStats);
            }

            #region Spell data
            xList = sheet.GetElementsByTagName("spell");
            if (xList.Count > 0)
            {
                spellData = new Spell(xList[0]);
            }
            #endregion
            if (spellData != null)
            {
                SaveSpellData(name,server,spellData);
                if (output != null)
                    output.SetSpell(spellData);
            }
            
            //For their records
            rawSheet = sheet;
            return output;
        }
        public static ArmoryCharacterSheet getCachedCharacterSheet(string name, string server, string url)
        {
            return getCharacterSheet(name, server, url);
        }
        public static ArmoryCharacterSheet getCharacterSheet(string name, string server, string url)
        {
            checkSqlConnections();
            ArmoryCharacterSheet output = null;
            bool RequestNewData = false;            
            #region Query SQL for Cached Data
            lock (cReaderLock)
            {
                DateTime LastUpdate = DateTime.MinValue;
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar, 512).Value = name;
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar, 128).Value = server;

                    cmd.CommandText = "Select * from armory_CharacterBaseStats Where name = @NAME and server = @SERVER";
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            rd.Read();
                            LastUpdate = rd.GetDateTime(30);
                            if (DateTime.Now - LastUpdate > maxCacheTime)
                                RequestNewData = true;
                            int maxHP = (int)rd["maxHP"], baseStr = (int)rd["baseStr"], effectiveStr = (int)rd["effectiveStr"], atkFromStr = (int)rd["atkFromStr"], blockFromStr = (int)rd["blockFromStr"],
                                    baseAgi = (int)rd["baseAgi"], effectiveAgi = (int)rd["effectiveAgi"], armorFromAgi = (int)rd["armorFromAgi"], atkFromAgi = (int)rd["atkFromAgi"],
                                    baseSta = (int)rd["baseSta"], effectiveSta = (int)rd["effectiveSta"], hpFromSta = (int)rd["hpFromSta"], petBonusStam = (int)rd["petBonusStam"],
                                    baseInt = (int)rd["baseInt"], effectiveInt = (int)rd["effectiveInt"], mpFromInt = (int)rd["mpFromInt"], petBonusInt = (int)rd["petBonusInt"],
                                    baseSpir = (int)rd["baseSpir"], effectiveSpir = (int)rd["effectiveSpir"], hpRegenFromSpir = (int)rd["hpRegenFromSpir"], mpRegenFromSpir = (int)rd["mpRegenFromSpir"],
                                    baseArmor = (int)rd["baseArmor"], effectiveArmor = (int)rd["effectiveArmor"], petArmorBonus = (int)rd["petArmorBonus"], effectiveSecondBar = (int)rd["effectiveSecondBar"],
                                    secondBarCasting = (int)rd["secondBarCasting"], secondBarNotCasting = (int)rd["secondBarNotCasting"], secondBarPerFive = (int)rd["secondBarPerFive"];
                            //throw new Exception(rd["critFromAgi"].ToString());
                            float critFromAgi = (float)(double)rd["critFromAgi"], critFromInt = (float)(double)rd["critFromInt"], armorMitigation = (float)(double)rd["armorMitigation"];
                            char secondBarType = ((string)rd["secondBarType"])[0];

                            output = new ArmoryCharacterSheet(new BaseStats(maxHP,
                                new BaseStats.atribStr(baseStr, effectiveStr, atkFromStr, blockFromStr),
                                new BaseStats.atribAgi(baseAgi, effectiveAgi, atkFromAgi, armorFromAgi, critFromAgi),
                                new BaseStats.atribSta(baseSta, effectiveSta, hpFromSta, petBonusStam),
                                new BaseStats.atribInt(baseInt, effectiveInt, mpFromInt, petBonusInt, critFromInt),
                                new BaseStats.atribSpir(baseSpir, effectiveSpir, hpRegenFromSpir, mpRegenFromSpir),
                                new BaseStats.atribArmor(baseArmor, effectiveArmor, petArmorBonus, armorMitigation),
                                new SecondBar(effectiveSecondBar, secondBarCasting, secondBarNotCasting, secondBarPerFive, secondBarType)),//TODO Setup Second Bar here
                                name, server, url);

                        }
                        else RequestNewData = true;
                    }
                }
            }
            #endregion
            //If we need new data add it to the data queue
            if (RequestNewData)
                AddCharacterSheetToQueue(name, server, url);
            return output;
        }
        public static ArmoryCharacterSheet[] getTopCharacterSheets(string columName, int count, bool asc, string guild)
        {
            string sql = "";
            return getTopCharacterSheets(columName, count, asc, guild, out sql);
        }
        public static ArmoryCharacterSheet[] getTopCharacterSheets(string columName, int count, bool asc, string guild, out string sqlString)
        {
            checkSqlConnections();
            sqlString = "";
            List<ArmoryCharacterSheet> output = new List<ArmoryCharacterSheet>(); ;
            
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {

                    if (guild != null && guild.Length > 1)
                        cmd.Parameters.Add("@GUILD", SqlDbType.VarChar).Value = guild;
                    cmd.CommandText = "SELECT TOP " + count + " * FROM armory_CharacterBaseStats ";
                    if (guild != null && guild.Length > 1)
                        cmd.CommandText += "WHERE guild = @GUILD ";
                    cmd.CommandText += " ORDER BY " + columName + " " + (asc ? "asc" : "desc");

                    sqlString = cmd.CommandText;

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            while (rd.Read())
                            {
                                string name = (string)rd["name"], server = (string)rd["server"];
                                int maxHP = (int)rd["maxHP"], baseStr = (int)rd["baseStr"], effectiveStr = (int)rd["effectiveStr"], atkFromStr = (int)rd["atkFromStr"], blockFromStr = (int)rd["blockFromStr"],
                                    baseAgi = (int)rd["baseAgi"], effectiveAgi = (int)rd["effectiveAgi"], armorFromAgi = (int)rd["armorFromAgi"], atkFromAgi = (int)rd["atkFromAgi"],
                                    baseSta = (int)rd["baseSta"], effectiveSta = (int)rd["effectiveSta"], hpFromSta = (int)rd["hpFromSta"], petBonusStam = (int)rd["petBonusStam"],
                                    baseInt = (int)rd["baseInt"], effectiveInt = (int)rd["effectiveInt"], mpFromInt = (int)rd["mpFromInt"], petBonusInt = (int)rd["petBonusInt"],
                                    baseSpir = (int)rd["baseSpir"], effectiveSpir = (int)rd["effectiveSpir"], hpRegenFromSpir = (int)rd["hpRegenFromSpir"], mpRegenFromSpir = (int)rd["mpRegenFromSpir"],
                                    baseArmor = (int)rd["baseArmor"], effectiveArmor = (int)rd["effectiveArmor"], petArmorBonus = (int)rd["petArmorBonus"], effectiveSecondBar = (int)rd["effectiveSecondBar"],
                                    secondBarCasting = (int)rd["secondBarCasting"], secondBarNotCasting = (int)rd["secondBarNotCasting"], secondBarPerFive = (int)rd["secondBarPerFive"];
                                //throw new Exception(rd["critFromAgi"].ToString());
                                float critFromAgi = (float)(double)rd["critFromAgi"], critFromInt = (float)(double)rd["critFromInt"], armorMitigation = (float)(double)rd["armorMitigation"];
                                char secondBarType = ((string)rd["secondBarType"])[0];

                                output.Add(new ArmoryCharacterSheet(new BaseStats(maxHP,
                                    new BaseStats.atribStr(baseStr, effectiveStr, atkFromStr, blockFromStr),
                                    new BaseStats.atribAgi(baseAgi, effectiveAgi, atkFromAgi, armorFromAgi, critFromAgi),
                                    new BaseStats.atribSta(baseSta, effectiveSta, hpFromSta, petBonusStam),
                                    new BaseStats.atribInt(baseInt, effectiveInt, mpFromInt, petBonusInt, critFromInt),
                                    new BaseStats.atribSpir(baseSpir, effectiveSpir, hpRegenFromSpir, mpRegenFromSpir),
                                    new BaseStats.atribArmor(baseArmor, effectiveArmor, petArmorBonus, armorMitigation),
                                    new SecondBar(effectiveSecondBar, secondBarCasting, secondBarNotCasting, secondBarPerFive, secondBarType)),//TODO Setup Second Bar here
                                    name, server, "r=" + server + "&cn=" + name));
                            }
                        }
                    }
                }
            }

            return output.ToArray();
        }
        public static DateTime CharacterSheetLastUpdateTime(string name, string server)
        {
            checkSqlConnections();
            DateTime output = DateTime.MinValue;

            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.Parameters.Add("@SERVER", SqlDbType.VarChar).Value = server;
                    cmd.Parameters.Add("@NAME", SqlDbType.VarChar).Value = name;

                    cmd.CommandText = "SELECT [lastUpdate] FROM armory_CharacterBaseStats WHERE [name] = @NAME and  server = @SERVER";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.HasRows)
                        {
                            rd.Read();
                            for (int i = 0; i < rd.FieldCount; i++)
                            {
                                switch (rd.GetName(i))
                                {
                                    case "lastUpdate":
                                        output = rd.GetDateTime(i);
                                        break;
                                }
                            }
                            
                        }
                    }

                }
            }
            return output;
        }


        public static List<string> getRequestQueueItems(int count)
        {
            List<string> output = new List<string>();
            checkSqlConnections();
            lock (cReaderLock)
            {
                using (SqlCommand cmd = cSqlReader.CreateCommand())
                {
                    cmd.CommandText = "SELECT TOP " + count + " * FROM armory_RequestQueue ORDER BY requestTime ASC";

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        string rType = "", url = "";
                        if (rd.HasRows)
                        {
                            while (rd.Read())
                            {
                                rType = rd.GetString(1).Trim();
                                url = rd.GetString(2).Trim();
                                output.Add(rType + "|" + url);
                            }
                        }
                    }
                }
            }

            return output;
        }
    }

    public static class PostData
    {
        public static void PostCharacterSheetXML(XmlDocument sheet, string server, string name)
        {
            /*try
            {*/
                //Request the new character sheet xml
                BaseStats baseStats = null;
                int MaxHP;

                XmlNodeList xList, xList2;
                #region BaseStats
                xList = sheet.GetElementsByTagName("health");
                if (xList.Count > 0)
                {
                    MaxHP = Convert.ToInt32(sheet.GetElementsByTagName("health")[0].Attributes["effective"].Value);
                }
                else MaxHP = 0;
                xList = sheet.GetElementsByTagName("baseStats");
                xList2 = sheet.GetElementsByTagName("secondBar");
                if (xList.Count > 0 && xList2.Count >0)
                {
                    baseStats = new BaseStats(xList[0], MaxHP, xList2[0]);
                }
                #endregion
                if (baseStats != null)
                    RequestData.SaveBaseStats(name, server, baseStats);
            /*}
            catch { }*/
        }
    }
}
