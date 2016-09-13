#Focal point based cropping for EPiServer using ImageResizing.NET

##Prerequisites
Make sure your Image Media ContentTypes inherit from ```ImageResizer.Plugins.EPiFocalPoint.FocalPointImageData```.

##Usage
Edit the image in AllPropertiesView, and place the red dot where you want it in the image.

##What is installed?
1. An ImageResizing.NET plugin is installed in web.config

		<resizer>
			<plugins>
				<add name="EPiFocalPointPlugin" />
			</plugins>
		</resizer>

1. ClientResources are installed (the editor needed for editors to place the focal point), along with some dojo stuff in module.config

		<module>
			<dojo>
				<paths>
					<add name="focal-point" path="" />
				</paths>
			</dojo>
		</module>


##How it works
The coordinates of the focal point are stored as a ```ImageResizer.Plugins.EPiFocalPoint.SpecializedProperties.FocalPoint``` property on the image. 
The image dimensions are also stored in the properties ```OriginalWidth``` and ```OriginalHeight``` whenever the image is saved.

When the image is requested, the ```crop``` parameter is added "under the hood", and then ImageResizing does its thing.

##Additional localizations
Embedded localizations are provided for Swedish and English. Should you need to localize in other languages, you can do so by adding XML translations thusly:

		<contenttypes>
			<imagedata>
				<properties>
					<focalpoint>
						<caption>Focal point</caption>
						<help>The point in the image, where the focus should be, automatically cropped images will be calculated based on this point.</help>
					</focalpoint>
					<originalheight>
						<caption>Height</caption>
						<help>The image height in pixels.</help>
					</originalheight>
					<originalwidth>
						<caption>Width</caption>
						<help>The image width in pixels.</help>
					</originalwidth>
				</properties>
			</imagedata>
		</contenttypes>
