namespace PPSLambda.Model
{
    public class EventDetail
    {
        public string EventId { get; }
        public string BusinessUnitId { get;}
        
        public string EventEditionId { get; }

        public EventDetail(string eventId, string businessUnitId, string eventEditionId)
        {
            EventId = eventId;
            BusinessUnitId = businessUnitId;
            EventEditionId = eventEditionId;
        }
    }
}
