// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_ADDRESSABLES && UTILITIES_ASYNC

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Utilities.Async;

namespace Utilities.Extensions
{
    public static class AddressablesExtensions
    {
        public static void Release<T>(this AsyncOperationHandle<T> handle)
            => Release((AsyncOperationHandle)handle);

        public static void Release(this AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                try
                {
                    Addressables.Release(handle);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        /// <summary>
        /// Checks cache, then downloads addressable if needed.
        /// </summary>
        /// <param name="key">Path.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/></param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        public static async Task DownloadAddressableAsync(object key, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            var downloadSizeOp = Addressables.GetDownloadSizeAsync(key);
            long downloadSize;

            try
            {
                downloadSize = await downloadSizeOp.Task.WithCancellation(cancellationToken);
            }
            finally
            {
                downloadSizeOp.Release();
            }

            if (downloadSize > 0)
            {
                await Addressables.DownloadDependenciesAsync(key).AwaitWithProgress(progress, true, cancellationToken);
            }
        }

        /// <summary>
        /// Wait on the <see cref="AsyncOperationHandle{T}"/> with the provided <see cref="IProgress{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"><see cref="AsyncOperationHandle{T}"/></param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/></param>
        /// <param name="autoRelease">Should the <see cref="AsyncOperationHandle{T}"/> be automatically released? Defaults to true.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        public static async Task<T> AwaitWithProgress<T>(this AsyncOperationHandle<T> operation, IProgress<float> progress, bool autoRelease = true, CancellationToken cancellationToken = default)
        {
            Thread backgroundThread = null;

            if (progress != null)
            {
                backgroundThread = new Thread(() => ProgressThread(operation, progress, cancellationToken))
                {
                    IsBackground = true
                };
            }

            T result;

            try
            {
                backgroundThread?.Start();
                result = await operation.Task.WithCancellation(cancellationToken);
            }
            finally
            {
                backgroundThread?.Join();
                progress?.Report(100f);

                var opException = operation.OperationException;

                if (autoRelease)
                {
                    operation.Release();
                }
                if (opException != null)
                {
                    throw opException;
                }
            }

            return result;
        }

        /// <summary>
        /// Wait on the <see cref="AsyncOperationHandle"/> with the provided <see cref="IProgress{T}"/>
        /// </summary>
        /// <param name="operation"><see cref="AsyncOperationHandle"/></param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/></param>
        /// <param name="autoRelease">Should the <see cref="AsyncOperationHandle"/> be automatically released? Defaults to true.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        public static async Task AwaitWithProgress(this AsyncOperationHandle operation, IProgress<float> progress, bool autoRelease = true, CancellationToken cancellationToken = default)
        {
            Thread backgroundThread = null;

            if (progress != null)
            {
                backgroundThread = new Thread(() => ProgressThread(operation, progress, cancellationToken))
                {
                    IsBackground = true
                };
            }

            try
            {
                backgroundThread?.Start();
                await operation.Task.WithCancellation(cancellationToken);
            }
            finally
            {
                backgroundThread?.Join();
                var opException = operation.OperationException;

                if (autoRelease)
                {
                    operation.Release();
                }

                if (opException != null)
                {
                    throw opException;
                }

                progress?.Report(100f);
            }
        }

        private static async void ProgressThread(AsyncOperationHandle handle, IProgress<float> progress, CancellationToken cancellationToken)
        {
            try
            {
                // ensure we're on main thread.
                await Awaiters.UnityMainThread;

                while (handle.IsValid() && !handle.IsDone && !cancellationToken.IsCancellationRequested)
                {
                    if (handle.OperationException != null)
                    {
                        break;
                    }

                    progress.Report(handle.PercentComplete * 100f);
                    await Task.Yield();
                }
            }
            catch (Exception)
            {
                // throw away
            }
        }
    }
}

#endif // UNITY_ADDRESSABLES && UTILITIES_ASYNC
