// <copyright file="Messenger.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

namespace VSWaterMark
{
	public static class Messenger
	{
		public delegate void UpdateAdornmentEventHandler();

		public static event UpdateAdornmentEventHandler UpdateAdornment;

		public static void RequestUpdateAdornment()
		{
			System.Diagnostics.Debug.WriteLine("RequestUpdateAdornment");
			UpdateAdornment?.Invoke();
		}

		public delegate void UpdateAdornmentPositionEventHandler();

		public static event UpdateAdornmentPositionEventHandler UpdateAdornmentPosition;

		public static void RequestUpdateAdornmentPosition()
		{
			System.Diagnostics.Debug.WriteLine("RequestUpdateAdornmentPosition");
			UpdateAdornmentPosition?.Invoke();
		}
	}
}
