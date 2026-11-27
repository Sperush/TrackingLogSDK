using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// Fans out each envelope to multiple transports (e.g. HTTP + Console + File).
    /// </summary>
    public sealed class CompositeLogTransport : ILogTransport
    {
        private readonly List<ILogTransport> _transports = new List<ILogTransport>();

        public string Name
        {
            get { return "Composite"; }
        }

        public CompositeLogTransport(IEnumerable<ILogTransport> transports = null)
        {
            if (transports != null)
            {
                foreach (ILogTransport transport in transports)
                {
                    Add(transport);
                }
            }
        }

        public int Count
        {
            get { return _transports.Count; }
        }

        public void Add(ILogTransport transport)
        {
            if (transport != null && !_transports.Contains(transport))
            {
                _transports.Add(transport);
            }
        }

        public void Send(TrackingEnvelope envelope, Action<TransportResult> onComplete)
        {
            if (_transports.Count == 0)
            {
                if (onComplete != null)
                {
                    onComplete(TransportResult.Ok(200));
                }
                return;
            }

            int remaining = _transports.Count;
            bool anyFailure = false;
            bool anyRetryable = false;
            long lastStatus = 200;
            string lastError = string.Empty;
            object sync = new object();

            for (int i = 0; i < _transports.Count; i++)
            {
                ILogTransport transport = _transports[i];
                transport.Send(envelope, result =>
                {
                    bool done = false;
                    lock (sync)
                    {
                        if (!result.Success)
                        {
                            anyFailure = true;
                            if (result.IsRetryable)
                            {
                                anyRetryable = true;
                            }
                            lastStatus = result.StatusCode;
                            lastError = result.Error;
                        }
                        remaining--;
                        if (remaining == 0)
                        {
                            done = true;
                        }
                    }

                    if (done && onComplete != null)
                    {
                        onComplete(anyFailure
                            ? TransportResult.Fail(lastStatus, lastError, anyRetryable)
                            : TransportResult.Ok(200));
                    }
                });
            }
        }

        public bool TrySendImmediate(TrackingEnvelope envelope)
        {
            if (_transports.Count == 0)
            {
                return true;
            }

            bool allOk = true;
            for (int i = 0; i < _transports.Count; i++)
            {
                if (!_transports[i].TrySendImmediate(envelope))
                {
                    allOk = false;
                }
            }
            return allOk;
        }
    }
}
