namespace Donut
{
    public interface IMqConfiguration
    {

        int InputPort { get; set; }
        int OutputPort { get; set; }
        int EventsPort { get; set; }
        string Destination { get; set; }
    }

    public class MqConfiguration
    {
        public int InputPort { get; set; }
        public int OutputPort { get; set; }
        public int EventsPort { get; set; }
        public string Destination { get; set; }
    }
}