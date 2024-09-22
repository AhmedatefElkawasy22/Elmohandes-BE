
namespace elmohandes.Server.Sevises
{
	public class GenricRepository<T> : IGenricRepository<T> where T : class
	{
		private readonly ApplicationDbContext _context;
		public GenricRepository(ApplicationDbContext context)
		{
			_context = context;
		}
		public ICollection<T> GetAll()
		{
			return _context.Set<T>().ToList();
		}
		public T? GetByID(int id)
		{
			return _context.Set<T>().Find(id);
		}
		public int Delete(int id)
		{
			T? entity = GetByID(id);
            if (entity is not null)
            {
                _context.Set<T>().Remove(entity);
				return _context.SaveChanges();
            }
			return 0;
        }

		public int Insert(T entity)
		{
			_context.Set<T>().Add(entity);
			return _context.SaveChanges();
		}

		public int Update(int id, T entity)
		{
			T? old = GetByID(id);
			if (old is not null)
			{
				// Detach the old entity to prevent tracking issues
				_context.Entry(old).State = EntityState.Detached;
				// Attach the new entity and set its state to Modified
				_context.Set<T>().Attach(entity);
				_context.Entry(entity).State = EntityState.Modified;

				return _context.SaveChanges();
			}
			return 0;
		}
	}
}
