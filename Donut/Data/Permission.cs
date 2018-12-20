using System.ComponentModel.DataAnnotations.Schema;
using Donut.Models;
using Netlyt.Interfaces.Models;

namespace Donut.Data
{
    public class Permission
    {
        public long Id { get; set; }
        public virtual Organization ShareWith { get; set; }
        public virtual Organization Owner { get; set; }
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
        [ForeignKey("DataIntegration")]
        public long? DataIntegrationId { get; set; }
        public virtual DataIntegration DataIntegration { get; set; }
        [ForeignKey("Model")]
        public long? ModelId { get; set; }
        public virtual Model Model { get; set; }
        public bool IsRemote { get; set; }
        public long? RemoteId { get; set; }
    }
}