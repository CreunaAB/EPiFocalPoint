﻿using EPiServer.Core;
using EPiServer.Data.Entity;
using ImageResizer.Plugins.EPiFocalPoint.SpecializedProperties;

namespace ImageResizer.Plugins.EPiFocalPoint {
	public interface IFocalPointData : IContentImage, IReadOnly {
		FocalPoint FocalPoint { get; set; }
		int? OriginalWidth { get; set; }
		int? OriginalHeight { get; set; }
	}
}