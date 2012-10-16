using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Xsl;
using System.Web.UI;
using System.Web.UI.WebControls;
using ArmoryLib;

public partial class Armory_CharacterSheet : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["r"] == null || Request["cn"] == null)
        {
            Response.Write( "<font color=\"red\" size=\"+3\">No Character Information Specified</font>");
        }
        XmlDocument sheet = new XmlDocument();
        RequestData.SqlConnectionString = ""; // Ommited for GitHub

        RequestData.getFreshCharacterSheet(Request["cn"], Request["r"], "r=" + Request["r"] + "&cn=" + Request["cn"], out sheet);
        Response.ContentType = "text/xml";
        Response.Write(sheet.OuterXml.Replace("/_layout/character/sheet.xsl", "http://overlordsofjustice.com/armory/character-sheet.xsl"));
    }
}