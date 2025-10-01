using System;
using System.Collections.Generic;
using System.Threading;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// A thread-safe factory that ensures all instances of type T[] are created on a dedicated thread.
    /// This guarantees that all arrays are allocated on the same thread-local heap.
    /// </summary>
    /// <typeparam name="T">The type of object to create. Must have a parameterless constructor.</typeparam>
    //*******************************************************************************************************
    public class ThreadBoundArrayFactory<T> where T : new()
    {
        //===================================================================================================
        /// <summary>
        /// The dedicated thread responsible for creating all instances of T.
        /// </summary>
        private readonly Thread m_oWorkerThread;

        //===================================================================================================
        /// <summary>
        /// Queue of pending creation requests from other threads.
        /// </summary>
        private readonly Queue<CreationRequest> m_oRequestQueue = new Queue<CreationRequest>();

        //===================================================================================================
        /// <summary>
        /// Lock object to synchronize access to the request queue.
        /// </summary>
        private readonly object m_oLock = new object();

        //===================================================================================================
        /// <summary>
        /// Signal used to notify the worker thread that a new request has arrived.
        /// </summary>
        private readonly AutoResetEvent m_oRequestSignal = new AutoResetEvent(false);

        //===================================================================================================
        /// <summary>
        /// Flag to control the lifetime of the worker thread.
        /// </summary>
        private volatile bool m_bRunning = true;

        //===================================================================================================
        /// <summary>
        /// The lengths of arrays to create
        /// </summary>
        private int m_nLength;

        //***************************************************************************************************
        /// <summary>
        /// Represents a single object creation request.
        /// </summary>
        //***************************************************************************************************
        private class CreationRequest
        {
            //===============================================================================================
            /// <summary>
            /// Event used to signal the requesting thread when the object has been created.
            /// </summary>
            public ManualResetEvent DoneEvent = new ManualResetEvent(false);

            //===============================================================================================
            /// <summary>
            /// The object created by the worker thread.
            /// </summary>
            public T[] CreatedArray;
        }

        //===================================================================================================
        /// <summary>
        /// Initializes a new instance of the ThreadBoundArrayFactory class and starts the worker thread.
        /// </summary>
        /// <param name="length">The length of the array to create.</param>
        //===================================================================================================
        public ThreadBoundArrayFactory(int length)
        {
            m_nLength = length;
            m_oWorkerThread = new Thread(WorkerLoop);
            m_oWorkerThread.IsBackground = true;
            m_oWorkerThread.Start();
        }

        //===================================================================================================
        /// <summary>
        /// The main loop of the worker thread. Waits for signals and processes creation requests.
        /// </summary>
        //===================================================================================================
        private void WorkerLoop()
        {
            while (m_bRunning)
            {
                // Wait until a request is signaled
                m_oRequestSignal.WaitOne();

                CreationRequest oRequest = null;

                // Safely dequeue the next request
                lock (m_oLock)
                {
                    if (m_oRequestQueue.Count > 0)
                    {
                        oRequest = m_oRequestQueue.Dequeue();
                    }
                }

                // Create the object and notify the requesting thread
                if (oRequest != null)
                {
                    oRequest.CreatedArray = new T[m_nLength];
                    oRequest.DoneEvent.Set();
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// Creates a new array of T on the dedicated thread.
        /// </summary>
        /// <returns>The newly created array of type T[].</returns>
        //===================================================================================================
        public T[] Create()
        {
            var oRequest = new CreationRequest();

            // Enqueue the request
            lock (m_oLock)
            {
                m_oRequestQueue.Enqueue(oRequest);
            }

            // Signal the worker thread to process the request
            m_oRequestSignal.Set();

            // Wait until the object is created
            oRequest.DoneEvent.WaitOne();
            return oRequest.CreatedArray;
        }

        //===================================================================================================
        /// <summary>
        /// Stops the worker thread gracefully.
        /// Should be called when the factory is no longer needed.
        /// </summary>
        //===================================================================================================
        public void Stop()
        {
            m_bRunning = false;

            // Wake up the thread so it can exit
            m_oRequestSignal.Set();

            // Wait for the thread to finish
            m_oWorkerThread.Join();
        }
    }
}
