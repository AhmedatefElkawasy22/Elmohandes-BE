namespace elmohandes.Server.Sevises
{
	public interface IGenricRepository<T> where T : class
	{
		ICollection<T> GetAll();
		T GetByID(int id);
		int Update (int id,T entity);
		int Delete (int id);
		int Insert(T entity);
	}
}
