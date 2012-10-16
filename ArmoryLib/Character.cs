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
    public class ArmoryCharacterSheet
    {
        protected XmlDocument characterSheet;

        public string name { protected set; get; }
        public string server { protected set; get; }
        public string sheetUrl { protected set; get; }
        public TallentSpec spec1 { protected set; get; }
        public TallentSpec spec2 { protected set; get; }

        public BaseStats baseStats { protected set; get; }

        public int honorKills { protected set; get; }

        public Resistances resistances { protected set; get; }
        public Melee melee { protected set; get; }
        public Ranged ranged { protected set; get; }
        public Spell spell { protected set; get; }
        public Defenses defenses { protected set; get; }
        public Items items { protected set; get; }
        public Glyphs glyphs { protected set; get; }

        public ArmoryCharacterSheet(BaseStats stats, string name, string server, string Url)
        {
            this.name = name;
            this.server = server;
            this.sheetUrl = Url;
            this.baseStats = stats;
        }
        public void SetSpell(Spell spell){ this.spell = spell;}

        public string ToFormatedString(string input)
        {
            string output = input;
            //Replace the tags for the actual data.
            output = output.Replace("##N##", name);
            if (baseStats != null)
            {
                output = output.Replace("##HP##", baseStats.maxHP.ToString());
                output = output.Replace("##STR##", baseStats.str.effectiveStr.ToString());
                output = output.Replace("##STA##", baseStats.sta.effectiveSta.ToString());
                output = output.Replace("##INT##", baseStats.intel.effectiveInt.ToString());
                output = output.Replace("##SPR##", baseStats.spir.effectiveSpir.ToString());
                output = output.Replace("##AGI##", baseStats.agi.effectiveAgi.ToString());
                output = output.Replace("##ARMOR##", baseStats.armor.effectiveArmor.ToString());
                


                if (baseStats._2ndBar != null)
                {
                    output = output.Replace("##MP##", baseStats._2ndBar.effective.ToString());
                    output = output.Replace("##MP5##", baseStats._2ndBar.casting.ToString());
                }
            }
            output = output.Replace("Longhammer", "Longnubby").Replace("Longdagger", "Longnubby").Replace("Thornstar", "Longnubby"); ;
            return output;
        }
    }

    public class ArmoryCharacter
    {
        public string name { protected set; get; }
        public string server { protected set; get; }
        public byte level { protected set; get; }
        public byte classID { protected set; get; }
        public byte raceID { protected set; get; }
        public bool isMale { protected set; get; }
        public int achPoints { protected set; get; }
        public byte guildRank { protected set; get; }
        public string armoryUrl { protected set; get; }
        public string ClassColor
        {
            get
            {
                switch (classID)
                {
                    case 1://WAR
                        return "#C79C6E";
                    case 2://PAL
                        return "#F58CBA";
                    case 3://HUNTER
                        return "#ABD473";
                    case 4://ROGUE
                        return "#FFF569";
                    case 5://PRIEST
                        return "#FFFFFF";
                    case 6://DK
                        return "#C41F3B";
                    case 7://SHM
                        return "#2459FF";
                    case 8://MAGE
                        return "#69CCF0";
                    case 9://LOCK
                        return "#9482C9";
                    case 10://Not used?
                    case 11://DRUID
                        return "#FF7D0A";
                    default://Black
                        return "#000000";
                }
            }
        }
        //private ArmoryCharacterSheet sheet = null;
        /*public ArmoryCharacterSheet characterSheet
        {

            protected set
            {
                sheet = value;
            }
            get
            {
                if (sheet == null)
                    sheet = RequestData.getCachedCharacterSheet(name, server, armoryUrl);
                return sheet;
            }
        }*/

        //Used by our RequestData class
        public ArmoryCharacter(XmlNode character, string Server) :
            this(Server, character.Attributes["name"].Value, Convert.ToByte(character.Attributes["level"].Value), Convert.ToByte(character.Attributes["classId"].Value),
            Convert.ToByte(character.Attributes["raceId"].Value), (character.Attributes["genderId"].Value == "0"), Convert.ToInt32(character.Attributes["achPoints"].Value),
            Convert.ToByte(character.Attributes["rank"].Value), character.Attributes["url"].Value)
        {
        }
        public ArmoryCharacter(string Server, string Name, byte Level, byte ClassID, byte RaceID, bool IsMale, int AchPoints, byte GuildRank, string ArmoryUrl)
        {
            this.server = Server;
            this.name = Name;
            this.level = Level;
            this.classID = ClassID;
            this.raceID = RaceID;
            this.isMale = IsMale;
            this.achPoints = AchPoints;
            this.guildRank = GuildRank;
            this.armoryUrl = ArmoryUrl;
        }

        public string ToHtml()
        {
            string output = "";
            output += "&nbsp;<img src=\"" + RequestXml.baseUrl + "/_images/icons/race/" + raceID + "-" + (isMale ? "0" : "1") + ".gif\">&nbsp;";
            output += "<img src=\"" + RequestXml.baseUrl + "/_images/icons/class/" + classID + ".gif\">&nbsp;";
            output += (this.level < 80 ? "[" + level + "]" : "") + "<font color=\"" + ClassColor + "\">" + name + "</font>&nbsp;&#9;";
            output += achPoints + "<br />";

            return output;
        }
        /// <summary>
        /// This will format the string with the appropriate data.
        /// </summary>
        /// <param name="input">##N## = name, ##L## = Level, ##C## = Class Icon, ##R## = Race/Gender Icon, ##A## = Achievement points, ##R## = Guild Rank(number), ##U## = Armory URL, ##F## = Font color(by class)</param>
        /// <returns></returns>
        public string ToFormatedString(string input)
        {
            string output = input;
            //Replace the tags for the actual data.
            output = output.Replace("##N##", name);
            output = output.Replace("##L##", level.ToString());
            output = output.Replace("##C##", "<img src=\"" + RequestXml.baseUrl + "/_images/icons/class/" + classID + ".gif\">");
            output = output.Replace("##R##", "<img src=\"" + RequestXml.baseUrl + "/_images/icons/race/" + raceID + "-" + (isMale ? "0" : "1") + ".gif\">");
            output = output.Replace("##A##", achPoints.ToString());
            output = output.Replace("##R##", guildRank.ToString());
            output = output.Replace("##U##", armoryUrl);
            output = output.Replace("##F##", ClassColor);

            output = output.Replace("Longhammer", "Longnubby").Replace("Longdagger", "Longnubby").Replace("Thornstar", "Longnubby"); ;
            return output;
        }

        public static int CompareByLevelAndRank(ArmoryCharacter x, ArmoryCharacter y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the 
                    // lengths of the two strings.
                    //
                    if (x.level > y.level)
                        return -11;
                    else if (x.level == y.level)
                    {//If they are equal sort by rank
                        if (x.guildRank > y.guildRank) return 1;
                        else if (x.guildRank < y.guildRank) return -1;
                        else return 0;
                    }
                    else
                        return 1;
                }
            }
        }
        /*
        public void LoadCharacterSheet()
        {
            if (sheet != null) return;
            sheet = RequestData.getCachedCharacterSheet(this.name, this.server, this.armoryUrl);
        }*/

    }
}
