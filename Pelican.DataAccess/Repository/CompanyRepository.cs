using Pelican.DataAccess.Data;
using Pelican.DataAccess.Repository.IRepository;
using Pelican.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Pelican.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository 
    {
        private readonly ApplicationDbContext _db;
        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        public void Save()
        {
           
        }

        public void Update(Company obj)
        {
            _db.Companies.Update(obj);
        }
    }
}
