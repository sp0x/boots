using Netlyt.Service.Auth;
using Netlyt.Service.Source;

namespace Netlyt.Service.DataSets
{
    public class UserDataSet
    {
        public IEntityCollection InhouseEntities { get; private set; }
        public UserBehaviourSet UserBehaviour { get; private set; }
        public SocialDataSet SocialSet { get; private set; }
        
    }
}