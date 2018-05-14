using System;

namespace Netlyt.Service.Models
{

    public enum BehaviourType
    {
        PointerMovement,
        Navigation
    }
    public class EndUserBehaviour : ExtendableObject
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public BehaviourType Type { get; set; }
        public long Duration { get; set; }
        public DateTime Created { get; set; }
        public string Referrer { get; set; }
        public string Url { get; set; }

//        public override EntityDocument GetXlsConverter()
//        {
//            var converter = new XlsConverter<EndUserBehaviour>(base.GetXlsConverter());
//            converter.MapAllProperties();
//            return converter;
//        }
    }
}