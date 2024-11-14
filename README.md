# VSWaterMark

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![Works with Visual Studio 2019](https://img.shields.io/static/v1.svg?label=VS&message=2019&color=5F2E96)
![Works with Visual Studio 2022](https://img.shields.io/static/v1.svg?label=VS&message=2022&color=A853C7)
![Visual Studio Marketplace 5 Stars](https://img.shields.io/badge/VS%20Marketplace-★★★★★-green)

[![Build](https://github.com/mrlacey/VSWaterMark/actions/workflows/build.yaml/badge.svg)](https://github.com/mrlacey/VSWaterMark/actions/workflows/build.yaml)
![Tests](https://gist.githubusercontent.com/mrlacey/c586ff0f495b4a8dd76ab0dbdf9c89e0/raw/VSWaterMark.badge.svg)

Display a text watermark over the Visual Studio editor.

Ensure your name, handle, URI, or other important text is always visible on the screen.
This can be useful for presentations, demos, screencasts, or streaming as it ensures that this text is always visible, even on photos or screen captures.

![VS screenshot showing highlighted watermark](./Assets/vswatermark-example.png)

The highlighted text may appear as part of the text in the editor but is floating above it. It stays put when you scroll.

## Fully Configurable

Size, position, color, and text are fully configurable.

Go to **Tools** &gt; **Options** and select **Water Mark**

![Screenshot of Water Mark settings in the Options dialog](./Assets/vswatermark-options.png)

### Use placeholders

Show something different for each file with placeholders.
This can help you (or anyone looking at the screen) differentiate files more easily.

Simply enter one of the following in the configured `Displayed text` and it will be replaced when shown in the editor.

- **`${currentFileName}`** - is replaced with the name of the current file.
- **`${currentDirectoryName}`** - is replaced with the name of the directory (on disk) that contains the current file.
- **`${currentProjectName}`** - is replaced with the name of the project (within the solution) the file is in.

If any of the placeholders can't be resolved, they're replaced with an empty string.  
You can use multiple placeholders at once and combine them with other text.  
Placeholders are not case-sensitive.

### Display Images

Display an image rather than text.

To do this, set the `DisplayedText` to start with **`IMG:`** immediately followed by an absolute path to the file to use.

## Available in the Marketplace

Get it from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=MattLaceyLtd.WaterMark).

## Help make it better

If you have any requests or suggestions, please [raise an issue](https://github.com/mrlacey/VSWaterMark/issues/new).

If you like it, please [leave a review on the marketplace](https://marketplace.visualstudio.com/items?itemName=MattLaceyLtd.WaterMark&ssr=false#review-details).

Help the development and support of this and other extensions by [becoming a sponsor](https://github.com/sponsors/mrlacey).
