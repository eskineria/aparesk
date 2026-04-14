using Eskineria.Domain.Entities;
using Eskineria.Core.Repository.Configuration;
using Eskineria.Core.Repository.Repositories;
using Eskineria.Persistence.Features.Products.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Eskineria.Persistence.Features.Products.Repositories;

public sealed class ProductRepository : EfRepository<DbContext, Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext dbContext, RepositoryOptions options)
        : base(dbContext, options)
    {
    }
}
