
namespace Worthwhile.JarSync.Core.Config
{
    public class SchedulerConfig
    {
        public const string SECTION_NAME = "Scheduler";

        public string Enable { get; set; } = "1";
        public string CronExpression { get; set; } = "";
        public bool IsEnabled => Enable == "1";

        public void Initialize()
        {
            if (!IsEnabled) return;

            if (string.IsNullOrWhiteSpace(CronExpression))
            {
                throw new Exception("CronExpression is not set");
            }
        }

        public List<DateTime> GenerateTimes(DateTime startDate, DateTime endDate)
        {
            startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, 0);
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, endDate.Hour, endDate.Minute, 0);

            var times = new List<DateTime>();

            var cron = new CronExpression(CronExpression);
            var next = cron.GetNextValidTimeAfter(startDate);

            while (next < endDate)
            {
                times.Add(next);
                next = cron.GetNextValidTimeAfter(next);
            }

            return times;
        }
    }

    public class CronExpression
    {
        public const int MINUTE_INDEX = 0;
        public const int HOUR_INDEX = 1;
        public const int DAY_INDEX = 2;
        public const int MONTH_INDEX = 3;
        public const int DAY_OF_WEEK_INDEX = 4;

        public string Expression { get; set; } = "";
        public string[] Parts { get; set; } = new string[] { };
        public HashSet<DayOfWeek> OnDayOfWeek = new HashSet<DayOfWeek>();

        public CronExpression(string expression)
        {
            Expression = expression;
            Parts = expression.Split(" ");
            Validate();
            InitDaysOfWeek();
        }

        private void Validate()
        {
            Parts[MINUTE_INDEX] = ValidateDigit(Parts[0], 0, 59, "minute");
            Parts[HOUR_INDEX] = ValidateDigit(Parts[1], 0, 23, "hour");
            Parts[DAY_INDEX] = ValidateDigit(Parts[2], 1, 31, "day of month");
        }

        private string ValidateDigit(string input, int min, int max, string errorType)
        {
            input = input.Trim();
            if (input == "*") return input;
            int temp;
            if (!int.TryParse(input, out temp))
            {
                throw new Exception($"Scheduler: cron expression {errorType} must be * or a 2 digit number");
            }
            if (temp < min) {
                throw new Exception($"Scheduler: cron expression {errorType} must be greater than or equal to {min}");
            }
            if (temp > max)
            {
                throw new Exception($"Scheduler: cron expression {errorType} must be less than or equal to {max}");
            }
            return temp.ToString("00");
        }

        private void InitDaysOfWeek()
        {
            if (Parts[DAY_OF_WEEK_INDEX] == "*")
            {
                OnDayOfWeek = new HashSet<DayOfWeek> { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            }
            else
            {
                foreach (var day in Parts[DAY_OF_WEEK_INDEX].Split(","))
                {
                    DayOfWeek dayOfWeek = day.ToUpper() switch
                    {
                        "SUN" => DayOfWeek.Sunday,
                        "MON" => DayOfWeek.Monday,
                        "TUE" => DayOfWeek.Tuesday,
                        "WED" => DayOfWeek.Wednesday,
                        "THU" => DayOfWeek.Thursday,
                        "FRI" => DayOfWeek.Friday,
                        "SAT" => DayOfWeek.Saturday,
                        _ => throw new Exception("Invalid day of week")
                    };
                    OnDayOfWeek.Add(dayOfWeek);
                }
            }
        }

        public DateTime GetNextValidTimeAfter(DateTime after)
        {
            var next = after;

            while (true)
            {
                next = next.AddSeconds(1);

                if (next.Second != 0) continue;

                if (Parts[MINUTE_INDEX] != "*" && !(Parts[MINUTE_INDEX] == next.Minute.ToString("00"))) continue;
                if (Parts[HOUR_INDEX] != "*" && !(Parts[HOUR_INDEX] == next.Hour.ToString("00"))) continue;
                if (Parts[DAY_INDEX] != "*" && !(Parts[DAY_INDEX] == next.Day.ToString("00"))) continue;
                if (Parts[MONTH_INDEX] != "*" && !(Parts[MONTH_INDEX] == next.Month.ToString())) continue;
                if (!OnDayOfWeek.Contains(next.DayOfWeek)) continue;

                break;
            }

            return next;
        }
    }
}
