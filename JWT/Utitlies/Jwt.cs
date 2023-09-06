namespace JWT.Utitlies {
    public class Jwt {
        public string Key { get; set; }
        public string IssUse { get; set; }
        public string Audience { get; set; }
        public double DurationInDays { get; set; }
    }
}
