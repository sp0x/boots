using System.Collections.Generic;
using Donut.Data;
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
        IEnumerable<ModelTarget> Targets { get; set; }
        ScriptTypeInfo Type { get; set; }
        string AssemblyPath { get; set; }
        void AddIntegrations(params Data.DataIntegration[] sourceIntegrations);
        DatasetMember GetDatasetMember(string dsName);
        IEnumerable<DatasetMember> GetDatasetMembers();
        Data.DataIntegration GetRootIntegration();
        string ToString();
    }
}