﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Web;

using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

using ImageResizer.Configuration;
using ImageResizer.Configuration.Xml;

namespace ImageResizer.Plugins.EPiFocalPoint {
	public class EPiFocalPointPlugin : IPlugin {
		private readonly IContentCacheKeyCreator contentCacheKeyCreator;
		private readonly ISynchronizedObjectInstanceCache cache;
		private static readonly ILogger Logger = LogManager.GetLogger();
		private readonly Dictionary<string, ResizeSettings> defaults = new Dictionary<string, ResizeSettings>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, ResizeSettings> settings = new Dictionary<string, ResizeSettings>(StringComparer.OrdinalIgnoreCase);
		private bool onlyAllowPresets;
		public EPiFocalPointPlugin() : this(ServiceLocator.Current.GetInstance<IContentCacheKeyCreator>(), ServiceLocator.Current.GetInstance<ISynchronizedObjectInstanceCache>()) { }

		public EPiFocalPointPlugin(IContentCacheKeyCreator contentCacheKeyCreator, ISynchronizedObjectInstanceCache cache) {
			this.contentCacheKeyCreator = contentCacheKeyCreator;
			this.cache = cache;
		}
		public IPlugin Install(Config c) {
			c.Plugins.add_plugin(this);
			ParseXml(c.getConfigXml().queryFirst("presets"));
			c.Pipeline.RewriteDefaults += PipelineRewriteDefaults;
			return this;
		}
		protected void ParseXml(Node presetConfigNode) {
			if(presetConfigNode?.Children == null) {
				return;
			}
			onlyAllowPresets = GetBoolFromString(presetConfigNode.Attrs["onlyAllowPresets"]);
			foreach(var presetNode in presetConfigNode.Children) {
				var name = presetNode.Attrs["name"];
				if(presetNode.Name.Equals("preset", StringComparison.OrdinalIgnoreCase)) {
					var presetDefaults = presetNode.Attrs["defaults"];
					if(!string.IsNullOrEmpty(presetDefaults)) {
						defaults[name] = new ResizeSettings(presetDefaults);
					}
					var presetSettings = presetNode.Attrs["settings"];
					if(!string.IsNullOrEmpty(presetSettings)) {
						settings[name] = new ResizeSettings(presetSettings);
					}
				}
			}
		}
		private static bool GetBoolFromString(string attributeValue) {
			return !string.IsNullOrWhiteSpace(attributeValue) && bool.Parse(attributeValue);
		}
		private void PipelineRewriteDefaults(IHttpModule sender, HttpContext context, IUrlEventArgs e) {
			ApplyFocalPointCropping(e);
		}
		private void ApplyFocalPointCropping(IUrlEventArgs urlEventArgs) {
#if DEBUG
			var stopWatch = new Stopwatch();
			stopWatch.Start();
#endif
			try {
				var resizeSettings = GetResizeSettingsFromQueryString(urlEventArgs.QueryString);
				if(resizeSettings == null) {
					return;
				}
				var cacheKey = GetCacheKeyForUrl(urlEventArgs, resizeSettings);
				var cropParameters = this.cache.Get(cacheKey) as string;
				if(cropParameters == null) {
					Logger.Debug($"Crop parameters not found in cache for '{urlEventArgs.VirtualPath}'.");
					var currentContent = ServiceLocator.Current.GetInstance<IContentRouteHelper>().Content;
					if(currentContent != null) {
						var evictionPolicy = GetEvictionPolicy(currentContent.ContentLink);
						var focalPointData = currentContent as IFocalPointData;
						if(focalPointData?.FocalPoint != null && focalPointData.ShouldApplyFocalPoint(resizeSettings)) {
							Logger.Debug($"Altering resize parameters for {focalPointData.Name} based on focal point.");
							cropParameters = CropDimensions.Parse(focalPointData, resizeSettings).ToString();
							this.cache.Insert(cacheKey, cropParameters, evictionPolicy);
						} else {
							Logger.Debug($"No focal point set for '{currentContent.Name}'.");
							this.cache.Insert(cacheKey, string.Empty, evictionPolicy);
						}
					}
				}
				if(!string.IsNullOrWhiteSpace(cropParameters)) {
					urlEventArgs.QueryString.Add("crop", cropParameters);
				}
			} catch(Exception ex) {
				Logger.Critical("A critical error occured when trying to get focal point data.", ex);
			}
#if DEBUG
			stopWatch.Stop();
			Logger.Debug($"{nameof(ApplyFocalPointCropping)} for {urlEventArgs.VirtualPath} took {stopWatch.ElapsedMilliseconds}ms.");
#endif
		}
		private ResizeSettings GetResizeSettingsFromQueryString(NameValueCollection queryString) {
			var preset = queryString["preset"];
			if(HasPreset(preset)) {
				return this.settings.ContainsKey(preset) && this.settings[preset] != null ? this.settings[preset] : this.defaults[preset];
			}
			return this.onlyAllowPresets ? null : new ResizeSettings(queryString);
		}
		private bool HasPreset(string preset) {
			return !string.IsNullOrWhiteSpace(preset) && (defaults.ContainsKey(preset) || settings.ContainsKey(preset));
		}
		private static string GetCacheKeyForUrl(IUrlEventArgs urlEventArgs, ResizeSettings resizeSettings) {
			var keyBuilder = new StringBuilder();
			keyBuilder.Append("focalpoint:");
			keyBuilder.Append(urlEventArgs.VirtualPath);
			keyBuilder.Append(":");
			foreach(var key in resizeSettings.AllKeys) {
				keyBuilder.AppendFormat("{0}:{1}", key, resizeSettings[key]);
			}
			return keyBuilder.ToString();
		}
		private CacheEvictionPolicy GetEvictionPolicy(ContentReference contentLink) {
			return new CacheEvictionPolicy(new[] { contentCacheKeyCreator.CreateCommonCacheKey(contentLink) });
		}
		public bool Uninstall(Config c) {
			c.Plugins.remove_plugin(this);
			c.Pipeline.RewriteDefaults -= PipelineRewriteDefaults;
			return true;
		}
	}
}