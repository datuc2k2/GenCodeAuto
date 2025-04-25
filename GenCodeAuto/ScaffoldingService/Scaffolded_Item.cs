//using Microsoft.EntityFrameworkCore.Metadata.Internal;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata;
//using System.IO;

//public class ScaffoldingService
//{
//    private readonly GenCodeAutoContext _context;

//    public ScaffoldingService(GenCodeAutoContext context)
//    {
//        _context = context;
//    }

//    public void GenerateScaffolding(string tableName)
//    {
//        var modelType = _context.Model.FindEntityType(tableName);
//        if (modelType == null)
//        {
//            throw new ArgumentException($"Table '{tableName}' not found in the model.");
//        }
//        // Generate Controller class
//        var controllerCode = GenerateControllerCode(modelType);
//        var commonDeleteCode = GenerateDeleteByIdCommandCode(modelType);
//        var queryAllEntityCode = GenerateGetAllEntityQueryCode(modelType);
//        var queryEntityByIdCode = GenerateGetEntityByIdQueryCode(modelType);
//        var DtoCode = GenerateDTOCode(modelType);

//        //Path controller
//        var outputPathController = Path.Combine(Directory.GetCurrentDirectory(), "Controllers", $"{modelType.ClrType.Name}Controller.cs");
//        Directory.CreateDirectory(Path.GetDirectoryName(outputPathController)); // Ensure the directory exists
//        File.WriteAllText(outputPathController, controllerCode);

//        //Path Command 
//        var outputPathCommand = Path.Combine(Directory.GetCurrentDirectory(), "Features",$"{modelType.ClrType.Name}Features", "Command", $"{modelType.ClrType.Name}Command.cs");
//        Directory.CreateDirectory(Path.GetDirectoryName(outputPathCommand)); // Ensure the directory exists
//        File.WriteAllText(outputPathCommand, commonDeleteCode);

//        //Path query
//        //get all entity 
//        var outputPathQuery = Path.Combine(Directory.GetCurrentDirectory(), "Features", $"{modelType.ClrType.Name}Features", "Queries", $"GetAll{modelType.ClrType.Name}Query.cs");
//        Directory.CreateDirectory(Path.GetDirectoryName(outputPathQuery)); // Ensure the directory exists
//        File.WriteAllText(outputPathQuery, queryAllEntityCode);
//        // get entity by id 
//        var outputPathQuery1 = Path.Combine(Directory.GetCurrentDirectory(), "Features", $"{modelType.ClrType.Name}Features", "Queries", $"Get{modelType.ClrType.Name}ByIdQuery.cs");
//        Directory.CreateDirectory(Path.GetDirectoryName(outputPathQuery1)); // Ensure the directory exists
//        File.WriteAllText(outputPathQuery1, queryEntityByIdCode);

//        //Path DTO
//        var outputPathDTO = Path.Combine(Directory.GetCurrentDirectory(), "DTO", $"{modelType.ClrType.Name}DTO.cs");
//        Directory.CreateDirectory(Path.GetDirectoryName(outputPathDTO)); // Ensure the directory exists
//        File.WriteAllText(outputPathDTO, DtoCode);


//        // Optionally, generate model and views here
//        // var modelCode = GenerateModelCode(modelType);
//        // Save generated model to project
//        // File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), $"{modelType.ClrType.Name}.cs"), modelCode);

//        // Generate View templates (omitted for brevity)
//    }

//    private string GenerateControllerCode(IEntityType entityType)
//    {
//        var controllerName = entityType.ClrType.Name + "sController";
//        var modelNamespace = entityType.ClrType.Namespace;
//        var modelName = entityType.ClrType.Name;
//        var modelName1 = entityType.ClrType.Name + "s";

//        var controllerCode = $@"
//using {modelNamespace};
//using Microsoft.AspNetCore.Mvc;
//using MediatR;
//using Microsoft.EntityFrameworkCore;
//using System.Threading.Tasks;
//using genCodeAuto.Features.{modelName}Features.Queries;
//using genCodeAuto.Features.{modelName}Features.Commands;

//namespace genCodeAuto.Controllers
//{{
//    [Route(""api/[controller]"")]
//    [ApiController]
//    public class {controllerName} : ControllerBase
//    {{
//        private readonly GenCodeAutoContext _dbContext;
//        private readonly IMediator _mediator;

//        public {controllerName}(GenCodeAutoContext dbContext,IMediator mediator)
//        {{
//        _dbContext = dbContext;
//        _mediator = mediator;
//        }}

//        // GET: api/{modelName1}
//        [HttpGet]
//        public async Task<IActionResult> Get{modelName1}()
//        {{
//            return Ok(await _mediator.Send(new GetAll{modelName}Query()));
//        }}

//        // GET: api/{modelName1}/5
//        [HttpGet(""{{id}}"")]
//        public async Task<ActionResult<{modelName}>> Get{modelName}(int id)
//        {{
//            return Ok(await _mediator.Send(new Get{modelName}ByIdQuery {{ {modelName}Id = id }}));
//        }}

//        /// PUT: api/{modelName1}/5
//        [HttpPut(""{{id}}"")]
//        public async Task<IActionResult> Put{modelName}(int id, {modelName} {modelName.ToLower()})
//        {{
//            if (id != {modelName.ToLower()}.{modelName}Id )
//            {{
//                return BadRequest();
//            }}

//           _dbContext.Entry({modelName.ToLower()}).State = EntityState.Modified;

//            try
//            {{
//                await _dbContext.SaveChangesAsync();
//            }}
//            catch (DbUpdateConcurrencyException)
//            {{
//                if (!CustomerExists(id))
//                {{
//                    return NotFound();
//                }}
//                else
//                {{
//                    throw;
//                }}
//            }}

//            return NoContent();
//        }}

