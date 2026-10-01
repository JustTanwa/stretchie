using stretchie;
using System.Collections.Generic;

namespace stretchie
{
    public class AppSettings
    {
        public int TimerMinutes { get; set; } = 1;
        public List<StretchStep> Exercises { get; set; } = new List<StretchStep>();
    }
}
