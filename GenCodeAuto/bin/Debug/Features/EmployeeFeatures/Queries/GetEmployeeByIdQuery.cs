
using MediatR;
using genCodeAuto.Models;
namespace genCodeAuto.Features.EmployeeFeatures.Queries
{
    public class GetEmployeeByIdQuery : IRequest<Employee>
    {
        public int EmployeeId { get; set; }
        public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Employee>
        {
            private readonly GenCodeAutoContext _dbContext;
            public GetEmployeeByIdQueryHandler(GenCodeAutoContext dbContext)
            {
                _dbContext = dbContext;
            }
            public async Task<Employee> Handle(GetEmployeeByIdQuery query , CancellationToken cancellationToken)
            {
                var employee = _dbContext.Employees.Where(a => a.EmployeeId == query.EmployeeId).FirstOrDefault();
                if (employee == null) return null;
                return employee;
            }
        }
    }
}
