using System;
using System.Data;
using Dapper;

namespace CsrApi.Repositories;

public sealed class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }

    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var guid) => guid,
            byte[] bytes when Guid.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var guid) => guid,
            _ => throw new DataException($"Cannot convert {value.GetType().Name} to Guid.")
        };
    }
}
