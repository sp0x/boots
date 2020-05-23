using System.Collections.Generic;

namespace Netlyt.Data.SQL
{
    public class QueryExpressionCompiler
    {
        protected List<QueryParameter> Parameters { get; set; }

        public QueryExpressionCompiler(List<QueryParameter> parameters)
        {
            Parameters = parameters;
        }
    }
}