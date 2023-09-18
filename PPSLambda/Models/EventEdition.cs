using System;

namespace PPSLambda.Models
{
    public class  EventEdition
    {
        public string Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PrimaryLocale { get; set; }
    }
}