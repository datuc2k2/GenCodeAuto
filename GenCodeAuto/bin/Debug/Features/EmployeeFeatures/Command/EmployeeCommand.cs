
using MediatR;
using genCodeAuto.Models;
using Microsoft.EntityFrameworkCore;
namespace genCodeAuto.Features.EmployeeFeatures.Commands
{
    public class DeleteEmployeeByIdCommand : IRequest<int>
    {
        public int EmployeeId { get; set; }
        public class DeleteEmployeeByIdCommandHandler : IRequestHandler<DeleteEmployeeByIdCommand, int>
        {
            private readonly GenCodeAutoContext _dbContext;
            public DeleteEmployeeByIdCommandHandler(GenCodeAutoContext dbContext)
            {
                _dbContext = dbContext;
            }
            public async Task<int> Handle(DeleteEmployeeByIdCommand command, CancellationToken cancellationToken)
            {
                var employee = await _dbContext.Employees.Where(a => a.EmployeeId == command.EmployeeId).FirstOrDefaultAsync();
                if (employee == null) return default;
                _dbContext.Employees.Remove(employee);
                await _dbContext.SaveChangesAsync();
                return employee.EmployeeId;
            }
        }
    }
}
