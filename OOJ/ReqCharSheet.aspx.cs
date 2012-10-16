using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using ArmoryLib;

public partial class ReqCharSheet : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["r"] == null || Request["r"].Length < 4)
            return;
        if (Request["cn"] == null || Request["cn"].Length < 3)
            return;
        XmlDocument CharacterSheet;
        //r=Terenas&amp;cn=Kittencannon
        string server = Request["r"], name = Request["cn"], url;
        url = "r=" + server + "&cn=" +name;

        RequestData.getFreshCharacterSheet(name, server, url, out CharacterSheet);

        if(CharacterSheet != null)
            Response.Write(CharacterSheet.OuterXml.Replace("href=\"/_layout/character/sheet.xsl\"","href=\"http://www.wowarmory.com/_layout/character/sheet.xsl\""));
    }
}
