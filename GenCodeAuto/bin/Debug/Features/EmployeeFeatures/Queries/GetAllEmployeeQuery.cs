
using MediatR;
using genCodeAuto.Models;
namespace genCodeAuto.Features.EmployeeFeatures.Queries
{
    public class GetAllEmployeeQuery : IRequest<IEnumerable<Employee>>
    {
        public int EmployeeId { get; set; }
        public class GetAllEmployeeQueryHandler : IRequestHandler<GetAllEmployeeQuery,IEnumerable <Employee>>
        {
            private readonly GenCodeAutoContext _dbContext;
            public GetAllEmployeeQueryHandler(GenCodeAutoContext dbContext)
            {
                _dbContext = dbContext;
            }
                public async Task<IEnumerable<Employee>> Handle(GetAllEmployeeQuery query , CancellationToken cancellationToken)
                {
                    var EmployeeList =  _dbContext.Employees.ToList();
                    if (EmployeeList == null)
                    {
                        return null;
                    }
                    return EmployeeList;
            }
        }
    }
}
