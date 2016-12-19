﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace uLearn.Web.Views.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using uLearn.Web;
    using uLearn.Web.Extensions;
    using uLearn.Web.Models;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public static class UserAvatar
    {

public static System.Web.WebPages.HelperResult GetAvatarPlaceholderColor(ApplicationUser user)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 
	var hue = Math.Abs(user.Id.GetHashCode()) % 360;

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t");

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "hsl(");


WebViewPage.WriteTo(@__razor_helper_writer, hue);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, ", 64%, 75%)");

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\r\n");



});

}


public static System.Web.WebPages.HelperResult GetAvatarPlaceholderLetter(ApplicationUser user)
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t");


WebViewPage.WriteTo(@__razor_helper_writer, char.ToUpper(user.VisibleName.FindFirstLetter('O')));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\r\n");



});

}


public static System.Web.WebPages.HelperResult Avatar(ApplicationUser user, string classes="")
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 
	if (user.HasAvatar)
	{

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t\t<img class=\"user__avatar ");


WebViewPage.WriteTo(@__razor_helper_writer, classes);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\" src=\"");


WebViewPage.WriteTo(@__razor_helper_writer, user.AvatarUrl);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\" alt=\"");


                     WebViewPage.WriteTo(@__razor_helper_writer, HttpUtility.HtmlAttributeEncode(user.VisibleName));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\" />\r\n");


	}
	else
	{

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\t\t<div class=\"user__avatar user__avatar__placeholder ");


          WebViewPage.WriteTo(@__razor_helper_writer, classes);

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\" style=\"background-color: ");


                                             WebViewPage.WriteTo(@__razor_helper_writer, GetAvatarPlaceholderColor(user));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "\">");


                                                                               WebViewPage.WriteTo(@__razor_helper_writer, GetAvatarPlaceholderLetter(user));

WebViewPage.WriteLiteralTo(@__razor_helper_writer, "</div>\r\n");


	}

});

}


public static System.Web.WebPages.HelperResult SmallAvatar(ApplicationUser user, string classes="")
{
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {


 
	
WebViewPage.WriteTo(@__razor_helper_writer, Avatar(user, "small " + classes));

                                  ;

});

}


    }
}
#pragma warning restore 1591
