using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class ResultApprovalRepository : GenericRepository<ResultApproval>, IResultApprovalRepository
    {
        public ResultApprovalRepository(ApplicationDbContext context) : base(context) { }
    }
}
