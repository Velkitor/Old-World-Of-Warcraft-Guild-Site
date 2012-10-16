using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ArmoryLib;

public partial class BuildRequestQueue : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request["k"] != "L0ngnubby") return;
        RequestData.SqlConnectionString = ""; // Omitted for GitHub

        Guild ooj = new Guild("Terenas", "Overlords of Justice");

        TimeSpan ts = TimeSpan.Zero;
        DateTime lastUpdate = DateTime.Now;
        foreach (ArmoryCharacter c in ooj.roster.members)
        {
            lastUpdate =RequestData.CharacterSheetLastUpdateTime(c.name, c.server);
            ts = DateTime.Now - lastUpdate;
            if (ts > new TimeSpan(2, 0, 0, 0))
            {
                RequestData.AddCharacterSheetToQueue(c.name, c.server, c.armoryUrl);
                Response.Write("*");
            }
            Response.Write(c.server + " " + c.name + " " + lastUpdate.ToShortDateString() + " " + lastUpdate.ToShortTimeString() +"\n");
        }
    }
}
