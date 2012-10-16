using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading;
using ArmoryLib;

public partial class Roster : System.Web.UI.Page
{
    static Guild ooj;
    //public static GuildRoster ooj = null;
    protected void Page_Load(object sender, EventArgs e)
    {
        
        if (!this.IsPostBack)
        {
            if(ooj == null){
                RequestXml.basePath = MapPath("./armory/");
                RequestData.SqlConnectionString = ""; // Omitted for GitHub
                ooj = new Guild("Terenas", "Overlords of Justice");
            }
        }
        //OOJ Roster
        //http://www.wowarmory.com/guild-info.xml?r=Terenas&gn=Overlords+of+Justice&rhtml=n
        BodyText.Text = ooj.roster.ReturnRoster();
        try
        {
            //BodyText.Text = ooj.ReturnRoster();
        }catch 
        {
            BodyText.Text = "Roster is currently loading...";
        }
        if (this.IsPostBack)
        {
        }
        else { }
    }
}
