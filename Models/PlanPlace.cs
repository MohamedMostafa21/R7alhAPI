using System;

namespace R7alaAPI.Models
{
    public class PlanPlace
    {
        public int Id { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; }

        public int PlaceId { get; set; }
        public Place Place { get; set; }

        public int? Order { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? AdditionalDescription { get; set; }
        public decimal SpecialPrice { get; set; } = 0m;
    }
}