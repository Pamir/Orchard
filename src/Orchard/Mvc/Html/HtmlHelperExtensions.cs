﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using Orchard.Collections;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.Utility;
using Orchard.Utility.Extensions;
using System.Web;

namespace Orchard.Mvc.Html {
    public static class HtmlHelperExtensions {

        public static string NameOf<T>(this HtmlHelper<T> html, Expression<Action<T>> expression) {
            return Reflect.NameOf(html.ViewData.Model, expression);
        }

        public static string NameOf<T, TResult>(this HtmlHelper<T> html, Expression<Func<T, TResult>> expression) {
            return Reflect.NameOf(html.ViewData.Model, expression);
        }

        public static string FieldNameFor<T, TResult>(this HtmlHelper<T> html, Expression<Func<T, TResult>> expression) {
            return html.ViewData.TemplateInfo.GetFullHtmlFieldName(ExpressionHelper.GetExpressionText(expression));
        }

        public static string FieldIdFor<T, TResult>(this HtmlHelper<T> html, Expression<Func<T, TResult>> expression) {
            var id = html.ViewData.TemplateInfo.GetFullHtmlFieldId(ExpressionHelper.GetExpressionText(expression));
            // because "[" and "]" aren't replaced with "_" in GetFullHtmlFieldId
            return id.Replace('[', '_').Replace(']', '_');
        }

        public static IHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, LocalizedString labelText) {
            return LabelFor(html, expression, labelText.ToString());
        }

