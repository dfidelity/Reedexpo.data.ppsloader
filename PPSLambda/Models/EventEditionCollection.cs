using System.Collections.Generic;

namespace PPSLambda.Models
{
    public class EventEditionCollection
    {
        public EventEditionCollection()
        {
            EventEditions = new List<EventEdition>();
        }

        public List<EventEdition> EventEditions { get; set; }
    }
}