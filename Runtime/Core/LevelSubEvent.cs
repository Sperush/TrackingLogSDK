namespace TrackingLogSDK
{
    /// <summary>
    /// Component event recorded during a level run.
    /// </summary>
    public struct LevelSubEvent : ITrackingSerializable
    {
        public string EventName;
        public string Value;
        public float TimeTick;

        public LevelSubEvent(string eventName, string value, float timeTick)
        {
            EventName = eventName ?? string.Empty;
            Value = value ?? string.Empty;
            TimeTick = timeTick;
        }

        public void WriteJson(TrackingJsonWriter writer)
        {
            writer.BeginObject();
            writer.WriteProperty("event_name", EventName);
            writer.WriteProperty("value", Value);
            writer.WriteProperty("time_tick", TimeTick, 3);
            writer.EndObject();
        }
    }
}
