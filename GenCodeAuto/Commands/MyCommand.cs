using System;
using System.ComponentModel.Design;
using System.Globalization;
using EnvDTE;
using Project = EnvDTE.Project;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using EnvDTE80;

namespace GenCodeAuto
{
    internal sealed class MyCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("4041a9f9-4161-450a-9683-67c84d756905");


        private readonly AsyncPackage package;

        private MyCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static MyCommand Instance { get; private set; }

        private IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in MyCommand's constructor requires the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new MyCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var selectedItem = await GetSelectedItemAsync();
            if (selectedItem != null)
            {
                var tableName = GetTableNameFromFileName(selectedItem.Name);
                var dbContextFile = FindDbContextFile(selectedItem);
                Console.WriteLine(dbContextFile.Name);
                if (dbContextFile != null)
                {
                    GenerateScaffolding(tableName, dbContextFile);
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "DbContext file not found in the Models folder.",
                        "Error",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
            else
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Please select a file.",
                    "Error",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
        private ProjectItem FindDbContextFile(ProjectItem selectedItem)
        {
            if (selectedItem.ContainingProject != null)
            {
                var projectItems = selectedItem.ContainingProject.ProjectItems;
                foreach (ProjectItem item in projectItems)
                {
                    Console.WriteLine(item.Name);
                    if (item.Name.EndsWith("Context.cs"))
                    {
                        return item;
                    }

                    if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                    {
                        var dbContextFile = FindDbContextFileRecursive(item.ProjectItems);
                        if (dbContextFile != null)
                        {
                            return dbContextFile;
                        }
                    }
                }
            }
            return null;
        }

        private ProjectItem FindDbContextFileRecursive(ProjectItems projectItems)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (item.Name.EndsWith("Context.cs"))
                {
                    return item;
                }

                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    var dbContextFile = FindDbContextFileRecursive(item.ProjectItems);
                    if (dbContextFile != null)
                    {
                        return dbContextFile;
                    }
                }
            }
            return null;
        }

        private async Task<ProjectItem> GetSelectedItemAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var monitorSelection = await ServiceProvider.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection == null) return null;

            monitorSelection.GetCurrentSelection(out IntPtr hierarchyPointer, out uint itemId, out _, out _);
            if (hierarchyPointer == IntPtr.Zero) return null;

            try
            {
                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPointer) as IVsHierarchy;
                if (hierarchy == null) return null;

                hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out object selectedObject);
                return selectedObject as ProjectItem;
            }
            finally
            {
                if (hierarchyPointer != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPointer);
                }
            }
        }




        private string GetTableNameFromFileName(string fileName)
        {
            // Xử lý tên file để lấy tên bảng
            // Ví dụ: Nếu tên file là "MyTable.cs", bạn có thể trích xuất "MyTable" từ đây
            return Path.GetFileNameWithoutExtension(fileName);
        }
        private void GenerateScaffolding(string tableName, ProjectItem dbContextFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get DTE service
            DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Project activeProject = GetActiveProject(dte);

            if (activeProject == null)
            {
                throw new InvalidOperationException("No active project found.");
            }
            string activeProjectPath = Path.GetDirectoryName(activeProject.FullName);
            string dbContextPath = dbContextFile.Name;
            string modelType = tableName;
            string dbContextType = Path.GetFileNameWithoutExtension(dbContextFile.Name);
            //string dbContextType = dbContextPath.Remove(dbContextPath.Length - 3);
            // Generate Controller class
            var controllerCode = GenerateControllerCode(modelType, dbContextType);
            var commonDeleteCode = GenerateDeleteByIdCommandCode(modelType, dbContextType);
            var queryAllEntityCode = GenerateGetAllEntityQueryCode(modelType, dbContextType);
            var queryEntityByIdCode = GenerateGetEntityByIdQueryCode(modelType, dbContextType);
            var DtoCode = GenerateDTOCode(modelType, dbContextType);

            //Path controller
            var outputPathController = Path.Combine(activeProjectPath, "Controllers", $"{modelType}Controller.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPathController)); // Ensure the directory exists
            File.WriteAllText(outputPathController, controllerCode);

            //Path Command 
            var outputPathCommand = Path.Combine(activeProjectPath, "Features", $"{modelType}Features", "Command", $"{modelType}Command.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPathCommand)); // Ensure the directory exists
            File.WriteAllText(outputPathCommand, commonDeleteCode);

            //Path query
            //get all entity 
            var outputPathQuery = Path.Combine(activeProjectPath, "Features", $"{modelType}Features", "Queries", $"GetAll{modelType}Query.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPathQuery)); // Ensure the directory exists
            File.WriteAllText(outputPathQuery, queryAllEntityCode);
            // get entity by id 
            var outputPathQuery1 = Path.Combine(activeProjectPath, "Features", $"{modelType}Features", "Queries", $"Get{modelType}ByIdQuery.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPathQuery1)); // Ensure the directory exists
            File.WriteAllText(outputPathQuery1, queryEntityByIdCode);

            //Path DTO
            var outputPathDTO = Path.Combine(activeProjectPath, "DTO", $"{modelType}DTO.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPathDTO)); // Ensure the directory exists
            File.WriteAllText(outputPathDTO, DtoCode);


            // Optionally, generate model and views here
            // var modelCode = GenerateModelCode(modelType);
            // Save generated model to project
            // File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), $"{modelType}.cs"), modelCode);

            // Generate View templates (omitted for brevity)
        }
        private Project GetActiveProject(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.Solution == null || dte.Solution.Projects == null)
                return null;

            var activeSolutionProjects = (Array)dte.ActiveSolutionProjects;
            if (activeSolutionProjects.Length > 0)
            {
                return (Project)activeSolutionProjects.GetValue(0);
            }
            return null;
        }

        private string GenerateControllerCode(string entityType, string dbContextType)
        {
            var controllerName = entityType + "sController";
            //var modelNamespace = entityType.ClrType.Namespace;
            var modelName = entityType;
            var modelName1 = entityType + "s";

            var controllerCode = $@"
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using genCodeAuto.Models;
using genCodeAuto.Features.{modelName}Features.Queries;
using genCodeAuto.Features.{modelName}Features.Commands;

namespace genCodeAuto.Controllers
{{
    [Route(""api/[controller]"")]
    [ApiController]
    public class {controllerName} : ControllerBase
    {{
        private readonly {dbContextType} _dbContext;
        private readonly IMediator _mediator;

        public {controllerName}({dbContextType} dbContext,IMediator mediator)
        {{
        _dbContext = dbContext;
        _mediator = mediator;
        }}

        // GET: api/{modelName1}
        [HttpGet]
        public async Task<IActionResult> Get{modelName1}()
        {{
            return Ok(await _mediator.Send(new GetAll{modelName}Query()));
        }}

        // GET: api/{modelName1}/5
        [HttpGet(""{{id}}"")]
        public async Task<ActionResult<{modelName}>> Get{modelName}(int id)
        {{
            return Ok(await _mediator.Send(new Get{modelName}ByIdQuery {{ {modelName}Id = id }}));
        }}

        /// PUT: api/{modelName1}/5
        [HttpPut(""{{id}}"")]
        public async Task<IActionResult> Put{modelName}(int id, {modelName} {modelName.ToLower()})
        {{
            if (id != {modelName.ToLower()}.{modelName}Id )
            {{
                return BadRequest();
            }}

           _dbContext.Entry({modelName.ToLower()}).State = EntityState.Modified;

            try
            {{
                await _dbContext.SaveChangesAsync();
            }}
            catch (DbUpdateConcurrencyException)
            {{
                if (!CustomerExists(id))
                {{
                    return NotFound();
                }}
                else
                {{
                    throw;
                }}
            }}

            return NoContent();
        }}

        // POST: api/{modelName1}
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<{modelName}>> Post{modelName}({modelName} {modelName.ToLower()})
        {{
            _dbContext.{modelName1}.Add({modelName.ToLower()});
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(""Get{modelName}"", new {{ id = {modelName.ToLower()}.{modelName}Id }}, {modelName.ToLower()});
        }}

        // DELETE: api/{modelName1}/5
        [HttpDelete(""{{id}}"")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {{
            return Ok(await _mediator.Send(new Delete{modelName}ByIdCommand {{ {modelName}Id = id}}));
        }}
        private bool CustomerExists(int id)
        {{
            return _dbContext.{modelName1}.Any(e => e.{modelName}Id == id);
        }}
    }}
}}";
            return controllerCode;
        }

        private string GenerateDeleteByIdCommandCode(string entityType, string dbContextType)
        {
            var modelNamespace = entityType;
            var modelName = entityType;
            var modelName1 = entityType + "s";
            var commonCode = $@"
using MediatR;
using genCodeAuto.Models;
using Microsoft.EntityFrameworkCore;
namespace genCodeAuto.Features.{modelName}Features.Commands
{{
    public class Delete{modelName}ByIdCommand : IRequest<int>
    {{
        public int {modelName}Id {{ get; set; }}
        public class Delete{modelName}ByIdCommandHandler : IRequestHandler<Delete{modelName}ByIdCommand, int>
        {{
            private readonly {dbContextType} _dbContext;
            public Delete{modelName}ByIdCommandHandler({dbContextType} dbContext)
            {{
                _dbContext = dbContext;
            }}
            public async Task<int> Handle(Delete{modelName}ByIdCommand command, CancellationToken cancellationToken)
            {{
                var {modelName.ToLower()} = await _dbContext.{modelName1}.Where(a => a.{modelName}Id == command.{modelName}Id).FirstOrDefaultAsync();
                if ({modelName.ToLower()} == null) return default;
                _dbContext.{modelName1}.Remove({modelName.ToLower()});
                await _dbContext.SaveChangesAsync();
                return {modelName.ToLower()}.{modelName}Id;
            }}
        }}
    }}
}}
";
            return commonCode;
        }

        private string GenerateGetAllEntityQueryCode(string entityType, string dbContextType)
        {
            var modelNamespace = entityType;
            var modelName = entityType;
            var modelName1 = entityType + "s";
            var queryCode = $@"
using MediatR;
using genCodeAuto.Models;
namespace genCodeAuto.Features.{modelName}Features.Queries
{{
    public class GetAll{modelName}Query : IRequest<IEnumerable<{modelName}>>
    {{
        public int {modelName}Id {{ get; set; }}
        public class GetAll{modelName}QueryHandler : IRequestHandler<GetAll{modelName}Query,IEnumerable <{modelName}>>
        {{
            private readonly {dbContextType} _dbContext;
            public GetAll{modelName}QueryHandler({dbContextType} dbContext)
            {{
                _dbContext = dbContext;
            }}
                public async Task<IEnumerable<{modelName}>> Handle(GetAll{modelName}Query query , CancellationToken cancellationToken)
                {{
                    var {modelName}List =  _dbContext.{modelName1}.ToList();
                    if ({modelName}List == null)
                    {{
                        return null;
                    }}
                    return {modelName}List;
            }}
        }}
    }}
}}
";
            return queryCode;
        }
        private string GenerateGetEntityByIdQueryCode(string entityType, string dbContextType)
        {
            var modelNamespace = entityType;
            var modelName = entityType;
            var modelName1 = entityType + "s";
            var queryCode = $@"
using MediatR;
using genCodeAuto.Models;
namespace genCodeAuto.Features.{modelName}Features.Queries
{{
    public class Get{modelName}ByIdQuery : IRequest<{modelName}>
    {{
        public int {modelName}Id {{ get; set; }}
        public class Get{modelName}ByIdQueryHandler : IRequestHandler<Get{modelName}ByIdQuery, {modelName}>
        {{
            private readonly {dbContextType} _dbContext;
            public Get{modelName}ByIdQueryHandler({dbContextType} dbContext)
            {{
                _dbContext = dbContext;
            }}
            public async Task<{modelName}> Handle(Get{modelName}ByIdQuery query , CancellationToken cancellationToken)
            {{
                var {modelName.ToLower()} = _dbContext.{modelName1}.Where(a => a.{modelName}Id == query.{modelName}Id).FirstOrDefault();
                if ({modelName.ToLower()} == null) return null;
                return {modelName.ToLower()};
            }}
        }}
    }}
}}
";
            return queryCode;
        }

        private string GenerateDTOCode(string entityType, string dbContextType)
        {
            var modelName = entityType;
            var queryCode = $@"
namespace genCodeAuto.Dto
{{
    public class {modelName}DTO 
    {{
        public int {modelName}Id {{ get; set; }}
        public string name {{ get; set; }}
    }}
}}
";
            return queryCode;
        }
    }
}



