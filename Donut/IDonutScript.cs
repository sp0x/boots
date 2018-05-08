using System.Collections.Generic;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutScript
    {
        List<AssignmentExpression> Features { get; set; }
        IList<MatchCondition> Filters { get; set; }
        HashSet<Data.DataIntegration> Integrations { get; set; }
        OrderByExpression StartingOrderBy { get; set; }
        string TargetAttribute { get; set; }
        ScriptTypeInfo Type { get; set; }

        void AddIntegrations(params Data.DataIntegration[] sourceIntegrations);
        DatasetMember GetDatasetMember(string dsName);
        IEnumerable<DatasetMember> GetDatasetMembers();
        Data.DataIntegration GetRootIntegration();
        string ToString();
    }
}