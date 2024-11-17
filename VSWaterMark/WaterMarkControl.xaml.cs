// <copyright file="WaterMarkControl.xaml.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System.Windows;
using System.Windows.Controls;

namespace VSWaterMark
{
	public partial class WaterMarkControl : UserControl
	{
		public WaterMarkControl()
		{
			this.InitializeComponent();
		}

		public bool RequestRePositionAfterNextMeasure { get; set; } = false;

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			System.Diagnostics.Debug.WriteLine("OnRenderSizeChanged");

			if (this.RequestRePositionAfterNextMeasure)
			{
				this.RequestRePositionAfterNextMeasure = false;
				Messenger.RequestUpdateAdornmentPosition();
			}

			base.OnRenderSizeChanged(sizeInfo);
		}
	}
}
