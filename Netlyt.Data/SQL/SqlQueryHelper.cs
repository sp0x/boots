using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NHibernate.SqlCommand;

namespace Netlyt.Data.SQL
{
    public class SqlQueryHelper
    {
        public static string WhereEntities<TRecord>(IEnumerable<TRecord> elements)
            where TRecord : class
        {
            PropertyInfo memberInfo;
            var members = DbQueryProvider.GetInstance().GetKeyMemberValues(elements, out memberInfo);  
            var strIds = string.Join(",", members.Select(x=>x.ToString()));
            var query = new SqlStringBuilder().Add($" `{memberInfo.Name}` in ({strIds})").ToSqlString(); 
            return query.ToString();
        }

    }
}