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
using WpfColorHelper;

namespace VSWaterMark
{
	public class WaterMarkAdornment
	{
		private readonly WaterMarkControl _root;
		private readonly IWpfTextView _view;
		private readonly IAdornmentLayer _adornmentLayer;

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
			Messenger.UpdateAdornmentPosition += () => OnUpdatePositionRequested();

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
					// add the watermark to the adornment layer and make it relative to the viewports
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
			ThreadHelper.ThrowIfNotOnUIThread();

			RefreshAdornment();
		}

		private void OnUpdatePositionRequested()
		{
			Canvas.SetLeft(_root, _view.ViewportRight - _root.ActualWidth);
		}

		private void OnSizeChanged()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			RefreshAdornment();
		}

		private void OnViewClosed()
		{
			_view.ViewportHeightChanged -= (sender, e) =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				OnSizeChanged();
			};
			_view.ViewportWidthChanged -= (sender, e) =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				OnSizeChanged();
			};

			_view.Closed -= (s, e) => OnViewClosed();

			Messenger.UpdateAdornment -= () => OnUpdateRequested();
			Messenger.UpdateAdornmentPosition -= () => OnUpdatePositionRequested();
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

				bool textChanged = false;

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
							// Don't detect this on the initial setting. ("@MRLacey" is the default text of the control)
							if (_root.WaterMarkText.Content.ToString() != "@MRLacey")
							{
								textChanged = true;
							}

							_root.WaterMarkText.Content = displayedText;
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
					_root.WaterMarkText.Foreground = ColorHelper.GetColorBrush(options.TextColor);

					if (_root.WaterMarkText.Foreground == null)
					{
						OutputError($"Unable to set the Text Color to {options.TextColor}");
					}
				}
				catch (Exception exc)
				{
					OutputError($"Unable to set the Text Color to {options.TextColor}", exc);
				}

				try
				{
					_root.WaterMarkBorder.Background = ColorHelper.GetColorBrush(options.BorderBackground);

					if (_root.WaterMarkText.Background == null)
					{
						OutputError($"Unable to set the Background Color to {options.BorderBackground}");
					}
				}
				catch (Exception exc)
				{
					OutputError($"Unable to set the Background to {options.BorderBackground}", exc);
				}

				try
				{
					_root.WaterMarkBorder.BorderBrush = ColorHelper.GetColorBrush(options.BorderColor);

					if (_root.WaterMarkText.BorderBrush == null)
					{
						OutputError($"Unable to set the Border Color to {options.BorderColor}");
					}
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

					// Avoid potential message loops when trygint to reposition after text changed.
					if (textChanged)
					{
						_root.RequestRePositionAfterNextMeasure = true;
					}
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

		private void OutputError(string message, Exception exc = null)
		{
			System.Diagnostics.Debug.WriteLine(exc);

			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			WaterMarkOutputPane.Instance.Write($"{message}{Environment.NewLine}");
			WaterMarkOutputPane.Instance.Activate();
		}
	}
}
