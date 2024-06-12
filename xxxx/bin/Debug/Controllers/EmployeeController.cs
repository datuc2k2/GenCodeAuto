
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using genCodeAuto.Features.EmployeeFeatures.Queries;
using genCodeAuto.Features.EmployeeFeatures.Commands;

namespace genCodeAuto.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly GenCodeAutoContext _dbContext;
        private readonly IMediator _mediator;

        public EmployeesController(GenCodeAutoContext dbContext,IMediator mediator)
        {
        _dbContext = dbContext;
        _mediator = mediator;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            return Ok(await _mediator.Send(new GetAllEmployeeQuery()));
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            return Ok(await _mediator.Send(new GetEmployeeByIdQuery { EmployeeId = id }));
        }

        /// PUT: api/Employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, Employee employee)
        {
            if (id != employee.EmployeeId )
            {
                return BadRequest();
            }

           _dbContext.Entry(employee).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Employees
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction("GetEmployee", new { id = employee.EmployeeId }, employee);
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            return Ok(await _mediator.Send(new DeleteEmployeeByIdCommand { EmployeeId = id}));
        }
        private bool CustomerExists(int id)
        {
            return _dbContext.Employees.Any(e => e.EmployeeId == id);
        }
    }
}