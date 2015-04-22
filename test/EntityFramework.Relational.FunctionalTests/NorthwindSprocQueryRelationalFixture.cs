﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Relational.FunctionalTests.TestModels.NorthwindSproc;

namespace Microsoft.Data.Entity.Relational.FunctionalTests
{
    public abstract class NorthwindSprocQueryRelationalFixture : NorthwindQueryRelationalFixture
    {
        public override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerOrderHistory>().Key(coh => coh.ProductName);
            modelBuilder.Entity<MostExpensiveProduct>().Key(mep => mep.TenMostExpensiveProducts);
        }
    }
}
