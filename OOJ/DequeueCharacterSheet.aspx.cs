using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ArmoryLib;

public partial class Dequeue : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["k"] != "L0ngnubby") return;
        if (Request["cn"] == null || Request["cn"].Length < 1) return;
        if (Request["r"] == null || Request["r"].Length < 1) return;
        RequestData.SqlConnectionString = ""; // Omitted for GitHub

        RequestData.PurgeRequestQueue(RequestData.ReqType.CHARACTER_SHEET, "r=" + Request["r"].Trim() + "&cn=" + Request["cn"].Trim());
        Response.Write("DQ OK" + "\nr=" + Request["r"].Trim() + "&cn=" + Request["cn"].Trim() + "|");
         
    }
}
