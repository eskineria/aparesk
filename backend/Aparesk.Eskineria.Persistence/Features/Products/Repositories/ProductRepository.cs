using Aparesk.Eskineria.Domain.Entities;
using Aparesk.Eskineria.Core.Repository.Configuration;
using Aparesk.Eskineria.Core.Repository.Repositories;
using Aparesk.Eskineria.Persistence.Features.Products.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aparesk.Eskineria.Persistence.Features.Products.Repositories;

public sealed class ProductRepository : EfRepository<DbContext, Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext, RepositoryOptions options)
        : base(dbContext, options)
    {
    }
}
