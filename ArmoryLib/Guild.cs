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
    public class Guild
    {
        public GuildRoster roster { protected set; get; }

        public Guild(string server, string guildName)
        {
            roster = RequestData.getGuildRoster(server, guildName);
        }
    }
    public class GuildRoster
    {
        //XmlDocument guildXML;
        public List<ArmoryCharacter> members { protected set; get; }
        DateTime LoadTime = DateTime.Now;
        ArmoryCharacter mostAch, leastAch;

        //An object to lock on to make this thread safe
        object lockOn = new object();

        public GuildRoster(List<ArmoryCharacter> members)
        {
            this.members = members;
            members.Sort(ArmoryCharacter.CompareByLevelAndRank);
            //Help out and work a Queue
            int sheetQueue = RequestData.WorkRequestQueue(1);
        }

        public string ReturnRoster()
        {
            //Work down some of this queue
            int sheetQueue = RequestData.WorkRequestQueue(1);

            string output = "<div style=\"width:896px;overflow:hidden;text-align:center;vertical-align:top;\"><font size=\"+3\"><center><a>Guild Roster</a></center></font><br />\n";
            output += "<table style=\"vertical-align:top;\"><tr valign=\"top\"><td>\n";
            output += "<table width=\"400\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#333333;\">\n";
            output += "<tr><td width=\"44\">&nbsp;</td><td width=\"200\" style=\"text-align:left;\"><a><b>Name</b></a>&nbsp;</td><td  style=\"text-align:center;\"><a><b>Achievement Points</b></a></td></tr>\n";
            lock (lockOn)
            {
                foreach (ArmoryCharacter c in members)
                {
                    if (c.level >= 80)
                    {
                        //output += c.ToHtml();
                        output += c.ToFormatedString("<tr><td width=\"44\">&nbsp;##R##&nbsp;##C##</td><td width=\"200\" style=\"text-align:left;\"><a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?"+c.armoryUrl+"\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>&nbsp;</td><td  style=\"text-align:center;\"><a>##A##</a></td></tr>\n");
                        if (mostAch == null) mostAch = c;
                        else if (mostAch.achPoints < c.achPoints)
                            mostAch = c;

                        if (leastAch == null) leastAch = c;
                        else if (leastAch.achPoints > c.achPoints)
                            leastAch = c;

                    }
                }
            }
            output += "</table>\n</td><td>\n";
            output += ReturnLeaderBoard();
            output += "</td></tr></table>\n";
            output += "Roster Loaded on " + LoadTime.ToShortDateString() + " " + LoadTime.ToShortTimeString() + " Queue Size: " + sheetQueue + "<br />\n";
            output += "</div>\n";
            return output;
        }
        public string ReturnLeaderBoard()
        {
            string output = "";
            string sql = "";
            output += "<table width=\"400\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#333333;\">\n";
            output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>No life!</b></a></td><td  style=\"text-align:center;\"><a><b>Achievement Points</b></a></td></tr>\n";
            lock (lockOn)
            {
                output += mostAch.ToFormatedString("<tr><td width=\"44\">&nbsp;##R##&nbsp;##C##</td><td width=\"200\" style=\"text-align:left;\"><a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + mostAch.armoryUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>&nbsp;</td><td  style=\"text-align:center;\"><a>##A##</a></td></tr>\n");
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\"><a><b>King of the slackers</b></a></td></tr>\n";
                output += leastAch.ToFormatedString("<tr><td width=\"44\">&nbsp;##R##&nbsp;##C##</td><td width=\"200\" style=\"text-align:left;\"><a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + leastAch.armoryUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>&nbsp;</td><td  style=\"text-align:center;\"><a>##A##</a></td></tr>\n");
                ArmoryCharacterSheet[] sheet = null;
                ArmoryCharacter memberEntry = null; ;
                string fontC = "#FFFFFF";
                string raceClass = "&nbsp;";
                string htmlName = "<font style=\"text-align:left;\">##N##</font>";
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>";
                #region HP
                sheet = RequestData.getTopCharacterSheets("maxHP", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Healthiest members</b></a></td><td style=\"text-align:center;\"><a><b>Max HP</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }
                    
                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"red\">##HP##</font></td></tr>\n");
                } 
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>\n";
                #region MP
                fontC = "#FFFFFF";
                raceClass = "&nbsp;";
                htmlName = "<font style=\"text-align:left;\">##N##</font>";
                memberEntry = null;

                //Shouldn't have to worry about the second bar type.  A caster gets more second bar than a non caster at about level 4.
                sheet = RequestData.getTopCharacterSheets("effectiveSecondBar", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>The Blue Man Group:</b></a></td><td style=\"text-align:center;\"><a><b>Max MP</b></a></td></tr>";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#0000FF\">##MP##</font></td></tr>\n");
                }
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>\n";
                #region STR
                fontC = "#FFFFFF";
                raceClass = "&nbsp;";
                htmlName = "<font style=\"text-align:left;\">##N##</font>";
                memberEntry = null;

                sheet = RequestData.getTopCharacterSheets("effectiveStr", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Strongest members</b></a></td><td style=\"text-align:center;\"><a><b>STR</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##STR##</font></td></tr>\n");
                } 
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>\n";
                #region INT

                sheet = RequestData.getTopCharacterSheets("effectiveInt", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Smartest members</b></a></td><td style=\"text-align:center;\"><a><b>INT</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##INT##</font></td></tr>\n");
                }
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>\n";
                #region AGI

                sheet = RequestData.getTopCharacterSheets("effectiveAgi", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Most agile members</b></a></td><td style=\"text-align:center;\"><a><b>AGI</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##AGI##</font></td></tr>\n");
                }
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>\n";
                #region STA

                sheet = RequestData.getTopCharacterSheets("effectiveSta", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Energizer bunnies</b></a></td><td style=\"text-align:center;\"><a><b>STA</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##STA##</font></td></tr>\n");
                }
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>\n";
                #region SPR

                sheet = RequestData.getTopCharacterSheets("effectiveSpir", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Most spiritual members</b></a></td><td style=\"text-align:center;\"><a><b>SPR</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##SPR##</font></td></tr>\n");
                }
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>";
                #region ARMOR

                sheet = RequestData.getTopCharacterSheets("effectiveArmor", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Thickheaded members</b></a></td><td style=\"text-align:center;\"><a><b>Armor</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##ARMOR##</font></td></tr>\n");
                }
                #endregion
                output += "<tr><td colspan=\"3\" style=\"text-align:left;\">&nbsp;</td></tr>";
                #region mp5
                //secondBarPerFive

                sheet = RequestData.getTopCharacterSheets("secondBarCasting", 3, false, "Overlords of Justice");
                output += "<tr><td colspan=\"2\" style=\"text-align:left;\"><a><b>Highest casting mana regen</b></a></td><td style=\"text-align:center;\"><a><b>Mp5</b></a></td></tr>\n";
                foreach (ArmoryCharacterSheet cs in sheet)
                {
                    fontC = "#FFFFFF";
                    raceClass = "&nbsp;";
                    htmlName = "<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font style=\"text-align:left;\" style=\"text-align:left;\">##N##</font></a>";
                    memberEntry = null;
                    foreach (ArmoryCharacter m in members)
                    {
                        if (m.name == cs.name)
                        {
                            memberEntry = m;
                            break;
                        }
                    }

                    if (memberEntry != null)
                    {
                        fontC = memberEntry.ClassColor;
                        raceClass = memberEntry.ToFormatedString("&nbsp;##R##&nbsp;##C##");
                        htmlName = memberEntry.ToFormatedString("<a href=\"http://overlordsofjustice.com/armory/CharacterSheet.aspx?" + cs.sheetUrl + "\"><font color=\"##F##\" style=\"text-align:left;\">##N##</font></a>");
                    }
                    output += cs.ToFormatedString("<tr><td width=\"44\">" + raceClass + "</td><td width=\"200\" style=\"text-align:left;\">" + htmlName + "&nbsp;</td><td  style=\"text-align:center;\"><font color=\"#00FF00\">##MP5##</font></td></tr>\n");
                }
                #endregion
            }
            output += "</table>\n";
            return output;
        }
    }
}
