using System;
using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Writes formatted telemetry payloads to Unity's Console for local debugging.
    /// </summary>
    public sealed class ConsoleLogTransport : ILogTransport
    {
        public string Name
        {
            get { return "Console"; }
        }

        public void Send(TrackingEnvelope envelope, Action<TransportResult> onComplete)
        {
            Log(envelope);
            if (onComplete != null)
            {
                onComplete(TransportResult.Ok(200));
            }
        }

        public bool TrySendImmediate(TrackingEnvelope envelope)
        {
            Log(envelope);
            return true;
        }

        private static void Log(TrackingEnvelope envelope)
        {
            if (envelope == null)
            {
                return;
            }

            Debug.Log("[TrackingLog][" + envelope.Collection + "/" + envelope.ActionName + "] " + envelope.JsonBody);
        }
    }
}
