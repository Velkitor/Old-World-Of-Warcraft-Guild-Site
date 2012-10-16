using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Xml;
using ArmoryLib;

public partial class UploadCharacterSheet : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }
    protected void CharacterSheet_Click(object sender, EventArgs e)
    {
        //If there is no logged in user hide the form and return.
        if (Request["k"] != "L0ngnubby")
        {
            return;
        }
        
        if (CharacterSheetXML.PostedFile.FileName == null || CharacterSheetXML.PostedFile.FileName == "")
            return; //No file?
        #region File Extension Check
        string[] split = CharacterSheetXML.PostedFile.FileName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        switch (split[split.Length - 1].ToLower())
        {
            case "xml":
                break;
            default://I don't know this extension
                return;
        }

        #endregion
        string fileName = Guid.NewGuid().ToString() + "." + split[split.Length - 1].ToLower();
        //Write the file to disk
        StreamReader SR = new StreamReader(CharacterSheetXML.PostedFile.InputStream);
        XmlDocument sheet = new XmlDocument();
        sheet.LoadXml(SR.ReadToEnd());

        BaseStats baseStats = null;
        int MaxHP;

        XmlNodeList xList, xList2;
        
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
        Response.Write(baseStats._2ndBar.effective.ToString() + " " + baseStats._2ndBar.Casting + " " + baseStats._2ndBar.NotCasting + " " + baseStats._2ndBar.PerFive);

        RequestData.SqlConnectionString = ""; // Omitted for Git Hub
        PostData.PostCharacterSheetXML(sheet, Request["r"], Request["cn"]);
    }
}
