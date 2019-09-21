// <copyright file="DocumentEventHandlers.cs" company="Matt Lacey Limited">
// Copyright (c) Matt Lacey Limited. All rights reserved.
// </copyright>

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace VSWaterMark
{
    public sealed class DocumentEventHandlers :
        IVsRunningDocTableEvents,
        IVsRunningDocTableEvents2,
        IVsRunningDocTableEvents3
    {
        private readonly IVsRunningDocumentTable rdt;

        public DocumentEventHandlers(IVsRunningDocumentTable vsrdt)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.rdt = vsrdt;

            rdt.AdviseRunningDocTableEvents(this, out uint _);
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        // On Document Opened
        public int OnBeforeDocumentWindowShow(uint itemCookie, int first, IVsWindowFrame wf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Messenger.RequestUpdateAdornment();

            return VSConstants.S_OK;
        }

        // fired when document attributes change, such as renaming, but also dirty state, etc.
        public int OnAfterAttributeChangeEx(uint cookie, uint atts, IVsHierarchy oldHier, uint oldId, string oldPath, IVsHierarchy newHier, uint newId, string newPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Messenger.RequestUpdateAdornment();

            return VSConstants.S_OK;
        }
    }
}