        public static IHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText) {
            if (String.IsNullOrEmpty(labelText)) {
                return MvcHtmlString.Empty;
            }
            var htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            var tag = new TagBuilder("label");
            tag.Attributes.Add("for", html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(htmlFieldName));
            tag.SetInnerText(labelText);
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString SelectOption<T>(this HtmlHelper html, T currentValue, T optionValue, string text) {
            return SelectOption(html, optionValue, object.Equals(optionValue, currentValue), text);
        }

        public static MvcHtmlString SelectOption(this HtmlHelper html, object optionValue, bool selected, string text) {
            var builder = new TagBuilder("option");

            if (optionValue != null)
                builder.MergeAttribute("value", optionValue.ToString());

            if (selected)
                builder.MergeAttribute("selected", "selected");

            builder.SetInnerText(text);

            return MvcHtmlString.Create(builder.ToString(TagRenderMode.Normal));
        }

        #region Pager

        public static IHtmlString Pager<T>(this HtmlHelper html, IPageOfItems<T> pageOfItems, int currentPage, int defaultPageSize, object values = null, string previousText = "<", string nextText = ">", bool alwaysShowPreviousAndNext = false) {
            if (pageOfItems.TotalPageCount < 2)
                return new HtmlString(string.Empty);

            var sb = new StringBuilder(75);
            var rvd = new RouteValueDictionary {{"q", ""},{"page", 0}};
            var viewContext = html.ViewContext;
            var urlHelper = new UrlHelper(viewContext.RequestContext);

            if (pageOfItems.PageSize != defaultPageSize)
                rvd.Add("pagesize", pageOfItems.PageSize);

            foreach (var item in viewContext.RouteData.Values) {
                rvd.Add(item.Key, item.Value);
            }


            if (values != null) {
                var rvd2 = new RouteValueDictionary(values);

                foreach (var item in rvd2) {
                    rvd[item.Key] = item.Value;
                }
            }

            sb.Append("<p class=\"pager\">");

            if (currentPage > 1 || alwaysShowPreviousAndNext) {
                if (currentPage == 2)
                    rvd.Remove("page");
                else
                    rvd["page"] = currentPage - 1;

                sb.AppendFormat(" <a href=\"{1}\" class=\"previous\">{0}</a>", previousText,
                                urlHelper.RouteUrl(rvd));
            }

            //todo: when there are many pages (> 15?) maybe do something like 1 2 3...6 7 8...13 14 15
            for (var p = 1; p <= pageOfItems.TotalPageCount; p++) {
                if (p == currentPage) {
                    sb.AppendFormat(" <span>{0}</span>", p);
                }
                else {
                    if (p == 1)
                        rvd.Remove("page");
                    else
                        rvd["page"] = p;

                    sb.AppendFormat(" <a href=\"{1}\">{0}</a>", p,
                                    urlHelper.RouteUrl(rvd));
                }
            }

            if (currentPage < pageOfItems.TotalPageCount || alwaysShowPreviousAndNext) {
                rvd["page"] = currentPage + 1;
                sb.AppendFormat("<a href=\"{1}\" class=\"next\">{0}</a>", nextText,
                                urlHelper.RouteUrl(rvd));
            }

            sb.Append("</p>");

            return new HtmlString(sb.ToString());
        }

        #endregion

        #region UnorderedList

        public static IHtmlString UnorderedList<T>(this HtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, MvcHtmlString> generateContent, string cssClass) {
            return htmlHelper.UnorderedList(items, generateContent, cssClass, null, null);
        }

        public static IHtmlString UnorderedList<T>(this HtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, MvcHtmlString> generateContent, string cssClass, string itemCssClass, string alternatingItemCssClass) {
            return UnorderedList(items, (t, i) => generateContent(t, i) as IHtmlString, cssClass, t => itemCssClass, t => alternatingItemCssClass);
        }

        public static IHtmlString UnorderedList<T>(this HtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, IHtmlString> generateContent, string cssClass) {
            return htmlHelper.UnorderedList(items, generateContent, cssClass, null, null);
        }

        public static IHtmlString UnorderedList<T>(this HtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, IHtmlString> generateContent, string cssClass, string itemCssClass, string alternatingItemCssClass) {
            return UnorderedList(items, generateContent, cssClass, t => itemCssClass, t => alternatingItemCssClass);
        }

        private static IHtmlString UnorderedList<T>(IEnumerable<T> items, Func<T, int, IHtmlString> generateContent, string cssClass, Func<T, string> generateItemCssClass, Func<T, string> generateAlternatingItemCssClass) {
            if (items == null || !items.Any()) return new HtmlString(string.Empty);

            var sb = new StringBuilder(250);
            int counter = 0, count = items.Count() - 1;

            sb.AppendFormat(
                !string.IsNullOrEmpty(cssClass) ? "<ul class=\"{0}\">" : "<ul>",
                cssClass
                );

            foreach (var item in items) {
                var sbClass = new StringBuilder(50);

                if (counter == 0)
                    sbClass.Append("first ");
                if (counter == count)
                    sbClass.Append("last ");
                if (generateItemCssClass != null)
                    sbClass.AppendFormat("{0} ", generateItemCssClass(item));
                if (counter % 2 != 0 && generateAlternatingItemCssClass != null)
                    sbClass.AppendFormat("{0} ", generateAlternatingItemCssClass(item));

                sb.AppendFormat(
                    sbClass.Length > 0
                        ? string.Format("<li class=\"{0}\">{{0}}</li>", sbClass.ToString().TrimEnd())
                        : "<li>{0}</li>",
                    generateContent(item, counter)
                    );

                counter++;
            }

            sb.Append("</ul>");

            return new HtmlString(sb.ToString());
        }

        #endregion

        #region Ellipsize

        public static IHtmlString Ellipsize(this HtmlHelper htmlHelper, string text, int characterCount) {
            return new HtmlString(htmlHelper.Encode(text).Ellipsize(characterCount));
        }

        public static IHtmlString Ellipsize(this HtmlHelper htmlHelper, string text, int characterCount, string ellipsis) {
            return new HtmlString(htmlHelper.Encode(text).Ellipsize(characterCount, ellipsis));
        }

        #endregion

        #region Excerpt

        public static MvcHtmlString Excerpt(this HtmlHelper html, string markup, int length) {

            return MvcHtmlString.Create(markup.RemoveTags().Ellipsize(length));
        }

        #endregion

        #region Format Date/Time

        public static LocalizedString DateTime(this HtmlHelper htmlHelper, DateTime? value, LocalizedString defaultIfNull) {
            return value.HasValue ? htmlHelper.DateTime(value.Value) : defaultIfNull;
        }

        public static LocalizedString DateTime(this HtmlHelper htmlHelper, DateTime? value, LocalizedString defaultIfNull, LocalizedString customFormat) {
            return value.HasValue ? htmlHelper.DateTime(value.Value, customFormat) : defaultIfNull;
        }

        public static LocalizedString DateTime(this HtmlHelper htmlHelper, DateTime value) {
            //TODO: (erikpo) This default format should come from a site setting
            return htmlHelper.DateTime(value.ToLocalTime(), new LocalizedString("MMM d yyyy h:mm tt")); //todo: above comment and get rid of just wrapping this as a localized string
        }

        public static LocalizedString DateTime(this HtmlHelper htmlHelper, DateTime value, LocalizedString customFormat) {
            //TODO: (erikpo) In the future, convert this to "local" time before calling ToString
            return new LocalizedString(value.ToString(customFormat.Text));
        }

        #endregion

        #region Image

        public static MvcHtmlString Image(this HtmlHelper htmlHelper, string src, string alt, object htmlAttributes) {
            return htmlHelper.Image(src, alt, new RouteValueDictionary(htmlAttributes));
        }

        public static MvcHtmlString Image(this HtmlHelper htmlHelper, string src, string alt, IDictionary<string, object> htmlAttributes) {
            UrlHelper url = new UrlHelper(htmlHelper.ViewContext.RequestContext);
            string imageUrl = url.Content(src);
            TagBuilder imageTag = new TagBuilder("img");

            if (!string.IsNullOrEmpty(imageUrl))
                imageTag.MergeAttribute("src", imageUrl);

            if (!string.IsNullOrEmpty(alt))
                imageTag.MergeAttribute("alt", alt);

            imageTag.MergeAttributes(htmlAttributes, true);

            if (imageTag.Attributes.ContainsKey("alt") && !imageTag.Attributes.ContainsKey("title"))
                imageTag.MergeAttribute("title", imageTag.Attributes["alt"] ?? "");

            return MvcHtmlString.Create(imageTag.ToString(TagRenderMode.SelfClosing));
        }

        #endregion

        #region Link

        public static IHtmlString Link(this HtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.Link(linkContents, href, null);
        }

        public static IHtmlString Link(this HtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.Link(linkContents, href, new RouteValueDictionary(htmlAttributes));
        }

        public static IHtmlString Link(this HtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            TagBuilder tagBuilder = new TagBuilder("a") 
                { InnerHtml = htmlHelper.Encode(linkContents) };
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", href);
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }

        #endregion

        #region LinkOrDefault

        public static IHtmlString LinkOrDefault(this HtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.LinkOrDefault(linkContents, href, null);
        }

        public static IHtmlString LinkOrDefault(this HtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.LinkOrDefault(linkContents, href, new RouteValueDictionary(htmlAttributes));
        }

        public static IHtmlString LinkOrDefault(this HtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            string linkText = htmlHelper.Encode(linkContents);
            
            if (string.IsNullOrEmpty(href)) {
                return new HtmlString(linkText);
            }

            TagBuilder tagBuilder = new TagBuilder("a")
            {
                InnerHtml = linkText
            };
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", href);
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }

        #endregion

        #region BeginFormAntiForgeryPost

        public static MvcForm BeginFormAntiForgeryPost(this HtmlHelper htmlHelper) {
            return htmlHelper.BeginFormAntiForgeryPost(htmlHelper.ViewContext.HttpContext.Request.Url.PathAndQuery, FormMethod.Post, new RouteValueDictionary());
        }

        public static MvcForm BeginFormAntiForgeryPost(this HtmlHelper htmlHelper, string formAction) {
            return htmlHelper.BeginFormAntiForgeryPost(formAction, FormMethod.Post, new RouteValueDictionary());
        }
        
        public static MvcForm BeginFormAntiForgeryPost(this HtmlHelper htmlHelper, string formAction, FormMethod formMethod) {
            return htmlHelper.BeginFormAntiForgeryPost(formAction, formMethod, new RouteValueDictionary());
        }

        public static MvcForm BeginFormAntiForgeryPost(this HtmlHelper htmlHelper, string formAction, FormMethod formMethod, object htmlAttributes) {
            return htmlHelper.BeginFormAntiForgeryPost(formAction, formMethod, new RouteValueDictionary(htmlAttributes));
        }

        public static MvcForm BeginFormAntiForgeryPost(this HtmlHelper htmlHelper, string formAction, FormMethod formMethod, IDictionary<string, object> htmlAttributes) {
            var tagBuilder = new TagBuilder("form");

            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("action", formAction);
            tagBuilder.MergeAttribute("method", HtmlHelper.GetFormMethodString(formMethod), true);

            htmlHelper.ViewContext.Writer.Write(tagBuilder.ToString(TagRenderMode.StartTag));

            return new MvcFormAntiForgeryPost(htmlHelper);
        }

        #endregion

        #region AntiForgeryTokenOrchard

        public static MvcHtmlString AntiForgeryTokenOrchard(this HtmlHelper htmlHelper) {
            var siteSalt = htmlHelper.Resolve<ISiteService>().GetSiteSettings().SiteSalt;

            try {
                return htmlHelper.AntiForgeryToken(siteSalt);
            }
            catch(System.Web.Mvc.HttpAntiForgeryException) {
                // Work-around an issue in MVC 2:  If the browser sends a cookie that is not
                // coming from this server (this can happen if the user didn't close their browser
                // while the application server configuration changed), clear it up
                // so that a new one is generated and sent to the browser. This is harmless
                // from a security point of view, since we are _issuing_ an anti-forgery token,
                // not validating input.

                // Remove the token so that MVC will create a new one.
                var antiForgeryTokenName = htmlHelper.GetAntiForgeryTokenName();
                htmlHelper.ViewContext.HttpContext.Request.Cookies.Remove(antiForgeryTokenName);

                // Try again
                return htmlHelper.AntiForgeryToken(siteSalt);
            }
        }

        private static string GetAntiForgeryTokenName(this HtmlHelper htmlHelper) {
            // Generate the same cookie name as MVC
            var appPath = htmlHelper.ViewContext.HttpContext.Request.ApplicationPath;
            const string antiForgeryTokenName = "__RequestVerificationToken";
            if (string.IsNullOrEmpty(appPath)) {
                return antiForgeryTokenName;
            }
            return antiForgeryTokenName + '_' + Base64EncodeForCookieName(appPath);
        }

        private static string Base64EncodeForCookieName(string s) {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).Replace('+', '.').Replace('/', '-').Replace('=', '_');
        }

        #endregion

        #region AntiForgeryTokenValueOrchardLink

        public static IHtmlString AntiForgeryTokenValueOrchardLink(this HtmlHelper htmlHelper, string linkContents, string href)  {
            return htmlHelper.AntiForgeryTokenValueOrchardLink(linkContents, href, (object)null);
        }

        public static IHtmlString AntiForgeryTokenValueOrchardLink(this HtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)  {
            return htmlHelper.AntiForgeryTokenValueOrchardLink(linkContents, href, new RouteValueDictionary(htmlAttributes));
        }

        public static IHtmlString AntiForgeryTokenValueOrchardLink(this HtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)  {
            return htmlHelper.Link(linkContents, htmlHelper.AntiForgeryTokenGetUrl(href).ToString(), htmlAttributes);
        }

        #endregion

        #region AntiForgeryTokenGetUrl

        public static IHtmlString AntiForgeryTokenGetUrl(this HtmlHelper htmlHelper, string baseUrl)  {
            return new HtmlString(string.Format("{0}{1}__RequestVerificationToken={2}", baseUrl, baseUrl.IndexOf('?') > -1 ? "&" : "?", htmlHelper.ViewContext.HttpContext.Server.UrlEncode(htmlHelper.AntiForgeryTokenValueOrchard().ToString())));
        }

        #endregion

        #region AntiForgeryTokenValueOrchard

        public static IHtmlString AntiForgeryTokenValueOrchard(this HtmlHelper htmlHelper) {
            //HAACK: (erikpo) Since MVC doesn't expose any of its methods for generating the antiforgery token and setting the cookie, we'll just let it do its thing and parse out what we need
            var field = htmlHelper.AntiForgeryTokenOrchard().ToHtmlString();
            var beginIndex = field.IndexOf("value=\"") + 7;
            var endIndex = field.IndexOf("\"", beginIndex);

            return new HtmlString(field.Substring(beginIndex, endIndex - beginIndex));
        }

        #endregion
    }
}
