// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Relational.Query.Expressions;
using Microsoft.Data.Entity.Relational.Query.Sql;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Sqlite.Query
{
    public class SqliteQueryGenerator : DefaultSqlQueryGenerator
    {
        protected override string ConcatOperator => "||";

        public SqliteQueryGenerator([NotNull] SelectExpression selectExpression)
            : base(selectExpression)
        {
        }

        protected override void GenerateTop(SelectExpression selectExpression)
        {
            // Handled by GenerateLimitOffset
        }

        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            Check.NotNull(selectExpression, nameof(selectExpression));

            if (selectExpression.Limit != null || selectExpression.Offset != null)
            {
                Sql.AppendLine()
                    .Append("LIMIT ")
                    .Append(selectExpression.Limit ?? -1);

                if (selectExpression.Offset != null)
                {
                    Sql.Append(" OFFSET ")
                        .Append(selectExpression.Offset);
                }
            }
        }
    }
}
