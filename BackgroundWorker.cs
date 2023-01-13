/****************************** Module Header ******************************\
* Module Name:    BackgroundWorker.cs
* Project:        CSASPNETBackgroundWorker
* Copyright (c) Microsoft Corporation
*
* The BackgroundWorker class calls a method in a separate thread. It allows 
* passing parameters to the method when it is called. And it can let the target 
* method report progress and result.
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
* All other rights reserved.
*
\*****************************************************************************/
using System.Threading;

namespace Traffic
{
    public class BackgroundWorker
    {
        private Thread _innerThread;
        private int _progress;
        private object _result;

        public int Progress => this._progress;

        public object Result => this._result;

        public bool IsRunning => this._innerThread != null && this._innerThread.IsAlive;

        public event BackgroundWorker.DoWorkEventHandler DoWork;

        public void RunWorker(params object[] arguments)
        {
            if (this.DoWork == null)
                return;
            this._innerThread = new Thread((ThreadStart)(() =>
            {
                this._progress = 0;
                this.DoWork(arguments);
                this._progress = 100;
            }));
            this._innerThread.Start();
        }

        public delegate void DoWorkEventHandler(params object[] arguments);
    }
}
