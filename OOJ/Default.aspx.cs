using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.Sql;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Data.SqlClient;


public partial class _Default : System.Web.UI.Page
{
    private static string conString = conString;
    protected void Page_Load(object sender, EventArgs e)
    {
        //Hide all my dynamic Divs on the first load
        if(!this.IsPostBack)
            HideAll();
        //If we have a user stored in a cookie and not the session... Add it to the session.
        try
        {
            if (Session["ooj_user"] == null || Session["ooj_user"] == "")
            {
                foreach (HttpCookie cookie in Response.Cookies)
                {
                    if (cookie.Name == "oojuser")
                        if (cookie.Value != null && cookie.Value != "")
                        {
                            Session["ooj_user"] = cookie.Value;
                            ApplyLoggedInName((string)Session["ooj_user"]);
                        }
                }
            }
        }
            //IE is giving me some strange error about converting a HttpCookie to a string.
            //The code above shouldn't have any issues with that, so I am just playing it safe.
        catch { }

        //If we have a logged in user in the session
        if (Session["ooj_user"] != null && ((string)Session["ooj_user"]).Length > 2)
        {
            ApplyLoggedInName((string)Session["ooj_user"]);
            LoginDiv.Visible = false;
            if (Request["n"] != null && Request["n"].Contains("t"))
            {
                SubmitNews.Visible = true;
            }
            if(IsAdmin(Session["ooj_user"].ToString())){
                AdminCP.Visible = true;
            }
            
        }//Not logged in
        else
        {
            ApplyLoginLink();
            if(Request["l"] != null && Request["l"].Contains("t"))
                LoginDiv.Visible = true;
        }
        
        //If it is a reload of the page.
        if (Page.IsPostBack)
        {
        }
        //First load of the page
        else
        {
            if (Request["nu"] != null && Request["nu"].Contains("t"))
            {
                int result=0;
                if(Request["idx"] != null && int.TryParse(Request["idx"], out result))
                {
                    NewsUpdate.Checked = true;
                    NewsIndex.Text = result.ToString();
                    SubmitNews.Visible = true;
                    PopulateNewsForm(result);
                }
            }
            //Gallery
            if (Request["g"] != null && Request["g"].Contains("t"))
            {
                //Was a short gallery name provided?
                if (Request["short"] != null)
                {
                    LoadGallery(Request["short"].ToString());
                }
                //Nope, just load the gallery list
                else
                {
                    LoadGallery();
                }
            }
            //Log out
            else if (Request["lo"] != null && Request["lo"].Contains("t"))
            {
                LogOut();
            }
            //All else fails load the news
            else
            {
                if (Request["nd"] != null)
                {
                    try
                    {
                        DateTime dt = Convert.ToDateTime(Request["nd"]);
                        LoadNews(dt);
                        LoadNewsHistoryLinks();
                    }
                    catch
                    {//Bad dates returns the normal news view
                        LoadNews();
                        LoadNewsHistoryLinks();
                    }
                }
                else
                {
                    LoadNews();
                    LoadNewsHistoryLinks();
                }
            }
        }
    }
    protected void ApplyLoginLink()
    {
        LoggedInUser.Text = "<font color=\"#eee114\"><a href=\"http://overlordsofjustice.com/?l=t\">Log In</a></font>";
    }
    protected void ApplyLoggedInName(string name)
    {
        if(name == null) return;
        LoggedInUser.Text = "<font color=\"#eee114\">" + name + "</font> <a href=\"http://overlordsofjustice.com/?lo=t\">[<font color=\"red\">x</font>]</a>";
    }
    //Utility functions
    protected string BuildNewsDiv(string Title, string Body, string Author, DateTime Time,int idx)
    {
        bool isAdmin = false;
        if (Session["ooj_user"] != null)
        {//Are they an admin?
            isAdmin = this.IsAdmin(Session["ooj_user"].ToString());
        }
        
        
        string output = "";
        output += "\t\t<div style=\"margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;\">\n";
        if(isAdmin)
            output += "\t\t\t<a href=\"http://overlordsofjustice.com/?nu=t&idx=" + idx.ToString() +"\"><font size=-1 color=\"#ded104\">[</font><font size=-2 color=\"#FF0000\">edit</font><font size=-1 color=\"#ded104\">]</font></a>\n";
        output += "\t\t\t<font size=+2 color=\"#ded104\"><b>" + Title + "</b></font>\n";
        output += "\t\t</div><br />\n";
        output += Body + "<br />\n";
        output += "<div style=\"text-align:right;width:896px;overflow:hidden;\"><font size=\"+1\" align=\"right\">-" + Author + " " + Time.ToShortDateString() + "</font></div><br /><br />\n";
        return output;
    }
    protected string BuildGalleryLink(string shortName, string Title, string Desc)
    {
        string output = "";
        output += "\t\t<div>\n";
        output += "\t\t\t<a class=\"gallery\" href=\"http://overlordsofjustice.com/?g=t&short=" + shortName + "\">" + Title + "</a><br />\n";
        if(Desc != null)
            output += Desc + "<br />\n";
        output += "\t\t</div><br />\n";
        return output;
    }
    protected string BuildGalleryImg(string fileName, string desc, string submitBy, DateTime when, string galleryShort)
    {
        string output = "";
        output += "\t\t<td width=\"220\">\n";
        //Do I have a thumbnail for this?
        output += "\t\t\t<a href=\"/gallery/" + galleryShort + "/" + fileName + "\">\n";
        //Have to use MapPath to use the relative virtual directory path.
        if (File.Exists(MapPath("./gallery/" + galleryShort +"/t_" + fileName)))
        {
            output += "\t\t\t<img border=\"0\" src=\"/gallery/"+galleryShort +"/t_"+ fileName +"\"><br />\n";
        }
        else
        {
            output += "\t\t\t<img border=\"0\" src=\"/gallery/"+galleryShort + "/" + fileName + "\" width=\"210\"><br />\n";
        }
        output += "\t\t\t</a>\n";
        output += "\t\t\tBy: " + submitBy + " at: " + when.ToShortDateString() + "<br />\n";
        output += "\t\t\t" + desc + "\n";
        output += "\t\t</td>\n";
        return output;
    }
    protected bool IsAdmin(string userName)
    {
        bool isAdmin = false;
        if (userName == null)
            return isAdmin;
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("@UN", SqlDbType.VarChar, 32).Value = userName;
                cmd.CommandText = "SELECT * FROM ooj_users WHERE [username] = @UN and [rights] = 1";
                SqlDataReader rd = cmd.ExecuteReader();
                if (rd.HasRows)
                {
                    isAdmin = true;
                }
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch
        {
            isAdmin = false;
        }
        return isAdmin;
    }
    protected void HideAll()
    {
        SubmitNews.Visible      = false;
        LoginDiv.Visible        = false;
        GalleryForm.Visible     = false;
        AdminCP.Visible         = false;
        CreateUserDiv.Visible   = false;
        CreateGalleryDiv.Visible= false;
    }
    //Functions for form submissions
    protected void LogIn_Click(object sender, EventArgs e)
    {
        //Clear the error label
        LoginLabel.Text = "";;
        if (username.Text.Length < 3 || username.Text.Length > 32)
        {
            LoginLabel.Text += "<font color=red>Invalid Username!</font>";
            return;
        }
        if (password.Text.Length < 3 || password.Text.Length > 60)
        {
            LoginLabel.Text += "<font color=red>Invalid Password!</font>";
            return;
        }
        SqlConnection conn = new SqlConnection(conString);
        conn.Open();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            //Set up our paramaters
            cmd.Parameters.Add("@UN", SqlDbType.VarChar, 32).Value = username.Text.Trim();
            cmd.Parameters.Add("@PW", SqlDbType.VarChar, 64).Value = password.Text.Trim();
            //Bind them to the query
            cmd.CommandText = "select * from ooj_users where username = @UN and password = CONVERT(varchar(256), HashBytes('SHA1', @PW),1)";
            //Execute
            SqlDataReader rd = cmd.ExecuteReader();
            //See if we have users
            if (rd.HasRows)
            {
                LoginLabel.Text = "Logged in?" + username.Text.Trim();
                Session.Add("ooj_user", username.Text.Trim());
                HttpCookie cookie = new HttpCookie("oojuser",username.Text.Trim());
                cookie.Name = "oojuser";
                cookie.Expires = DateTime.Now.AddDays(30);
                Response.Cookies.Add(cookie);
                ApplyLoggedInName(username.Text.Trim());
                LoginDiv.Visible = false;
            }
            rd.Close();
        }
        conn.Close();
        Response.Redirect("http://overlordsofjustice.com/");
    }
    protected void LogOut()
    {
        Response.Cookies["oojuser"].Expires = DateTime.Now.AddDays(-1);
        Session["ooj_user"] = null;
        Response.Redirect("http://overlordsofjustice.com/");
    }
    protected void NewsSubmit_Click(object sender, EventArgs e)
    {
        if (Session["ooj_user"] == null)
        {//Should never get here this way but if it does...
         //Do not let a non logged in user submit news!
            NewsLabel.Text = "<font color=red>No valid logged in user!</font>";
            return;
        }
        //Are they an admin?
        if (!this.IsAdmin(Session["ooj_user"].ToString())) return;
        //Clear the error label
        NewsLabel.Text = "";
        if (BodyTextBox.Text.Length < 1)
        {
            NewsLabel.Text += "<font color=red>Please enter a body for this message!</font>";
            return;
        }
        SqlConnection conn = new SqlConnection(conString);
        conn.Open();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            //Set up our paramaters
            if(TitleTextBox.Text != null)
                cmd.Parameters.Add("@TITLE", SqlDbType.VarChar, 1024).Value = TitleTextBox.Text.Trim();
            else
                cmd.Parameters.Add("@TITLE", SqlDbType.VarChar, 1024).Value = "&nbsp;";
            cmd.Parameters.Add("@DATE", SqlDbType.DateTime,8).Value = DateTime.Now;
            cmd.Parameters.Add("@USER", SqlDbType.VarChar, 32).Value = Session["ooj_user"];
            //Yeah that should give us enough space for about anything
            cmd.Parameters.Add("@BODY", SqlDbType.VarChar, 8000).Value = BodyTextBox.Text;
            //Bind them to the query
            if (NewsUpdate.Checked)
            {
                cmd.Parameters.Add("@IDX", SqlDbType.Int).Value = Convert.ToInt32(NewsIndex.Text);
                cmd.CommandText = "UPDATE ooj_news SET [title] = @TITLE, [body] = @BODY WHERE [index] = @IDX";
            }
            else
            {
                cmd.CommandText = "INSERT INTO [ooj_news]([title] ,[dateSubmit] ,[byWho] ,[body]) VALUES (@TITLE,@DATE,@USER,@BODY)";
            }
            //Execute
            cmd.ExecuteNonQuery();
         }
        conn.Close();
        SubmitNews.Visible = false;
        Response.Redirect("http://overlordsofjustice.com/");
    }
    public Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
    {
        Bitmap result = new Bitmap(nWidth, nHeight);
        using (Graphics g = Graphics.FromImage((System.Drawing.Image)result))
            g.DrawImage(b, 0, 0, nWidth, nHeight);
        return result;
    }
    //Load the body contents
    protected void LoadNews()
    {
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT top 5 * FROM ooj_news ORDER BY dateSubmit desc";
                SqlDataReader rd = cmd.ExecuteReader();
                if (rd.HasRows)
                {
                    BodyText.Text = "";
                    while (rd.Read())
                    {
                        BodyText.Text += BuildNewsDiv(rd.GetString(1), rd.GetString(4), rd.GetString(3), rd.GetDateTime(2), rd.GetInt32(0));
                    }
                }
                else
                {
                    BodyText.Text = "<font size=+2>No news!</font>";
                }
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch
        {
            BodyText.Text = "<font size=+2 color=red><b>Unable to connect to the news database.</b></font>";
        }
    }
    protected void LoadNews(DateTime dt)
    {
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("@MINDATE", SqlDbType.DateTime).Value = dt;
                cmd.Parameters.Add("@MAXDATE", SqlDbType.DateTime).Value = dt.AddDays(1);
                cmd.CommandText = "SELECT top 5 * FROM ooj_news WHERE dateSubmit >= @MINDATE and dateSubmit <= @MAXDATE ORDER BY dateSubmit desc";
                SqlDataReader rd = cmd.ExecuteReader();
                if (rd.HasRows)
                {
                    BodyText.Text = "";
                    while (rd.Read())
                    {
                        BodyText.Text += BuildNewsDiv(rd.GetString(1), rd.GetString(4), rd.GetString(3), rd.GetDateTime(2), rd.GetInt32(0));
                    }
                }
                else
                {
                    BodyText.Text = "<font size=+2>No news!</font>";
                }
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch
        {
            BodyText.Text = "<font size=+2 color=red><b>Unable to connect to the news database.</b></font>";
        }
    }
    protected void LoadNewsHistoryLinks()
    {
        List<DateTime> dates = new List<DateTime>();
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT dateSubmit FROM ooj_news order by dateSubmit desc";
                SqlDataReader rd = cmd.ExecuteReader();
                if (rd.HasRows)
                {
                    while (rd.Read())
                    {
                        dates.Add(rd.GetDateTime(0));
                    }
                }
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch
        {
        }
        BodyText.Text += "\t\t<div style=\"margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;\">\n";
        BodyText.Text += "\t\t\t<font size=+2 color=\"#ded104\"><b>Archived News Articles</b></font>\n";
        BodyText.Text += "\t\t</div><br />\n";
        
        int i = 0;
        foreach (DateTime d in dates)
        {
            if (i % 9 == 0)
            {
                if (i > 0) BodyText.Text += "</div>";
                BodyText.Text += "<div style\"test-align:center;\">";
            }
            BodyText.Text += "<div style=\"float: left; width:99px;\"><a href=\"http://overlordsofjustice.com/?nd=" + d.ToShortDateString() + "\">" + d.ToShortDateString() + "</a></div>";
            i++;
        }
        BodyText.Text += "</div><br />";
    }
    protected void PopulateNewsForm(int idx)
    {
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("@IDX", SqlDbType.Int).Value = idx;
                cmd.CommandText = "SELECT top 1 * FROM ooj_news WHERE [index] = @IDX";

                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.HasRows)
                    {
                        rd.Read();
                        for (int i = 0; i < rd.FieldCount; i++)
                        {
                            switch (rd.GetName(i))
                            {
                                case "title":
                                    TitleTextBox.Text = rd.GetString(i);
                                    break;

                                case "body":
                                    BodyTextBox.Text = rd.GetString(i);
                                    break;
                            }
                        }
                    }
                }
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch
        {
        }
    }
    protected void LoadGallery()
    {
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ooj_galleries ORDER BY title asc";
                SqlDataReader rd = cmd.ExecuteReader();
                if (rd.HasRows)
                {
                    BodyText.Text = "";
                    string descript = "";
                    while (rd.Read())
                    {
                        descript = "";
                        if (!rd.IsDBNull(3))
                            descript = rd.GetString(3);
                        BodyText.Text += BuildGalleryLink(rd.GetString(1), rd.GetString(2), descript) + "\n";
                    }
                }
                else
                {
                    BodyText.Text = "<font size=+2>No galleries!</font>";
                }
                rd.Close();
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch(Exception e)
        {
            BodyText.Text = "<font size=+2 color=red><b>Unable to connect to the database.</b></font>";
            //BodyText.Text += "<br />" + e.Message;
        }
    }
    //Override LoadGallery to take a short name of a gallery and load it.
    protected void LoadGallery(string shortName)
    {
        //Nothing to see here!
        if(shortName == null) return;
        string SName = "";
        if (shortName.Length > 4)
            SName = shortName.Substring(0, 4);
        else
            SName = shortName;
        BodyText.Text = "";
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            string GalleryName ="", GalleryDesc = "";

            //Get the gallery details
            #region Gallery Details
            try
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Parameters.Add("@SHORT", SqlDbType.VarChar, 4).Value = SName;
                    cmd.CommandText = "SELECT * FROM ooj_galleries WHERE shortName = @SHORT";

                    SqlDataReader rd = cmd.ExecuteReader();
                    if (rd.HasRows)
                    {
                        rd.Read();
                        GalleryName = rd.GetString(2);
                        if (!rd.IsDBNull(3))
                            GalleryDesc = rd.GetString(3);
                        BodyText.Text += BuildGalleryLink(shortName, GalleryName, GalleryDesc);
                    }
                    rd.Close();
                }
            }
            //I don't care if we can get this or not.  Shouldn't stop our page
            catch { } 
            #endregion
            
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("@SHORT", SqlDbType.VarChar, 4).Value = SName;
                cmd.CommandText = "SELECT * FROM ooj_galleryPhotos WHERE galleryShort = @SHORT ORDER BY [when] desc ";
                SqlDataReader rd = cmd.ExecuteReader();
                if (rd.HasRows)
                {
                    string descript = "";
                    int count = 0;
                    BodyText.Text += "\t\t<div>\n";
                    BodyText.Text += "\t\t<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\n";
                    while (rd.Read())
                    {
                        //Deal with end of rows and beginning of rows
                        if (count % 4 == 0)
                        {
                            if (count != 0)
                                BodyText.Text += "\t\t</tr>\n";
                            BodyText.Text += "\t\t<tr>\n";
                        }
                        //Build a GalleryImg cell
                        if (!rd.IsDBNull(5))
                            descript = rd.GetString(5);
                        BodyText.Text += BuildGalleryImg(rd.GetString(2),descript,rd.GetString(3),rd.GetDateTime(4),SName);
                        count++;
                    }
                    //Figure out if I need to close out a row.
                    if (count % 4 != 0)
                        BodyText.Text += "\t\t<td colspan=\"" + (4 -count%4) + "\">&nbsp;</td></tr>\n";
                    BodyText.Text += "\t\t</table>\n";
                    BodyText.Text += "\t\t</div>\n";
                }
                else
                {
                    BodyText.Text += "<font size=+2>No Pictures in this gallery.</font>";
                }
                rd.Close();
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch (Exception e)
        {
            BodyText.Text = "<font size=+2 color=red><b>Unable to load this gallery.</b></font>";
            //BodyText.Text += "<br />" + e.Message;
        }
        //If we have an OOJ user allow us to see the gallery.
        if (Session["ooj_user"] != null)
        {
            GalleryForm.Visible = true;
            return;
        }
        
    }
    protected void GallerySubmit_Click(object sender, EventArgs e)
    {
        //If there is no logged in user hide the form and return.
        if (Session["ooj_user"] == null)
        {
            GalleryForm.Visible = false;
            return;
        }
        GalleryFormError.Text = "";
        if(GalleryImgUploader.PostedFile.FileName == null || GalleryImgUploader.PostedFile.FileName == "")
            return; //No file?
        #region File Extension Check
        string[] split = GalleryImgUploader.PostedFile.FileName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        switch (split[split.Length - 1].ToLower())
        {
            case "jpg":
            case "jpeg":
            case "gif":
            case "bmp"://sure, but this is a dumb format
            case "tga":
            case "png":
                break;
            default://I don't know this extension
                return;
        }
        
        #endregion
        string fileName = Guid.NewGuid().ToString() + "." + split[split.Length-1].ToLower();
        //Write the file to disk
        GalleryImgUploader.PostedFile.SaveAs(MapPath("/gallery/" + Request["short"] + "/" + fileName));
        //Add this picture into the database!
        #region SQL Insert
        try
        {
            SqlConnection conn = new SqlConnection(conString);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                string desc = "";
                if (GalleryDescription.Text != null)
                {
                    if (GalleryDescription.Text.Length >= 1024)
                        desc = GalleryDescription.Text.Substring(0, 1023);
                    else
                        desc = GalleryDescription.Text;
                }
                string SName = Request["short"];
                if (SName == null || SName == "")
                    SName = "misc";
                if (SName.Length > 4)
                    SName = SName.Substring(0, 4);
                cmd.Parameters.Add("@SHORT", SqlDbType.VarChar, 4).Value = SName;
                cmd.Parameters.Add("@FILENAME", SqlDbType.VarChar, 100).Value = fileName;
                cmd.Parameters.Add("@USER", SqlDbType.VarChar, 32).Value = Session["ooj_user"];
                cmd.Parameters.Add("@DATE", SqlDbType.DateTime, 8).Value = DateTime.Now;
                cmd.Parameters.Add("@DESC", SqlDbType.VarChar, 1024).Value = desc;

                cmd.CommandText = "INSERT INTO [ooj_galleryPhotos]([galleryShort] ,[fileLoc] ,[submitBy] ,[when], [desc]) VALUES (@SHORT,@FILENAME,@USER,@DATE,@DESC)";
                cmd.ExecuteNonQuery();
            }
            conn.Close();
        }//Catch any SQL errors, server down etc
        catch(Exception ex)
        {
            GalleryFormError.Text = "<font size=+2 color=red><b>Unable to insert the picture record into the database.  Please notify the webmaster so that it can be manually added.</b></font>";
            BodyText.Text += "<br />" + ex.Message;
        }
        #endregion
        //Try and build a thumbnail
        #region Thumbnail Conversion
        try
        {
            using (Bitmap bmp = new Bitmap(MapPath("/gallery/" + Request["short"] + "/" + fileName)))
            {
                //Is it too big?
                if (bmp.Width > 210)
                {
                    //Target is 210px to fit nicely in our gallery cells
                    float w = 0, h = 0, ratio = 1f;

                    ratio = 210f / (float)bmp.Width;
                    h = bmp.Height * ratio;
                    w = 210;

                    using (Bitmap thumb = this.ResizeBitmap(bmp, (int)w, (int)h))
                    {
                        thumb.Save(MapPath("/gallery/" + Request["short"] + "/t_" + fileName));
                    }
                }
            }

        }
        catch { } 
        #endregion

        Response.Redirect("http://overlordsofjustice.com/?g=t&short=" + Request["short"]);
    }
    protected void SubmitNewsButton_Click(object sender, ImageClickEventArgs e)
    {
        SubmitNews.Visible = !SubmitNews.Visible;
    }
    protected void CreateUserButton_Click(object sender, ImageClickEventArgs e)
    {
        CreateUserDiv.Visible = !CreateUserDiv.Visible;
    }
    protected void CreateGalleryButton_Click(object sender, ImageClickEventArgs e)
    {
        CreateGalleryDiv.Visible = !CreateGalleryDiv.Visible;
    }
    protected void CreateUser_Click(object sender, EventArgs e)
    {
        if (Session["ooj_user"] == null)
        {//Should never get here this way but if it does...
            //Do not let a non logged in user submit news!
            CreateUserDiv.Visible = false;
            return;
        }
        if (!this.IsAdmin(Session["ooj_user"].ToString())) return;
        //Clear the error label
        CreateUserLabel.Text = "";
        SqlConnection conn = new SqlConnection(conString);
        conn.Open();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Parameters.Add("@UN", SqlDbType.VarChar, 32).Value = CreateUN.Text.Trim();
            cmd.Parameters.Add("@PW", SqlDbType.VarChar, 64).Value = CreatePW.Text.Trim();
            //Bind them to the query
            cmd.CommandText = "INSERT INTO [ooj_users]([username] ,[password]) VALUES (@UN, CONVERT(varchar(256), HashBytes('SHA1', @PW),1))";
            try
            {
                //Execute
                cmd.ExecuteNonQuery();
                CreateUserLabel.Text = "<font color=\"blue\">Created user " + CreateUN.Text.Trim() + ".</font>";
            }
            catch 
            {
                CreateUserLabel.Text = "<font color=\"red\">Unable to create user " + CreateUN.Text.Trim() + ".</font>";
            }
        }
        conn.Close();
    }
    protected void CreateGallerySubmit_Click(object sender, EventArgs e)
    {
        if (Session["ooj_user"] == null)
        {//Should never get here this way but if it does...
            //Do not let a non logged in user submit news!
            CreateUserDiv.Visible = false;
            return;
        }
        if (!this.IsAdmin(Session["ooj_user"].ToString())) return;
        //Clear the error label
        CreateGalleryLabel.Text = "";

        //Validate the data
        bool error = false;
        if (CreateGalleryShort.Text == null || CreateGalleryShort.Text.Length < 1){
            CreateGalleryLabel.Text += "<font color=\"red\">Invalid gallery short name.</font>";
            error = true;
        }
        if (CreateGalleryShort.Text.Length > 4)
        {
            CreateGalleryLabel.Text += "<font color=\"red\">Invalid gallery short name (too long).</font>";
            error = true;
        }
        if (CreateGalleryTitle.Text == null || CreateGalleryTitle.Text.Length < 1)
        {
            CreateGalleryLabel.Text += "<font color=\"red\">Invalid gallery title.</font>";
            error = true;
        }
        if (CreateGalleryTitle.Text.Length > 100)
        {
            CreateGalleryLabel.Text += "<font color=\"red\">Invalid gallery title (too long).</font>";
            error = true;
        }
        if (error) return;
        if (CreateGalleryDesc.Text != null && CreateGalleryDesc.Text.Length > 1024)
        {
            CreateGalleryLabel.Text += "<font color=\"red\">Invalid gallery description (too long).</font>";
            error = true;
        }
        string desc = "";
        if (CreateGalleryDesc.Text != null && CreateGalleryDesc.Text.Length > 0)
            desc = CreateGalleryDesc.Text;

        SqlConnection conn = new SqlConnection(conString);
        conn.Open();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Parameters.Add("@SHORT", SqlDbType.VarChar, 4).Value    = CreateGalleryShort.Text;
            cmd.Parameters.Add("@TITLE", SqlDbType.VarChar, 100).Value  = CreateGalleryTitle.Text;
            cmd.Parameters.Add("@DESC", SqlDbType.VarChar, 1024).Value  = desc;
            //Bind them to the query
            cmd.CommandText = "INSERT INTO [ooj_galleries]([shortName] ,[title], [desc]) VALUES (@SHORT,@TITLE,@DESC)";
            try
            {
                //Execute
                cmd.ExecuteNonQuery();
                CreateGalleryLabel.Text = "<font color=\"blue\">Created gallery " + CreateGalleryTitle.Text.Trim() + ".</font>";
            }
            catch 
            {
                CreateGalleryLabel.Text = "<font color=\"red\">Unable to create gallery " + CreateGalleryTitle.Text.Trim() + ".</font>";
            }
        }
        conn.Close();
    }
    
}
