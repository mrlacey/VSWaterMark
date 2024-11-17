// <copyright file="WaterMarkOutputPane.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace VSWaterMark
{
    public class WaterMarkOutputPane
    {
        private static Guid wxPaneGuid = new Guid("6A924017-BCD6-4237-AF49-F17B9D484E65");

        private static WaterMarkOutputPane instance;

        private readonly IVsOutputWindowPane wmPane;

        private WaterMarkOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow)
            {
                outWindow.GetPane(ref wxPaneGuid, out this.wmPane);

                if (this.wmPane == null)
                {
                    outWindow.CreatePane(ref wxPaneGuid, "Water Mark", 1, 0);
                    outWindow.GetPane(ref wxPaneGuid, out this.wmPane);
                }
            }
        }

        public static WaterMarkOutputPane Instance => instance ?? (instance = new WaterMarkOutputPane());

        public void Write(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.wmPane.OutputStringThreadSafe(message);
        }

        public void Activate()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.wmPane.Activate();
        }
    }
}
