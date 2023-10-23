// <copyright file="WaterMarkAdornment.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VSWaterMark
{
    public class WaterMarkAdornment
    {
#pragma warning disable SA1309 // Field names should not begin with underscore
        private readonly WaterMarkControl _root;
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _adornmentLayer;
#pragma warning restore SA1309 // Field names should not begin with underscore

        private string fileName = null;

        public WaterMarkAdornment(IWpfTextView view)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            _view = view;
            _root = new WaterMarkControl();

            // Grab a reference to the adornment layer that this adornment should be added to
            _adornmentLayer = view.GetAdornmentLayer(nameof(WaterMarkAdornment));

            // Reposition the adornment whenever the editor window is resized
            _view.ViewportHeightChanged += (sender, e) => OnSizeChanged();
            _view.ViewportWidthChanged += (sender, e) => OnSizeChanged();

            _view.Closed += (s, e) => OnViewClosed();

            Messenger.UpdateAdornment += () => OnUpdateRequested();

            RefreshAdornment();
        }

        public void RefreshAdornment()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            // clear the adornment layer of previous adornments
            _adornmentLayer.RemoveAdornment(_root);

            if (TryLoadOptions())
            {
                try
                {
                    // add the image to the adornment layer and make it relative to the viewports
                    _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _root, null);
                }
                catch (InvalidOperationException ioe)
                {
                    OutputError("Unable to display the water mark at this time due to layout issues.");
                    System.Diagnostics.Debug.WriteLine(ioe);
                }
                catch (ArgumentException argexc)
                {
                    // This started happening when document is first loading in ~vs.17.3
                    if (!argexc.StackTrace.Contains("System.Windows.Media.VisualCollection.Add("))
                    {
                        OutputError($"Unable to display the watermark{Environment.NewLine}{argexc}", argexc);
                    }
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to display the water mark{Environment.NewLine}{exc}", exc);
                }
            }
        }

        private void OnUpdateRequested()
        {
            RefreshAdornment();
        }

        private void OnSizeChanged()
        {
            RefreshAdornment();
        }

        private void OnViewClosed()
        {
            _view.ViewportHeightChanged -= (sender, e) => OnSizeChanged();
            _view.ViewportWidthChanged -= (sender, e) => OnSizeChanged();

            _view.Closed -= (s, e) => OnViewClosed();

            Messenger.UpdateAdornment -= () => OnUpdateRequested();
        }

        private string GetFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (fileName == null)
            {
                _view.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer buffer);

                if (buffer is IPersistFileFormat pff)
                {
                    pff.GetCurFile(out fileName, out _);
                }
            }

            return fileName;
        }

        private bool TryLoadOptions()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (VSWaterMarkPackage.Instance != null)
            {
                var options = VSWaterMarkPackage.Instance?.Options;

                if (!options.IsEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("options not enabled");
                    return false;
                }

                try
                {
                    string ReplaceIgnoreCase(string input, string replace, Func<string> with)
                    {
                        var cfPos = input.IndexOf(replace, StringComparison.InvariantCultureIgnoreCase);

                        if (cfPos >= 0)
                        {
                            return input.Substring(0, cfPos) + with.Invoke() + input.Substring(cfPos + replace.Length);
                        }

                        return input;
                    }

                    var displayedText = options.DisplayedText;

                    if (displayedText.StartsWith("IMG:"))
                    {
                        try
                        {
                            _root.WaterMarkImage.Visibility = Visibility.Visible;
                            _root.WaterMarkText.Visibility = Visibility.Hidden;

                            var path = displayedText.Substring(4);

                            if (System.IO.File.Exists(path))
                            {
                                _root.WaterMarkImage.Source = new BitmapImage(new Uri(path));
                            }
                            else
                            {
                                OutputError($"Specified image not found: '{path}'");
                            }
                        }
                        catch (Exception exc)
                        {
                            OutputError($"Unable to set image.", exc);
                        }
                    }
                    else
                    {
                        _root.WaterMarkImage.Visibility = Visibility.Hidden;
                        _root.WaterMarkText.Visibility = Visibility.Visible;

                        if (displayedText.ToLowerInvariant().Contains("${current"))
                        {
                            var curFile = GetFileName();

                            if (!string.IsNullOrWhiteSpace(curFile))
                            {
                                displayedText = ReplaceIgnoreCase(
                                                    displayedText,
                                                    "${currentFileName}",
                                                    () =>
                                                    {
                                                        try
                                                        {
                                                            return System.IO.Path.GetFileName(curFile);
                                                        }
                                                        catch (Exception exc)
                                                        {
                                                            OutputError($"Unable to get the name of the file from the path '{curFile}'.");
                                                            System.Diagnostics.Debug.WriteLine(exc);
                                                            return string.Empty;
                                                        }
                                                    });

                                displayedText = ReplaceIgnoreCase(
                                                    displayedText,
                                                    "${currentDirectoryName}",
                                                    () =>
                                                    {
                                                        try
                                                        {
                                                            return new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(curFile)).Name;
                                                        }
                                                        catch (Exception exc)
                                                        {
                                                            OutputError($"Unable to get the name of the dirctory from the current file path '{curFile}'.");
                                                            System.Diagnostics.Debug.WriteLine(exc);
                                                            return string.Empty;
                                                        }
                                                    });

                                var projItem = ProjectHelpers.Dte2.Solution.FindProjectItem(curFile);

                                displayedText = ReplaceIgnoreCase(
                                                    displayedText,
                                                    "${currentProjectName}",
                                                    () =>
                                                    {
                                                        try
                                                        {
                                                            ThreadHelper.ThrowIfNotOnUIThread();

                                                            return projItem?.ContainingProject.Name ?? string.Empty;
                                                        }
                                                        catch (Exception exc)
                                                        {
                                                            OutputError("Unable to get the name of the project the current file is in.");
                                                            System.Diagnostics.Debug.WriteLine(exc);
                                                            return string.Empty;
                                                        }
                                                    });
                            }
                            else
                            {
                                // We try and refresh/reload/reposition the adornment any time somethign relevant happens.
                                // This includes when documents are initially opened and resized but before the file name is available.
                                // Avoid filling the output window with these messages when there's nothing the user can do about them.
                                //OutputError("Unable to get name of the current file.");
                                System.Diagnostics.Debug.WriteLine("Unable to get name of the current file.");
                            }
                        }

                        if (!_root.WaterMarkText.Content.ToString().Equals(displayedText))
                        {
                            // TODO: If right-aligned, need to remeasure the width appropriately | See  #9
                            _root.WaterMarkText.Content = displayedText;
                            ////_ = System.Threading.Tasks.Task.Delay(200).ConfigureAwait(true);
                            ////_root.Measure((_view as FrameworkElement).RenderSize);

                            // Need to force a reshresh after the content has been changed to ensure it gets aligned correctly.
                            ////ThreadHelper.JoinableTaskFactory.Run(async () =>
                            ////{
                            ////    // A small pause for the adornment to be drawn at the new size and then request update to pick up new width.
                            ////    await System.Threading.Tasks.Task.Delay(200);

                            ////    ////Messenger.RequestUpdateAdornment();
                            ////    ///
                            ////    RefreshAdornment();
                            ////});
                        }
                    }
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the text to {options.DisplayedText}", exc);
                }

                try
                {
                    _root.WaterMarkText.FontSize = options.TextSize;
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the FontSize to {options.TextSize}", exc);
                }

                try
                {
                    _root.WaterMarkText.FontFamily = new FontFamily(options.FontFamilyName);
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the FontFamily to {options.FontFamilyName}", exc);
                }

                try
                {
                    _root.WaterMarkText.FontWeight = options.IsFontBold ? FontWeights.Bold : FontWeights.Normal;
                }
                catch (Exception exc)
                {
                    OutputError("Unable to set the FontWeight", exc);
                }

                try
                {
                    _root.WaterMarkText.Foreground = GetColorBrush(options.TextColor);
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Text Color to {options.TextColor}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.Background = GetColorBrush(options.BorderBackground);
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Background to {options.BorderBackground}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.BorderBrush = GetColorBrush(options.BorderColor);
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Border Color to {options.BorderColor}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.Padding = new Thickness(options.BorderPadding);
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Padding to {options.BorderPadding}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.Margin = new Thickness(options.BorderMargin);
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Margin to {options.BorderMargin}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.Opacity = options.BorderOpacity;
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Opacity to {options.BorderOpacity}", exc);
                }

                if (options.PositionTop)
                {
                    Canvas.SetTop(_root, _view.ViewportTop);
                }
                else
                {
                    Canvas.SetTop(_root, _view.ViewportBottom - _root.ActualHeight);
                }

                if (options.PositionLeft)
                {
                    Canvas.SetLeft(_root, _view.ViewportLeft);
                }
                else
                {
                    Canvas.SetLeft(_root, _view.ViewportRight - _root.ActualWidth);
                }

                return !string.IsNullOrWhiteSpace(options.DisplayedText);
            }

            System.Diagnostics.Debug.WriteLine("Package not loaded");

            // Try and load the package so it's there the next time try to access it.
            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                Guid packageToBeLoadedGuid = new Guid(VSWaterMarkPackage.PackageGuidString);
                shell.LoadPackage(ref packageToBeLoadedGuid, out _);
            }

            return false;
        }

        private SolidColorBrush GetColorBrush(string color)
        {
            if (!color.TrimStart().StartsWith("#"))
            {
                color = this.GetHexForNamedColor(color.Trim());
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.Trim()));
        }

        private void OutputError(string message, Exception exc = null)
        {
            System.Diagnostics.Debug.WriteLine(exc);

            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            WaterMarkOutputPane.Instance.Write($"{message}{Environment.NewLine}");
            WaterMarkOutputPane.Instance.Activate();
        }

        private string GetHexForNamedColor(string colorName)
        {
            switch (colorName.ToLowerInvariant())
            {
                case "aliceblue": return "#F0F8FF";
                case "antiquewhite": return "#FAEBD7";
                case "aqua": return "#00FFFF";
                case "aquamarine": return "#7FFFD4";
                case "azure": return "#F0FFFF";
                case "beige": return "#F5F5DC";
                case "bisque": return "#FFE4C4";
                case "black": return "#000000";
                case "blanchedalmond": return "#FFEBCD";
                case "blue": return "#0000FF";
                case "blueviolet": return "#8A2BE2";
                case "brown": return "#A52A2A";
                case "burgendy": return "#FF6347";
                case "burlywood": return "#DEB887";
                case "cadetblue": return "#5F9EA0";
                case "chartreuse": return "#7FFF00";
                case "chocolate": return "#D2691E";
                case "coral": return "#FF7F50";
                case "cornflowerblue": return "#6495ED";
                case "cornsilk": return "#FFF8DC";
                case "crimson": return "#DC143C";
                case "cyan": return "#00FFFF";
                case "darkblue": return "#00008B";
                case "darkcyan": return "#008B8B";
                case "darkgoldenrod": return "#B8860B";
                case "darkgray": return "#A9A9A9";
                case "darkgreen": return "#006400";
                case "darkgrey": return "#A9A9A9";
                case "darkkhaki": return "#BDB76B";
                case "darkmagenta": return "#8B008B";
                case "darkolivegreen": return "#556B2F";
                case "darkorange": return "#FF8C00";
                case "darkorchid": return "#9932CC";
                case "darkred": return "#8B0000";
                case "darksalmon": return "#E9967A";
                case "darkseagreen": return "#8FBC8B";
                case "darkslateblue": return "#483D8B";
                case "darkslategray": return "#2F4F4F";
                case "darkslategrey": return "#2F4F4F";
                case "darkturquoise": return "#00CED1";
                case "darkviolet": return "#9400D3";
                case "darkyellow": return "#D7C32A";
                case "deeppink": return "#FF1493";
                case "deepskyblue": return "#00BFFF";
                case "dimgray": return "#696969";
                case "dimgrey": return "#696969";
                case "dodgerblue": return "#1E90FF";
                case "firebrick": return "#B22222";
                case "floralwhite": return "#FFFAF0";
                case "forestgreen": return "#228B22";
                case "fuchsia": return "#FF00FF";
                case "gainsboro": return "#DCDCDC";
                case "ghostwhite": return "#F8F8FF";
                case "gold": return "#FFD700";
                case "goldenrod": return "#DAA520";
                case "gray": return "#808080";
                case "green": return "#008000";
                case "greenyellow": return "#ADFF2F";
                case "grey": return "#808080";
                case "honeydew": return "#F0FFF0";
                case "hotpink": return "#FF69B4";
                case "indianred": return "#CD5C5C";
                case "indigo": return "#4B0082";
                case "ivory": return "#FFFFF0";
                case "khaki": return "#F0E68C";
                case "lavender": return "#E6E6FA";
                case "lavenderblush": return "#FFF0F5";
                case "lawngreen": return "#7CFC00";
                case "lemonchiffon": return "#FFFACD";
                case "lightblue": return "#ADD8E6";
                case "lightcoral": return "#F08080";
                case "lightcyan": return "#E0FFFF";
                case "lightgoldenrodyellow": return "#FAFAD2";
                case "lightgray": return "#D3D3D3";
                case "lightgreen": return "#90EE90";
                case "lightgrey": return "#d3d3d3";
                case "lightpink": return "#FFB6C1";
                case "lightsalmon": return "#FFA07A";
                case "lightseagreen": return "#20B2AA";
                case "lightskyblue": return "#87CEFA";
                case "lightslategray": return "#778899";
                case "lightslategrey": return "#778899";
                case "lightsteelblue": return "#B0C4DE";
                case "lightyellow": return "#FFFFE0";
                case "lime": return "#00FF00";
                case "limegreen": return "#32CD32";
                case "linen": return "#FAF0E6";
                case "magenta": return "#FF00FF";
                case "maroon": return "#800000";
                case "mediumaquamarine": return "#66CDAA";
                case "mediumblue": return "#0000CD";
                case "mediumorchid": return "#BA55D3";
                case "mediumpurple": return "#9370DB";
                case "mediumseagreen": return "#3CB371";
                case "mediumslateblue": return "#7B68EE";
                case "mediumspringgreen": return "#00FA9A";
                case "mediumturquoise": return "#48D1CC";
                case "mediumvioletred": return "#C71585";
                case "midnightblue": return "#191970";
                case "mint": return "#66CDAA";
                case "mintcream": return "#F5FFFA";
                case "mistyrose": return "#FFE4E1";
                case "moccasin": return "#FFE4B5";
                case "navajowhite": return "#FFDEAD";
                case "navy": return "#000080";
                case "ochre": return "#D7C32A";
                case "oldlace": return "#FDF5E6";
                case "olive": return "#808000";
                case "olivedrab": return "#6B8E23";
                case "orange": return "#FFA500";
                case "orangered": return "#FF4500";
                case "orchid": return "#DA70D6";
                case "palegoldenrod": return "#EEE8AA";
                case "palegreen": return "#98FB98";
                case "paleturquoise": return "#AFEEEE";
                case "palevioletred": return "#DB7093";
                case "papayawhip": return "#FFEFD5";
                case "peachpuff": return "#FFDAB9";
                case "peru": return "#CD853F";
                case "pink": return "#FFC0CB";
                case "plum": return "#DDA0DD";
                case "powderblue": return "#B0E0E6";
                case "purple": return "#800080";
                case "pumpkin": return "#FF4500";
                case "rebeccapurple": return "#663399";
                case "red": return "#FF0000";
                case "rosybrown": return "#BC8F8F";
                case "royalblue": return "#4169E1";
                case "saddlebrown": return "#8B4513";
                case "salmon": return "#FA8072";
                case "sandybrown": return "#F4A460";
                case "seagreen": return "#2E8B57";
                case "seashell": return "#FFF5EE";
                case "sienna": return "#A0522D";
                case "silver": return "#C0C0C0";
                case "skyblue": return "#87CEEB";
                case "slateblue": return "#6A5ACD";
                case "slategray": return "#708090";
                case "slategrey": return "#708090";
                case "snow": return "#FFFAFA";
                case "springgreen": return "#00FF7F";
                case "steelblue": return "#4682B4";
                case "tan": return "#D2B48C";
                case "teal": return "#008080";
                case "thistle": return "#D8BFD8";
                case "tomato": return "#FF6347";
                case "turquoise": return "#40E0D0";
                case "violet": return "#EE82EE";
                case "volt": return "#CEFF00";
                case "wheat": return "#F5DEB3";
                case "white": return "#FFFFFF";
                case "whitesmoke": return "#F5F5F5";
                case "yellow": return "#FFFF00";
                case "yellowgreen": return "#9ACD32";
                default: return colorName;
            }
        }
    }
}
