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
using ArmoryLib;

public partial class PostCharacterSheet : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        //our extrenly secure plain text password
        if (Request["k"] != "L0ngnubby") return;
        if (Request["cn"] == null || Request["cn"].Length < 1) return;
        if (Request["r"] == null || Request["r"].Length < 1) return;

        RequestData.SqlConnectionString = ""; // Omitted for GitHub

        XmlDocument sheet = new XmlDocument();
        string fileName ="";
        foreach (string f in Request.Files.AllKeys)
        {
            HttpPostedFile file = Request.Files[f];
            
            fileName = file.FileName;
            if (fileName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last().ToLower() == "xml")
            {
                StreamReader SR = new StreamReader(file.InputStream);
                sheet.LoadXml(SR.ReadToEnd());
                PostData.PostCharacterSheetXML(sheet, Request["r"], Request["cn"]);

                Response.Write("OK");
                return;
            }
        }
    }
}
