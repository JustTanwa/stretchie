namespace stretchie{
    public enum StepType { Hold, Repetition }

    public class StretchStep
    {
        public string Name { get; set; }
        public StepType Type { get; set; }
        public int TargetCount { get; set; }
        public int DurationSeconds { get; set; }
        public string ImagePath { get; set; }

        // Parameterless constructor required for JSON serialization
        public StretchStep() { }

        public StretchStep(string name, StepType type, int targetCount, int durationSeconds = 0, string imagePath = "")
        {
            Name = name;
            Type = type;
            TargetCount = targetCount;
            DurationSeconds = durationSeconds;
            ImagePath = imagePath;
        }
    }
}
