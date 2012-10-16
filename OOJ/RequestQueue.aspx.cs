using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ArmoryLib;

public partial class RequestQueue : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["n"] == null || Request["n"].Length < 1) return;
        int count = 0;
        try{
            count = Convert.ToInt32( Request["n"]);
        }
        catch{ return;}
        //Make sure our SQL connection is setup properly
        RequestData.SqlConnectionString = ""; // Omitted for GitHub

        List<string> req = RequestData.getRequestQueueItems(count);
        foreach (string s in req)
        {
            Response.Write(s + "\n");
        }
    }
}
