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
    }
}
