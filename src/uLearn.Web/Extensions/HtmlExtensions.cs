﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Xml.Linq;
using System.Xml.XPath;

namespace uLearn.Web.Extensions
{
	public static class HtmlExtensions
	{
		private const string urlRegexS = @"(
											(?<protocol>(https?|ftps?)://|mailto:)?
											(?<usernameAndPassword>
											 [-a-z0-9_.+]+
											 (:[-a-z0-9_.]+)?
											 @h
											)?
											(?<hostnameOrIpAddress>
											 (([-a-z0-9_]{2,}\.)+[a-z]{2,})
											 |
											 ((?:(?:25[0-5]|2[0-4]\d|[01]\d\d|\d?\d)(?(\.?\d)\.)){4})
											)
											(?<port>:\d+)?
											(?<relativePath>
											 [/?]
											 (
											  [-a-z0-9._?,;'/\+&%$#=~!@]*
											  (?<pathInPairedBrackets>
											   \(
												[-a-z0-9._?,;'/\+&%$#=~!@]*
											   \)
											  )?
											  [-a-z0-9._?,;'/\+&%$#=~!@]*
											  # not all chars can be last in url
											  (?<=[-a-z0-9_'/\+&%$#=~@)]|&[a-z]+;)
											 )?
											)?
										   )
										   ";
		private static readonly Regex urlRegex = new Regex(urlRegexS, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		public static string EncodeMultiLineText(this HtmlHelper helper, string text, bool keepFirstSpaces=false)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			text = helper.Encode(text);

			if (keepFirstSpaces)
				text = Regex.Replace(text, @"^\s+", m => string.Concat(Enumerable.Repeat("&nbsp;", m.Value.Length)), RegexOptions.Multiline);

			return text.Replace("\n", "<br/>").Replace("\r", "");
		}

		private static bool IsEmailUrl(string url)
		{
			return url.Contains("@") && !url.Contains("/");
		}

		public static string HighlightLink(this HtmlHelper helper, string url)
		{
			var fullUrl = url;
			if (!url.Contains("://") && !url.StartsWith("mailto:"))
			{
				if (IsEmailUrl(url))
					fullUrl = "mailto:" + url;
				else
					fullUrl = "http://" + url;
			}
			return $"<a href=\"{helper.AttributeEncode(HttpUtility.HtmlDecode(fullUrl))}\">{url}</a>";
		}

		/// <summary>
		/// Find urls in html-encoded text and replace it with links
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="htmlEncodedText">HTML-encoded text</param>
		/// <returns>HTML string with links</returns>
		public static string HighlightLinks(this HtmlHelper helper, string htmlEncodedText)
		{
			return urlRegex.Replace(htmlEncodedText, match => helper.HighlightLink(match.Groups[0].Value));
		}

		/// <summary>
		/// Creates `DropDownList` with item's data-attributes. Options are the same as for `SelectExtensions.DropdownList()`,
		/// but `selectList` is a list of `SelectListItemWithAttributes`.
		/// </summary>
		public static MvcHtmlString DropDownListWithItemAttributes(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItemWithAttributes> selectList, IDictionary<string, object> htmlAttributes)
		{
			selectList = selectList.ToList();
			var selectItems = selectList.ToDictionary(item => item.Value, item => item);
			var selectDoc = XDocument.Parse(htmlHelper.DropDownList(name, selectList, htmlAttributes).ToString());

			var selectElement = selectDoc.Element("select");
			if (selectElement != null)
			{
				var options = selectElement.XPathSelectElements("option").ToList();

				foreach (var option in options)
				{
					var optionValue = option.Attribute("value");
					if (optionValue != null && selectItems.ContainsKey(optionValue.Value))
					{
						var listItem = selectItems[optionValue.Value];
						foreach (var attribute in HtmlHelper.AnonymousObjectToHtmlAttributes(listItem.HtmlAttributes))
							option.SetAttributeValue(attribute.Key, attribute.Value);
					}
				}
			}

			return MvcHtmlString.Create(selectDoc.ToString());
		}

		public static MvcHtmlString DropDownListWithItemAttributes(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItemWithAttributes> selectList, object htmlAttributes)
		{
			return DropDownListWithItemAttributes(htmlHelper, name, selectList, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
		}
	}

	public class SelectListItemWithAttributes : SelectListItem
	{
		public object HtmlAttributes = new object();
	}

}