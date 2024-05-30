using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace ServiceStack.Aws.DynamoDb;

public class UpdateExpression : UpdateItemRequest
{
    protected IPocoDynamo Db { get; set; }

    protected DynamoMetadataType Table { get; set; }
}

public class UpdateExpression<T> : UpdateExpression
{
    public UpdateExpression(IPocoDynamo db)
        : this(db, db.GetTableMetadata(typeof(T)), null, null) { }

    public UpdateExpression(IPocoDynamo db, DynamoMetadataType table, object hash, object range = null)
    {
        this.Db = db;
        this.Table = table;
        this.TableName = this.Table.Name;
        this.Key = db.Converters.ToAttributeKeyValue(db, table, hash, range);
        this.ReturnValues = ReturnValue.NONE;
    }

    public UpdateExpression<T> AddConditionExpression(string conditionExpr)
    {
        if (this.ConditionExpression == null)
            this.ConditionExpression = conditionExpr;
        else
            this.ConditionExpression += " AND " + conditionExpr;

        return this;
    }

    public UpdateExpression<T> Condition(Expression<Func<T, bool>> conditionExpression)
    {
        var q = PocoDynamoExpression.Create(typeof(T), conditionExpression, paramPrefix: "p");
        return Condition(q.FilterExpression, q.Params, q.Aliases);
    }

    public UpdateExpression<T> Condition(string conditionExpression, Dictionary<string, object> args = null, Dictionary<string, string> aliases = null)
    {
        AddConditionExpression(conditionExpression);

        if (args != null)
        {
            Db.ToExpressionAttributeValues(args).Each(x =>
                this.ExpressionAttributeValues[x.Key] = x.Value);
        }
        if (aliases != null)
        {
            foreach (var entry in aliases)
            {
                this.ExpressionAttributeNames[entry.Key] = entry.Value;
            }
        }

        return this;
    }

    public UpdateExpression<T> Set(Expression<Func<T>> fn)
    {
        var args = fn.AssignedValues();

        if (args != null)
        {
            var hasExpr = UpdateExpression != null
                          && UpdateExpression.IndexOf("SET", StringComparison.OrdinalIgnoreCase) >= 0;

            foreach (var entry in args)
            {
                if (UpdateExpression == null)
                    UpdateExpression = "SET ";
                else if (!hasExpr)
                    UpdateExpression += " SET ";
                else
                    UpdateExpression += ",";

                hasExpr = true;

                var param = ":" + entry.Key;
                this.UpdateExpression += GetMemberName(entry.Key) + " = " + param;
                this.ExpressionAttributeValues[param] = Db.ToAttributeValue(entry.Value);
            }
        }

        return this;
    }

    public UpdateExpression<T> Add(Expression<Func<T>> fn)
    {
        var args = fn.AssignedValues();

        if (args != null)
        {
            var hasExpr = UpdateExpression != null
                          && UpdateExpression.IndexOf("ADD", StringComparison.OrdinalIgnoreCase) >= 0;

            foreach (var entry in args)
            {
                if (UpdateExpression == null)
                    UpdateExpression = "ADD ";
                else if (!hasExpr)
                    UpdateExpression += " ADD ";
                else
                    UpdateExpression += ",";

                hasExpr = true;

                var param = ":" + entry.Key;
                this.UpdateExpression += GetMemberName(entry.Key) + " " + param;
                this.ExpressionAttributeValues[param] = Db.ToAttributeValue(entry.Value);
            }
        }

        return this;
    }

    public UpdateExpression<T> Remove(Func<T, object> fields)
    {
        var args = fields.ToObjectKeys().ToArraySafe();

        if (args != null)
        {
            var hasExpr = UpdateExpression != null
                          && UpdateExpression.IndexOf("REMOVE", StringComparison.OrdinalIgnoreCase) >= 0;

            foreach (var arg in args)
            {
                if (UpdateExpression == null)
                    UpdateExpression = "REMOVE ";
                else if (!hasExpr)
                    UpdateExpression += " REMOVE ";
                else
                    UpdateExpression += ",";

                hasExpr = true;

                this.UpdateExpression += GetMemberName(arg);
            }
        }

        return this;
    }

    public string GetMemberName(string memberName)
    {
        if (DynamoConfig.IsReservedWord(memberName))
        {
            var alias = "#" + memberName.Substring(0, 2).ToUpper();
            bool aliasExists = false;
            foreach (var entry in ExpressionAttributeNames)
            {
                if (entry.Value == memberName)
                    return entry.Key;
                if (entry.Key == alias)
                    aliasExists = true;
            }

            if (aliasExists)
                alias += ExpressionAttributeNames.Count;

            ExpressionAttributeNames[alias] = memberName;
            return alias;
        }

        return memberName;
    }
}