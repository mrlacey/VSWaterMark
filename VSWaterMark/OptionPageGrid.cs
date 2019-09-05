// <copyright file="OptionPageGrid.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace VSWaterMark
{
    public class OptionPageGrid : DialogPage
    {
        [Category("WaterMark")]
        [DisplayName("Enabled")]
        [Description("Show the watermark.")]
        public bool IsEnabled { get; set; } = true;

        [Category("WaterMark")]
        [DisplayName("Top")]
        [Description("Show the watermark at the top.")]
        public bool PositionTop { get; set; } = false;

        [Category("WaterMark")]
        [DisplayName("Left")]
        [Description("Show the watermark on the left.")]
        public bool PositionLeft { get; set; } = true;

        [Category("Text")]
        [DisplayName("Displayed text")]
        [Description("The text to show in the watermark.")]
        public string DisplayedText { get; set; } = "Go to Tools > Options > Water Mark to change this text.";

        [Category("Text")]
        [DisplayName("Text size")]
        [Description("The size of the text in the watermark.")]
        public double TextSize { get; set; } = 16.0;

        [Category("Text")]
        [DisplayName("Font family")]
        [Description("The name of the font to use.")]
        public string FontFamilyName { get; set; } = "Consolas";

        [Category("Text")]
        [DisplayName("Bold")]
        [Description("Should the text be displayed in bold.")]
        public bool IsFontBold { get; set; } = false;

        [Category("Text")]
        [DisplayName("Color")]
        [Description("Name of the color to use for the text.")]
        public string TextColor { get; set; } = "Red";

        [Category("Background")]
        [DisplayName("Border")]
        [Description("Name of the color to use for the border.")]
        public string BorderColor { get; set; } = "Gray";

        [Category("Background")]
        [DisplayName("Background")]
        [Description("Name of the color to use for the background.")]
        public string BorderBackground { get; set; } = "White";

        [Category("Background")]
        [DisplayName("Margin")]
        [Description("Number of pixels between the border and the edge of the editor.")]
        public double BorderMargin { get; set; } = 10;

        [Category("Background")]
        [DisplayName("Padding")]
        [Description("Number of pixels between the text and the border.")]
        public double BorderPadding { get; set; } = 3;

        [Category("Background")]
        [DisplayName("Opacity")]
        [Description("Strength of the background opacity.")]
        public double BorderOpacity { get; set; } = 0.7;
    }
}
