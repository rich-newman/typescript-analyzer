using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLinterVsix
{
    public class BuildEventsBase : IVsUpdateSolutionEvents2
    {
        private readonly IVsSolution _solution;
        private readonly IVsSolutionBuildManager3 _buildManager;
        private uint _cookie1 = VSConstants.VSCOOKIE_NIL;
        private uint _cookie2 = VSConstants.VSCOOKIE_NIL;
        private uint _cookie3 = VSConstants.VSCOOKIE_NIL;

        public BuildEventsBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            this._solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (this._solution == null)
            {
                throw new InvalidOperationException("Cannot get solution service");
            }
            this._buildManager = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager3;
        }

        public void StartListeningForChanges()
        {
            //ErrorHandler.ThrowOnFailure(this._solution.AdviseSolutionEvents(this, out this._cookie1));
            if (this._buildManager != null)
            {
                var bm2 = this._buildManager as IVsSolutionBuildManager2;
                if (bm2 != null)
                {
                    ErrorHandler.ThrowOnFailure(bm2.AdviseUpdateSolutionEvents(this, out this._cookie2));
                }
                //ErrorHandler.ThrowOnFailure(this._buildManager.AdviseUpdateSolutionEvents3(this, out this._cookie3));
            }
        }

        public void Dispose()
        {
            // Ignore failures in UnadviseSolutionEvents
            if (this._cookie1 != VSConstants.VSCOOKIE_NIL)
            {
                this._solution.UnadviseSolutionEvents(this._cookie1);
                this._cookie1 = VSConstants.VSCOOKIE_NIL;
            }
            if (this._cookie2 != VSConstants.VSCOOKIE_NIL)
            {
                ((IVsSolutionBuildManager2)this._buildManager).UnadviseUpdateSolutionEvents(this._cookie2);
                this._cookie2 = VSConstants.VSCOOKIE_NIL;
            }
            if (this._cookie3 != VSConstants.VSCOOKIE_NIL)
            {
                this._buildManager.UnadviseUpdateSolutionEvents3(this._cookie3);
                this._cookie3 = VSConstants.VSCOOKIE_NIL;
            }
        }

        public virtual int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int UpdateSolution_Cancel()
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public virtual int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}
