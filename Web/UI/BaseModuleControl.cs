using System;
using System.Web;
using System.IO;

using Cuyahoga.Core;
using Cuyahoga.Core.Domain;
using Cuyahoga.Web.Util;

namespace Cuyahoga.Web.UI
{
	/// <summary>
	/// Base class for all module user controls.
	/// Credits to the DotNetNuke team (http://www.dotnetnuke.com) for the output caching idea!
	/// </summary>
	public class BaseModuleControl : LocalizedUserControl
	{
		private ModuleBase _module;
		private string _cachedOutput;
		private bool _displaySyndicationIcon;

		/// <summary>
		/// Indicator if there is cached content. The derived ModuleControls should determine whether to
		/// load content or not.
		/// </summary>
		protected bool HasCachedOutput
		{
			get { return this._cachedOutput != null; }
		}

		/// <summary>
		/// Indicate if the module should display the syndication icon at its current state.
		/// </summary>
		protected bool DisplaySyndicationIcon
		{
			get { return this._displaySyndicationIcon; }
			set { this._displaySyndicationIcon = value; }
		}

		/// <summary>
		/// The accompanying ModuleBase business object. Use this property  to access
		/// module properties, sections and nodes from the code-behind of the module user controls.
		/// </summary>
		public ModuleBase Module
		{
			get { return this._module; }
			set { this._module = value; }
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public BaseModuleControl()
		{
			// Show the syndication icon by default. It can be set by subclasses.
			this._displaySyndicationIcon = (this is ISyndicatable);
		}

		protected override void OnInit(EventArgs e)
		{
			if (this.Module.Section.CacheDuration > 0 
				&& this.Module.CacheKey != null
				&& ! this.Page.User.Identity.IsAuthenticated
				&& ! this.Page.IsPostBack)
			{
				// Get the cached content. Don't use cached output after a postback.
				if (HttpContext.Current.Cache[this.Module.CacheKey] != null && ! this.IsPostBack)
				{
					// Found cached content.
					this._cachedOutput = HttpContext.Current.Cache[this.Module.CacheKey].ToString();
				}
			}
			base.OnInit(e);
		}


		/// <summary>
		/// Wrap the section content in a visual block.
		/// </summary>
		/// <param name="writer"></param>
		protected override void Render(System.Web.UI.HtmlTextWriter writer)
		{
			// TODO: get rid of this html hacking. Need a more declarative approach.
			writer.Write("<div class=\"section\">");
			// Section title
			if (this._module.Section != null && this._module.Section.ShowTitle)
			{
				writer.Write("<h3>" + this._module.DisplayTitle + "</h3>");
			}
			// Rss feed
			writer.Write("<div id=\"moduletools\" align=\"right\">");
			if (this._displaySyndicationIcon)
			{
				writer.Write(String.Format("<a href=\"{0}\"><img src=\"{1}\" border=\"0\"></a>", 
					UrlHelper.GetRssUrlFromSection(this._module.Section) + this._module.ModulePathInfo, UrlHelper.GetApplicationPath() + "Images/feed-rss.gif"));
			}
			// Edit button
			User cuyahogaUser = this.Page.User.Identity as User;
			if (this._module.Section.ModuleType.EditPath != null
				&& cuyahogaUser != null
				&& cuyahogaUser.CanEdit(this._module.Section))
			{
				writer.Write(String.Format("<a href=\"{0}?NodeId={1}&SectionId={2}\">Edit</a>", UrlHelper.GetApplicationPath() + this._module.Section.ModuleType.EditPath, this._module.Section.Node.Id, this._module.Section.Id));
			}
			writer.Write("</div>");
			// Write module content and handle caching when neccesary.
			// Don't cache when the user is logged in or after a postback.
			if (this._module.Section.CacheDuration > 0 
				&& this.Module.CacheKey != null
				&& ! this.Page.User.Identity.IsAuthenticated
				&& ! this.Page.IsPostBack)
			{
				if (this._cachedOutput == null)
				{
					StringWriter tempWriter = new StringWriter();
					base.Render(new System.Web.UI.HtmlTextWriter(tempWriter));
					this._cachedOutput = tempWriter.ToString();
					HttpContext.Current.Cache.Insert(this.Module.CacheKey, this._cachedOutput, null
						, DateTime.Now.AddSeconds(this._module.Section.CacheDuration), TimeSpan.Zero);
				}
				// Output the user control's content.
                writer.Write(_cachedOutput);
			}
			else
			{
				base.Render (writer);
			}
			writer.Write("</div>");
		}

		/// <summary>
		/// Empty the output cache for the current module state.
		/// </summary>
		protected void InvalidateCache()
		{
			if (this.Module.CacheKey != null)
			{
				HttpContext.Current.Cache.Remove(this.Module.CacheKey);
			}
		}
	}
}
