// <copyright file="VSWaterMarkPackage.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSWaterMark
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionHasMultipleProjects, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionHasSingleProject, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSWaterMarkPackage.PackageGuidString)]
    [InstalledProductRegistration("#110", "#112", "1.3.1", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideOptionPage(typeof(OptionPageGrid), "Water Mark", "General", 0, 0, true)]
    [ProvideProfileAttribute(typeof(OptionPageGrid), "Water Mark", "General", 106, 107, isToolsOptionPage: true, DescriptionResourceID = 108)]
    public sealed class VSWaterMarkPackage : AsyncPackage
    {
        /// <summary>
        /// VSWaterMarkPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f9d6a658-d02a-4d86-8faa-674b985f1fa3";

#pragma warning disable SA1401 // Fields should be private
        public static VSWaterMarkPackage Instance;
#pragma warning restore SA1401 // Fields should be private

        private DocumentEventHandlers docHandlers;

        public OptionPageGrid Options
        {
            get
            {
                return (OptionPageGrid)this.GetDialogPage(typeof(OptionPageGrid));
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            VSWaterMarkPackage.Instance = this;

            var rdt = await this.GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;

            docHandlers = new DocumentEventHandlers(rdt);

            System.Diagnostics.Debug.WriteLine("InitializeAsync");
            Messenger.RequestUpdateAdornment();
        }
    }
}
