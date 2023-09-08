//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Data;

namespace ServiceStack.Data;

public interface IHasDbConnection
{
    IDbConnection DbConnection { get; }
}

public interface IHasDbCommand
{
    IDbCommand DbCommand { get; }
}

public interface IHasDbTransaction
{
    IDbTransaction DbTransaction { get; }
}
