using CyberpunkGenerator.Data;
using CyberpunkGenerator.Economy;

namespace CyberpunkGenerator.Models
{
    public class Pop : IZoneable
    {
        private static readonly Random _rng = new Random();

        public string Name { get; set; }
        public int Size { get; set; }
        public PopSocioeconomicClass SocioeconomicClass { get; set; }
        public PopField Field { get; set; }
        public bool IsEmployed { get; set; }

        public PopSocioeconomicClass? TargetClass => SocioeconomicClass;
        public PlacementType PlacementType => PlacementType.Residential;
        public int PlacementSeed { get; } = _rng.Next();

        // Dynamically calculates required space based on the central blueprints
        public int RequiredSqm => Size * EconomyBlueprints.SqmPerPerson[SocioeconomicClass];

        // ── Bidirectional patronage tracking ─────────────────────────────────

        /// <summary>
        /// All patronage links where this pop is the consumer.
        /// A pop may have multiple links for the same good type if no single
        /// business could fulfill its entire need.
        /// </summary>
        public List<Patronage> OutboundPatronage { get; } = new();

        /// <summary>
        /// Releases all patronage links, freeing reserved capacity on all
        /// supplier businesses. Called when this pop is evicted during
        /// displacement. The pop re-forms links when re-placed.
        /// </summary>
        public void ReleaseAllLinks()
        {
            foreach (var patronage in OutboundPatronage)
            {
                patronage.Supplier.ReleaseCapacity(patronage.Good, patronage.Quantity);
                patronage.Supplier.InboundPatronage.Remove(patronage);
            }
            OutboundPatronage.Clear();
        }

        public Pop Split(int sizeToExtract)
        {
            if (sizeToExtract >= Size || sizeToExtract <= 0)
                throw new ArgumentException("Invalid split amount.");

            Size -= sizeToExtract;

            return new Pop
            {
                Name = this.Name,
                SocioeconomicClass = this.SocioeconomicClass,
                Field = this.Field,
                Size = sizeToExtract,
                IsEmployed = this.IsEmployed
            };
        }

        public override string ToString() => $"{Size} {SocioeconomicClass} {Field}s ({Name})";
    }
}