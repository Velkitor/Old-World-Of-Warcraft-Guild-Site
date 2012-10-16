<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Roster.aspx.cs" Inherits="Roster" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Overlords of Justice</title>
    <link rel="STYLESHEET" type="text/css" href="style.css" />
</head>
<body bgcolor="#fdf0cf">
<form id="form1" runat="server">
<div align="center" style="z-index:1;margin-right:auto;margin-left:auto;width:896px;overflow:hidden;text-align:left;" id="mainDiv">
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

    <!-- Body Text -->
    <asp:Label ID="BodyText" runat="server">&nbsp;</asp:Label>
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

