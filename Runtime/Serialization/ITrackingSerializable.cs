namespace TrackingLogSDK
{
    /// <summary>
    /// Interface for value types or objects that serialize directly into TrackingJsonWriter.
    /// </summary>
    public interface ITrackingSerializable
    {
        void WriteJson(TrackingJsonWriter writer);
    }
}
