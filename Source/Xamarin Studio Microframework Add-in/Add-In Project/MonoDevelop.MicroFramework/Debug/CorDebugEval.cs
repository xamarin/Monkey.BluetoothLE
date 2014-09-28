using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using BreakpointDef = Microsoft.SPOT.Debugger.WireProtocol.Commands.Debugging_Execution_BreakpointDef;

namespace Microsoft.SPOT.Debugger
{    
    public class CorDebugEval
    {
        const int SCRATCH_PAD_INDEX_NOT_INITIALIZED = -1;

        bool m_fActive;
        CorDebugThread m_threadReal;
        CorDebugThread m_threadVirtual;
        CorDebugAppDomain m_appDomain;
        int m_iScratchPad;
        CorDebugValue m_resultValue;
        EvalResult m_resultType;
        bool m_fException;

        public enum EvalResult
        {
            NotFinished,
            Complete,
            Abort,
            Exception,
        }
        
        public CorDebugEval (CorDebugThread thread) 
        {
            m_appDomain = thread.Chain.ActiveFrame.AppDomain;
            m_threadReal = thread;
            m_resultType = EvalResult.NotFinished;
            ResetScratchPadLocation ();            
        }

        public CorDebugThread ThreadVirtual
        {
            get { return m_threadVirtual; }
        }

        public CorDebugThread ThreadReal
        {
            get { return m_threadReal; }
        }

        public Engine Engine
        {
            get { return Process.Engine; }
        }

        public CorDebugProcess Process
        {
            get { return m_threadReal.Process; }
        }

        public CorDebugAppDomain AppDomain
        {
            get { return m_appDomain; }
        }

        private CorDebugValue GetResultValue ()
        {
            if (m_resultValue == null)
            {
                m_resultValue = Process.ScratchPad.GetValue (m_iScratchPad, m_appDomain);
            }

            return m_resultValue;
        }

        private int GetScratchPadLocation ()
        {
            if (m_iScratchPad == SCRATCH_PAD_INDEX_NOT_INITIALIZED)
            {
                m_iScratchPad = Process.ScratchPad.ReserveScratchBlock ();
            }

            return m_iScratchPad;
        }

        private void ResetScratchPadLocation()
        {
            m_iScratchPad = SCRATCH_PAD_INDEX_NOT_INITIALIZED;
            m_resultValue = null;            
        }

        public void StoppedOnUnhandledException()
        {
            /*
             * store the fact that this eval ended with an exception
             * the exception will be stored in the scratch pad by TinyCLR
             * but the event to cpde should wait to be queued until the eval
             * thread completes.  At that time, the information is lost as to
             * the fact that the result was an exception (rather than the function returning
             * an object of type exception. Hence, this flag.
            */
            m_fException = true;
        }

        public void EndEval (EvalResult resultType, bool fSynchronousEval)
        {
            try
            {
                //This is used to avoid deadlock.  Suspend commands synchronizes on this.Process                
                Process.SuspendCommands(true);

                Debug.Assert(Utility.FImplies(fSynchronousEval, !m_fActive));

                if (fSynchronousEval || m_fActive)  //what to do if the eval isn't active anymore??
                {
                    bool fKillThread = false;

                    if (m_threadVirtual != null)
                    {
                        if (m_threadReal.GetLastCorDebugThread() != m_threadVirtual)
                            throw new ArgumentException();

                        m_threadReal.RemoveVirtualThread(m_threadVirtual);
                    }

                    //Stack frames don't appear if they are not refreshed
                    if (fSynchronousEval)
                    {
                        for (CorDebugThread thread = this.m_threadReal; thread != null; thread = thread.NextThread)
                        {
                            thread.RefreshChain();
                        }
                    }

                    if(m_fException)
                    {
                        resultType = EvalResult.Exception;
                    }

                    //Check to see if we are able to EndEval -- is this the last virtual thread?
                    m_fActive = false;
                    m_resultType = resultType;
                    switch (resultType)
                    {
                        case EvalResult.Complete:
                            Process.EnqueueEvent(new ManagedCallbacks.ManagedCallbackEval(m_threadReal, this, ManagedCallbacks.ManagedCallbackEval.EventType.EvalComplete));
                            break;

                        case EvalResult.Exception:
                            Process.EnqueueEvent(new ManagedCallbacks.ManagedCallbackEval(m_threadReal, this, ManagedCallbacks.ManagedCallbackEval.EventType.EvalException));                             
                            break;

                        case EvalResult.Abort:
                            fKillThread = true;
                            /* WARNING!!!!
                             * If we do not give VS a EvalComplete message within 3 seconds of them calling ICorDebugEval::Abort then VS will attempt a RudeAbort
                             * and will display a scary error message about a serious internal debugger error and ignore all future debugging requests, among other bad things.
                             */
                            Process.EnqueueEvent(new ManagedCallbacks.ManagedCallbackEval(m_threadReal, this, ManagedCallbacks.ManagedCallbackEval.EventType.EvalComplete));
                            break;
                    }

                    if (fKillThread && m_threadVirtual != null)
                    {
                        Engine.KillThread(m_threadVirtual.Id);
                    }

                    if (resultType == EvalResult.Abort)
                    {
                        Process.PauseExecution();
                    }
                }
            }
            finally
            {
                Process.SuspendCommands(false);
            }
        }

        private uint GetTypeDef_Index(CorElementType elementType, CorDebugClass pElementClass)
        {
            uint tdIndex;

            if (pElementClass != null)
            {
                tdIndex = pElementClass.TypeDef_Index;
            }
            else
            {
                CorDebugProcess.BuiltinType builtInType = this.Process.ResolveBuiltInType(elementType);
                tdIndex = builtInType.GetClass(this.m_appDomain).TypeDef_Index;
            }

            return tdIndex;
        }
    }
}
