using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CsvParser.Configuration;
using CSVParser.Data;
using CsvParser.Data.AIReportQueryRow;

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

			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query
			// TODO: Replace with your actual query



			return query.AsNoTracking();
		}
	}
}