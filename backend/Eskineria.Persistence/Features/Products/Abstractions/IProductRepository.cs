using Eskineria.Domain.Entities;
using Eskineria.Core.Repository.Repositories;

namespace Eskineria.Persistence.Features.Products.Abstractions;

public interface IProductRepository : IEntityRepository<Product>;
