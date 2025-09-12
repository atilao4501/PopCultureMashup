namespace PopCultureMashup.Application.Settings
{
    public class RecommendationSettings
    {
        public double SimilarityWeight { get; set; }
        public double PopularityWeight { get; set; }
        public double RecencyWeight { get; set; }
        public double NoveltyWeight { get; set; }
        public bool UseDiversification { get; set; }
        public int DiversificationK { get; set; }

        public double ThemeWeightDefault { get; set; }
        public double GenreWeightDefault { get; set; }
        public double CreatorWeightDefault { get; set; }

        public double ThemeWeightBooks { get; set; }
        public double GenreWeightBooks { get; set; }
        public double CreatorWeightBooks { get; set; }

        public double HalfLifeGames { get; set; }
        public double HalfLifeBooks { get; set; }
    }
}

