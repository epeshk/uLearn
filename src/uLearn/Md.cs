﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace uLearn
{
	public static class Md
	{
		public static string RenderMd(this string md, FileInfo sourceFile)
		{
			var baseUrl = CourseUnitUtils.GetDirectoryRelativeWebPath(sourceFile);
			return md.RenderMd(baseUrl);
		}

		public static HtmlString RenderTex(this string textWithTex)
		{
			var texReplacer = new TexReplacer(textWithTex);
			string html = HttpUtility.HtmlEncode(texReplacer.ReplacedText);
			return new HtmlString(texReplacer.PlaceTexInsertsBack(html));
		}

		public static string RenderMd(this string md, string baseUrl = null)
		{
			var texReplacer = new TexReplacer(md);

			var markdown = new Markdown2(baseUrl)
			{
				NewWindowForExternalLinks = true,
				ExtraMode = true,
				SafeMode = false,
				MarkdownInHtml = false
			};

			markdown.FormatCodeBlock += FormatCodePrettyPrint;

			var html = markdown.Transform(texReplacer.ReplacedText);
			return texReplacer.PlaceTexInsertsBack(html);
		}


		public static Regex rxExtractLanguage = new Regex("^({{(.+)}}[\r\n])", RegexOptions.Compiled);
		public static string FormatCodePrettyPrint(MarkdownDeep.Markdown m, string code)
		{
			// Try to extract the language from the first line
			var match = rxExtractLanguage.Match(code);
			string language = null;

			if (match.Success)
			{
				// Save the language
				var g = (Group)match.Groups[2];
				language = g.ToString();

				// Remove the first line
				code = code.Substring(match.Groups[1].Length);
			}

			// If not specified, look for a link definition called "default_syntax" and
			// grab the language from its title
			if (language == null)
			{
				var d = m.GetLinkDefinition("default_syntax");
				if (d != null)
					language = d.title;
			}

			// Common replacements
			if (language == "C#")
				language = "csharp";
			if (language == "C++")
				language = "cpp";

			// Wrap code in pre/code tags and add PrettyPrint attributes if necessary
			if (string.IsNullOrEmpty(language))
				return string.Format("<pre><code>{0}</code></pre>\n", code);
			else
				return string.Format("<textarea class=\"code code-sample data-lang='{0}' \">{1}</textarea>\n",
									language.ToLowerInvariant(), code);
		}
		public static Tuple<string, List<string>> GetHtmlWithUrls(this string md, string baseUrl = null)
		{
			
			var texReplacer = new EdxTexReplacer(md);

			var markdown = new Markdown2(baseUrl, false)
			{
				NewWindowForExternalLinks = true,
				ExtraMode = true,
				SafeMode = false,
				MarkdownInHtml = false,
			};
			
			var relativeUrls = new List<string>();
			markdown.RelativeUrl += relativeUrls.Add;

			var html = markdown.Transform(texReplacer.ReplacedText);
			
			return Tuple.Create(texReplacer.PlaceTexInsertsBack(html), relativeUrls);
		}
	}
}