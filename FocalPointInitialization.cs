﻿using System;
using System.Drawing;
using System.Linq;

using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Framework.Localization;
using EPiServer.Framework.Localization.XmlResources;
using EPiServer.Logging;

namespace ImageResizer.Plugins.EPiFocalPoint {
	[InitializableModule, ModuleDependency(typeof(FrameworkInitialization))]
	public class FocalPointInitialization : IInitializableModule {
		private const string LocalizationProviderName = "FocalPointLocalizations";
		private bool eventsAttached;
		private static readonly ILogger Logger = LogManager.GetLogger();
		public void Initialize(InitializationEngine context) {
			InitializeLocalizations(context);
			InitializeEventHooks(context);
		}
		private static void InitializeLocalizations(InitializationEngine context) {
			var localizationService = context.Locate.Advanced.GetInstance<LocalizationService>() as ProviderBasedLocalizationService;
			if(localizationService != null) {
				var localizationProviderInitializer = new EmbeddedXmlLocalizationProviderInitializer();
				var localizationProvider = localizationProviderInitializer.GetInitializedProvider(LocalizationProviderName, typeof(FocalPointInitialization).Assembly);
				localizationService.Providers.Insert(0, localizationProvider);
			}
		}
		private void InitializeEventHooks(InitializationEngine context) {
			if(!eventsAttached) {
				var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
				contentEvents.CreatingContent += SavingImage;
				contentEvents.SavingContent += SavingImage;
				eventsAttached = true;
			}
		}
		private static void SavingImage(object sender, ContentEventArgs e) {
			var focalPointData = e.Content as IFocalPointData;
			if(focalPointData != null) {
				SetDimensions(focalPointData);
			}
		}
		private static void SetDimensions(IFocalPointData focalPointData) {
			if(!focalPointData.IsReadOnly && focalPointData.BinaryData != null) {
				using(var stream = focalPointData.BinaryData.OpenRead()) {
					using(var bitmap = Image.FromStream(stream, false)) {
						if(focalPointData.OriginalHeight != bitmap.Height) {
							Logger.Information($"Setting height for {focalPointData.Name} to {bitmap.Height}.");
							focalPointData.OriginalHeight = bitmap.Height;
						}
						if(focalPointData.OriginalWidth != bitmap.Width) {
							Logger.Information($"Setting width for {focalPointData.Name} to {bitmap.Width}.");
							focalPointData.OriginalWidth = bitmap.Width;
						}
					}
				}
			}
		}
		public void Uninitialize(InitializationEngine context) {
			UninitializeLocalizations(context);
			UninitializeEventHooks(context);
		}
		private static void UninitializeLocalizations(InitializationEngine context) {
			var localizationService = context.Locate.Advanced.GetInstance<LocalizationService>() as ProviderBasedLocalizationService;
			var localizationProvider = localizationService?.Providers.FirstOrDefault(p => p.Name.Equals(LocalizationProviderName, StringComparison.Ordinal));
			if(localizationProvider != null) {
				localizationService.Providers.Remove(localizationProvider);
			}
		}
		private void UninitializeEventHooks(InitializationEngine context) {
			var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
			contentEvents.CreatingContent -= SavingImage;
			contentEvents.SavingContent -= SavingImage;
			eventsAttached = false;
		}
	}
}