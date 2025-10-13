using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.Data.AIReportQueryRow;

// This is a template for the Qeuery you want to make for a index type, you would pair this with a created Row Template and 
// Add the types of both to the index list. 
namespace CsvParser.QueryTemplates
{
	// TODO: Describe what data this query retrieves
	public class TemplateQueryService
	{
		private readonly TMRRadzenContext _dbContext;
		private readonly AIConfig _settings;

		public TemplateQueryService(
			TMRRadzenContext dbContext,
			IOptions<AIConfig> settings)
		{
			_dbContext = dbContext;
			_settings = settings.Value;
		}

		// TODO: Replace Row your actual Row
		public IQueryable<TemplateCsvRow> GetQuery()
		{
			_dbContext.Database.SetCommandTimeout(_settings.SqlCommandTimeoutSec);



			return query.AsNoTracking();
		}
	}
}