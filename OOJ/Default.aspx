<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" ValidateRequest="false"%>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Overlords of Justice</title>
    <link rel="STYLESHEET" type="text/css" href="style.css" />
</head>
<body bgcolor="#fdf0cf">
<form id="form1" runat="server">
<div align="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:auto;text-align:left;" id="mainDiv">
    <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-image:url('/img/toolbar_bg.jpg');vertical-align:middle;" id="AdminCP" runat="server">
        <a>Admin CP:</a>&nbsp;&nbsp;
        <asp:ImageButton ID="SubmitNewsButton" runat="server" ImageUrl="./img/adminCP/addNews.gif" onclick="SubmitNewsButton_Click" />&nbsp;&nbsp;
        <asp:ImageButton ID="CreateUserButton" runat="server" ImageUrl="./img/adminCP/addUser.gif" onclick="CreateUserButton_Click" />&nbsp;&nbsp;
        <asp:ImageButton ID="CreateGallery" runat="server" ImageUrl="./img/adminCP/addGallery.gif" onclick="CreateGalleryButton_Click" />
        <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;height:4px;">&nbsp;</div>
    </div>
    <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-image:url('/img/toolbar_bg.jpg');">
        <a href="http://overlordsofjustice.com/"><img src="./img/home.gif" border="0" /></a>&nbsp;&nbsp;
        <a href="http://overlordsofjustice.com/roster.aspx"><img src="./img/roster.gif" border="0" /></a>&nbsp;&nbsp;
        <a href="http://overlordsofjustice.com/?g=t"><img src="./img/gallery.gif" border="0" /></a>&nbsp;&nbsp;
        <a href="http://overlordsofjustice.com/forum/"><img src="./img/forum.gif" border="0" /></a>
    </div>
    <div style="text-align:center;"><img src="./img/banner.gif" style="height: 183px" alt="" />
        <div align="center" style="position:relative; top:-65px; left:-425px; z-index:3;">
            <div style="position:relative; left:800px; width:100px;"><asp:Label ID="LoggedInUser" runat="server" style="text-shadow:black;"></asp:Label></div>
        </div>
    </div>
    <div runat="server" style="text-align:center;" id="LoginDiv">
    <!-- Login Area -->
        <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;"><a>Log In</a></div>        
        Username: <asp:TextBox ID="username" runat="server"></asp:TextBox><br />
        Password: <asp:TextBox ID="password" runat="server" TextMode="Password"></asp:TextBox><br />
        <asp:Button ID="LogIn" Text="Log In" runat="server" onclick="LogIn_Click" /><br />
        <asp:Label ID="LoginLabel" runat="server"></asp:Label>
    </div>
    
    <div runat="server" style="text-align:center;" id="CreateUserDiv">
        <!-- Create User Area -->
        <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;"><a>Create User</a></div>        
        Username: <asp:TextBox ID="CreateUN" runat="server"></asp:TextBox><br />
        Password: <asp:TextBox ID="CreatePW" runat="server"></asp:TextBox><br />
        <asp:Button ID="CreateUserSubmit" Text="Create" runat="server" onclick="CreateUser_Click" /><br />
        <asp:Label ID="CreateUserLabel" runat="server"></asp:Label>
    </div>
    
    <div id="CreateGalleryDiv" runat="server">
        <!-- Create Gallery Form -->
        <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;"><a>CreateGallery</a></div>        
        Title: <asp:TextBox ID="CreateGalleryTitle" runat="server" Width="500"></asp:TextBox><br />
        Short Name (4 letters): <asp:TextBox ID="CreateGalleryShort" runat="server" Width="50"></asp:TextBox><br />
        Body:<br />
        <asp:TextBox ID="CreateGalleryDesc" runat="server" TextMode="MultiLine" Width="800" Height="150"></asp:TextBox><br />
        <center></center><asp:Button ID="CreateGallerySubmit" Text="Create Gallery" runat="server" onclick="CreateGallerySubmit_Click" /></center><br />
        <asp:Label ID="CreateGalleryLabel" runat="server"></asp:Label>
    </div>

    
    <div id="SubmitNews" runat="server">
        <!-- Submit News Form -->
        <div align ="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;background-color:#111111;"><a>News Submission</a></div>        
        Title: <asp:TextBox ID="TitleTextBox" runat="server" Width="500"></asp:TextBox><br />
        Body:<br />
        <asp:TextBox ID="BodyTextBox" runat="server" TextMode="MultiLine" Width="800" Height="300"></asp:TextBox><br />
        <asp:CheckBox ID="NewsUpdate" runat="server" Checked="false" Visible="false" /><asp:Label ID="NewsIndex" runat="server" Visible="false">&nbsp;</asp:Label>
        <center><asp:Button ID="NewsSubmit" Text="Submit News" runat="server" onclick="NewsSubmit_Click" /></center><br />
        <asp:Label ID="NewsLabel" runat="server"></asp:Label>
    </div>
    <!-- Body Text -->
    <asp:Label ID="BodyText" runat="server">&nbsp;</asp:Label>
    
    <div id="GalleryForm" runat="server">
        <!-- Gallery submit image form -->
        File: <asp:FileUpload ID="GalleryImgUploader" runat="server" /><br />
        Description: <asp:TextBox ID="GalleryDescription" runat="server"></asp:TextBox><br />
        <asp:Button ID="GallerySubmit" runat="server" Text="Submit Picture" 
            onclick="GallerySubmit_Click" /><br />
        <asp:Label ID="GalleryFormError" runat="server">&nbsp;</asp:Label>
    </div>
</div>

<br />
<div align="center" style="margin-right:auto;margin-left:auto; margin-bottom:0px;">
Please feel free to donate.<br />
All funds will go towards the website and any other OOJ related services.<br />
<a href="http://tinyurl.com/donateooj"><img alt="" border="0" src="http://www.paypal.com/en_US/i/btn/btn_donate_LG.gif"></a>
</div>
</form>
</body>
</html>

