// <copyright file="WaterMarkAdornment.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.VisualStudio.Text.Editor;

namespace VSWaterMark
{
    public class WaterMarkAdornment
    {
#pragma warning disable SA1309 // Field names should not begin with underscore
        private readonly WaterMarkControl _root;
        private readonly IWpfTextView _view;
        private readonly IAdornmentLayer _adornmentLayer;
#pragma warning restore SA1309 // Field names should not begin with underscore

        public WaterMarkAdornment(IWpfTextView view)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            _view = view;
            _root = new WaterMarkControl();

            // Grab a reference to the adornment layer that this adornment should be added to
            _adornmentLayer = view.GetAdornmentLayer(nameof(WaterMarkAdornment));

            // Reposition the adornment whenever the editor window is resized
            _view.ViewportHeightChanged += (sender, e) => { OnSizeChange(); };
            _view.ViewportWidthChanged += (sender, e) => { OnSizeChange(); };

            TryLoadOptions();
        }

        public void OnSizeChange()
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
                catch (Exception exc)
                {
                    OutputError($"Unable to display the water mark{Environment.NewLine}{exc}", exc);
                }
            }
        }

        private bool TryLoadOptions()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (VSWaterMarkPackage.Instance != null)
            {
                var options = VSWaterMarkPackage.Instance?.Options;

                if (!options.IsEnabled)
                {
                    return false;
                }

                try
                {
                    _root.WaterMarkText.Content = options.DisplayedText;
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
                    _root.WaterMarkText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(options.TextColor));
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Text Color to {options.TextColor}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(options.BorderBackground));
                }
                catch (Exception exc)
                {
                    OutputError($"Unable to set the Background to {options.BorderBackground}", exc);
                }

                try
                {
                    _root.WaterMarkBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(options.BorderColor));
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

            return false;
        }

        private void OutputError(string message, Exception exc = null)
        {
            System.Diagnostics.Debug.WriteLine(exc);

            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            WaterMarkOutputPane.Instance.Write($"{message}{Environment.NewLine}");
            WaterMarkOutputPane.Instance.Activate();
        }
    }
}