//        // POST: api/{modelName1}
//        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
//        [HttpPost]
//        public async Task<ActionResult<{modelName}>> Post{modelName}({modelName} {modelName.ToLower()})
//        {{
//            _dbContext.{modelName1}.Add({modelName.ToLower()});
//            await _dbContext.SaveChangesAsync();
//            return CreatedAtAction(""Get{modelName}"", new {{ id = {modelName.ToLower()}.{modelName}Id }}, {modelName.ToLower()});
//        }}

//        // DELETE: api/{modelName1}/5
//        [HttpDelete(""{{id}}"")]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {{
//            return Ok(await _mediator.Send(new Delete{modelName}ByIdCommand {{ {modelName}Id = id}}));
//        }}
//        private bool CustomerExists(int id)
//        {{
//            return _dbContext.{modelName1}.Any(e => e.{modelName}Id == id);
//        }}
//    }}
//}}";
//        return controllerCode;
//    }

//    private string GenerateDeleteByIdCommandCode(IEntityType entityType)
//    {
//        var modelNamespace = entityType.ClrType.Namespace;
//        var modelName = entityType.ClrType.Name;
//        var modelName1 = entityType.ClrType.Name+"s";
//        var commonCode = $@"
//using {modelNamespace};
//using MediatR;
//using genCodeAuto.Models;
//using Microsoft.EntityFrameworkCore;
//namespace genCodeAuto.Features.{modelName}Features.Commands
//{{
//    public class Delete{modelName}ByIdCommand : IRequest<int>
//    {{
//        public int {modelName}Id {{ get; set; }}
//        public class Delete{modelName}ByIdCommandHandler : IRequestHandler<Delete{modelName}ByIdCommand, int>
//        {{
//            private readonly GenCodeAutoContext _dbContext;
//            public Delete{modelName}ByIdCommandHandler(GenCodeAutoContext dbContext)
//            {{
//                _dbContext = dbContext;
//            }}
//            public async Task<int> Handle(Delete{modelName}ByIdCommand command, CancellationToken cancellationToken)
//            {{
//                var {modelName.ToLower()} = await _dbContext.{modelName1}.Where(a => a.{modelName}Id == command.{modelName}Id).FirstOrDefaultAsync();
//                if ({modelName.ToLower()} == null) return default;
//                _dbContext.{modelName1}.Remove({modelName.ToLower()});
//                await _dbContext.SaveChangesAsync();
//                return {modelName.ToLower()}.{modelName}Id;
//            }}
//        }}
//    }}
//}}
//";
//        return commonCode;
//    }

//    private string GenerateGetAllEntityQueryCode(IEntityType entityType)
//    {
//        var modelNamespace = entityType.ClrType.Namespace;
//        var modelName = entityType.ClrType.Name;
//        var modelName1 = entityType.ClrType.Name+"s";
//        var queryCode = $@"
//using {modelNamespace};
//using MediatR;
//using genCodeAuto.Models;
//namespace genCodeAuto.Features.{modelName}Features.Queries
//{{
//    public class GetAll{modelName}Query : IRequest<IEnumerable<{modelName}>>
//    {{
//        public int {modelName}Id {{ get; set; }}
//        public class GetAll{modelName}QueryHandler : IRequestHandler<GetAll{modelName}Query,IEnumerable <{modelName}>>
//        {{
//            private readonly GenCodeAutoContext _dbContext;
//            public GetAll{modelName}QueryHandler(GenCodeAutoContext dbContext)
//            {{
//                _dbContext = dbContext;
//            }}
//                public async Task<IEnumerable<{modelName}>> Handle(GetAll{modelName}Query query , CancellationToken cancellationToken)
//                {{
//                    var {modelName}List =  _dbContext.{modelName1}.ToList();
//                    if ({modelName}List == null)
//                    {{
//                        return null;
//                    }}
//                    return {modelName}List;
//            }}
//        }}
//    }}
//}}
//";
//        return queryCode;
//    }
//    private string GenerateGetEntityByIdQueryCode(IEntityType entityType)
//    {
//        var modelNamespace = entityType.ClrType.Namespace;
//        var modelName = entityType.ClrType.Name;
//        var modelName1 = entityType.ClrType.Name + "s";
//        var queryCode = $@"
//using {modelNamespace};
//using MediatR;
//using genCodeAuto.Models;
//namespace genCodeAuto.Features.{modelName}Features.Queries
//{{
//    public class Get{modelName}ByIdQuery : IRequest<{modelName}>
//    {{
//        public int {modelName}Id {{ get; set; }}
//        public class Get{modelName}ByIdQueryHandler : IRequestHandler<Get{modelName}ByIdQuery, {modelName}>
//        {{
//            private readonly GenCodeAutoContext _dbContext;
//            public Get{modelName}ByIdQueryHandler(GenCodeAutoContext dbContext)
//            {{
//                _dbContext = dbContext;
//            }}
//            public async Task<{modelName}> Handle(Get{modelName}ByIdQuery query , CancellationToken cancellationToken)
//            {{
//                var {modelName.ToLower()} = _dbContext.{modelName1}.Where(a => a.{modelName}Id == query.{modelName}Id).FirstOrDefault();
//                if ({modelName.ToLower()} == null) return null;
//                return {modelName.ToLower()};
//            }}
//        }}
//    }}
//}}
//";
//        return queryCode;
//    }

//    private string GenerateDTOCode(IEntityType entityType)
//    {
//        var modelName = entityType.ClrType.Name;
//        var queryCode = $@"
//namespace genCodeAuto.Dto
//{{
//    public class {modelName}DTO 
//    {{
//        public int {modelName}Id {{ get; set; }}
//        public string name {{ get; set; }}
//    }}
//}}
//";
//        return queryCode;
//    }
//}

